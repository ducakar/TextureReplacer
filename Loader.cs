/*
 * Copyright © 2014 Davorin Učakar
 * Copyright © 2013 Ryan Bray
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TextureReplacer
{
  public class Loader
  {
    // Texture compression and mipmap generation parameters.
    private int lastTextureCount = 0;
    private int memorySpared = 0;
    // List of substrings for paths where mipmap generating is enabled.
    private string[] mipmapDirSubstrings = { "/FX/", "/Parts/", "/Spaces/", "TextureReplacer/" };
    // Features.
    private bool? isCompressionEnabled = null;
    private bool? isMipmapGenEnabled = true;
    // Instance.
    public static Loader instance = null;

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    private static void log(string s, params object[] args)
    {
      Debug.Log("[TR.Loader] " + String.Format(s, args));
    }

    /**
     * True iff `i` is a power of two.
     */
    public static bool isPow2(int i)
    {
      return i > 0 && (i & (i - 1)) == 0;
    }

    /**
     * Estimate texture size in RAM.
     *
     * This is only a rough estimate. It doesn't bother with details like the padding bytes or exact
     * mipmap size calculation.
     */
    private static int textureSize(Texture2D texture)
    {
      int nPixels = texture.width * texture.height;
      int size = texture.format == TextureFormat.DXT1 ? nPixels * 4 / 6 :
                 texture.format == TextureFormat.DXT5 ? nPixels * 4 / 4 :
                 texture.format == TextureFormat.Alpha8 ? nPixels * 1 :
                 texture.format == TextureFormat.RGB24 ? nPixels * 3 : nPixels * 4;

      // Is this correct? Does Unity even store mipmaps in RAM?
      if (texture.mipmapCount != 1)
        size += size / 3;

      return size;
    }

    /**
     * Texture compression & mipmap generation pass.
     *
     * This is run on each game update until game database is loaded.
     */
    public void processTextures()
    {
      List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;

      for (int i = lastTextureCount; i < texInfos.Count; ++i)
      {
        Texture2D texture = texInfos[i].texture;

        if (texture != null)
        {
          TextureFormat format = texture.format;

          // `texture.GetPixel() throws an exception if the texture is not readable and hence it
          // cannot be compressed nor mipmaps generated.
          try
          {
            texture.GetPixel(0, 0);
          }
          catch (UnityException)
          {
            continue;
          }

          // Generate mipmaps if necessary. Images that may be UI icons should be excluded to
          // prevent blurriness when using less-than-full texture quality.
          if (isMipmapGenEnabled.Value && texture.mipmapCount == 1
              && (texture.width | texture.height) != 1 && mipmapDirSubstrings != null
              && mipmapDirSubstrings.Any(s => texture.name.IndexOf(s) >= 0))
          {
            int oldSize = textureSize(texture);
            Color32[] pixels32 = texture.GetPixels32();

            // PNGs and JPEGs are always loaded as transparent, so we check if they actually contain
            // any transparent pixels. If not, they are converted to DXT1.
            bool hasAlpha = format == TextureFormat.RGBA32 || format == TextureFormat.DXT5;
            bool isTransparent = hasAlpha && pixels32.Any(p => p.a != 255);

            // Rebuild texture. This time with mipmaps.
            TextureFormat newFormat = isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            texture.Resize(texture.width, texture.height, newFormat, true);
            texture.SetPixels32(pixels32);
            texture.Apply(true, false);

            int newSize = textureSize(texture);
            memorySpared += oldSize - newSize;

            log("Generated mipmaps for {0} [{1}x{2} {3} -> {4}]",
                texture.name, texture.width, texture.height, format, texture.format);

            format = texture.format;
          }

          // Compress if necessary.
          if (isCompressionEnabled.Value
              && format != TextureFormat.DXT1 && format != TextureFormat.DXT5)
          {
            if (!isPow2(texture.width) || !isPow2(texture.height))
            {
              log("Failed to compress {0}, dimensions {1}x{2} are not powers of 2",
                  texture.name, texture.width, texture.height);
            }
            else
            {
              int oldSize = textureSize(texture);

              texture.Compress(true);

              int newSize = textureSize(texture);
              memorySpared += oldSize - newSize;

              log("Compressed {0} [{1}x{2} {3} -> {4}]",
                  texture.name, texture.width, texture.height, format, texture.format);
            }
          }
        }
      }

      lastTextureCount = texInfos.Count;
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public Loader(ConfigNode rootNode)
    {
      string sIsCompressionEnabled = rootNode.GetValue("isCompressionEnabled");
      if (sIsCompressionEnabled != null)
      {
        if (sIsCompressionEnabled == "always")
          isCompressionEnabled = true;
        else if (sIsCompressionEnabled == "never")
          isCompressionEnabled = false;
        else if (sIsCompressionEnabled == "auto")
          isCompressionEnabled = null;
        else
          log("Invalid value for isCompressionEnabled: {0}", sIsCompressionEnabled);
      }

      string sIsMipmapGenEnabled = rootNode.GetValue("isMipmapGenEnabled");
      if (sIsMipmapGenEnabled != null)
      {
        if (sIsMipmapGenEnabled == "always")
          isMipmapGenEnabled = true;
        else if (sIsMipmapGenEnabled == "never")
          isMipmapGenEnabled = false;
        else if (sIsMipmapGenEnabled == "auto")
          isMipmapGenEnabled = null;
        else
          log("Invalid value for isMipmapGenEnabled: {0}", sIsMipmapGenEnabled);
      }

      string sMipmapDirSubstrings = rootNode.GetValue("mipmapDirSubstrings");
      if (sMipmapDirSubstrings != null)
        mipmapDirSubstrings = TextureReplacer.splitConfigValue(sMipmapDirSubstrings);

      // Prevent conflicts with TextureCompressor. If it is found among loaded plugins, texture
      // compression step will be skipped since TextureCompressor should handle it (better).
      bool isTextureCompressorDetected =
        AssemblyLoader.loadedAssemblies.Any(a => a.name.StartsWith("TextureCompressor"));

      if (isTextureCompressorDetected)
      {
        if (isCompressionEnabled == null)
        {
          log("Detected TextureCompressor, disabling texture compression");
          isCompressionEnabled = false;
        }
        if (isMipmapGenEnabled == null)
        {
          log("Detected TextureCompressor, disabling mipmap generation");
          isMipmapGenEnabled = false;
        }
      }

      if (isCompressionEnabled == null)
        isCompressionEnabled = true;
      if (isMipmapGenEnabled == null)
        isMipmapGenEnabled = true;
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      if (memorySpared > 0)
      {
        log("Texture compression spared {0:0.0} MiB = {1:0.0} MB",
            memorySpared / 1024.0 / 1024.0, memorySpared / 1000.0 / 1000.0);
      }
    }
  }
}
