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
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;

namespace TextureReplacer
{
  internal class Loader
  {
    // Texture compression and mipmap generation parameters.
    private int lastTextureCount = 0;
    private int memorySpared = 0;
    // List of substrings for paths where mipmap generating is enabled.
    private List<Regex> generateMipmaps = new List<Regex>();
    // List of substrings for paths where textures shouldn't be unloaded.
    private List<Regex> keepReadable = new List<Regex>();
    // Features.
    private bool? isCompressionEnabled = null;
    private bool? isMipmapGenEnabled = true;
    // Instance.
    public static Loader instance = null;

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

      if (texture.mipmapCount != 1)
        size += size / 3;

      return size;
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
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
          Util.log("Invalid value for isCompressionEnabled: {0}", sIsCompressionEnabled);
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
          Util.log("Invalid value for isMipmapGenEnabled: {0}", sIsMipmapGenEnabled);
      }

      string sGenerateMipmaps = rootNode.GetValue("generateMipmaps");
      if (sGenerateMipmaps != null)
      {
        foreach (string s in Util.splitConfigValue(sGenerateMipmaps))
          generateMipmaps.Add(new Regex(s));
      }

      string sKeepReadable = rootNode.GetValue("keepReadable");
      if (sKeepReadable != null)
      {
        foreach (string s in Util.splitConfigValue(sKeepReadable))
          keepReadable.Add(new Regex(s));
      }
    }

    /**
     * This must be run only once after all configuration files are read.
     */
    public void configure()
    {
      // Prevent conflicts with TextureCompressor. If it is found among loaded plugins, texture
      // compression step will be skipped since TextureCompressor should handle it (better).
      bool isACMDetected =
        AssemblyLoader.loadedAssemblies.Any(a => a.name.StartsWith("ActiveTextureManagement"));

      if (isACMDetected)
      {
        if (isCompressionEnabled == null)
        {
          Util.log("Detected Active Texture Management, disabling texture compression.");
          isCompressionEnabled = false;
        }
        if (isMipmapGenEnabled == null)
        {
          Util.log("Detected Active Texture Management, disabling mipmap generation.");
          isMipmapGenEnabled = false;
        }
      }
      else
      {
        if (isCompressionEnabled == null)
          isCompressionEnabled = true;
        if (isMipmapGenEnabled == null)
          isMipmapGenEnabled = true;
      }
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
        TextureFormat format = texture.format;

        if (texture == null)
          continue;

        // Apply trilinear filter.
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

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

        bool hasGenMipmaps = false;
        bool hasCompressed = false;

        // Generate mipmaps if necessary. Images that may be UI icons should be excluded to prevent
        // blurriness when using less-than-full texture quality.
        if (isMipmapGenEnabled.Value && texture.mipmapCount == 1 &&
            (texture.width > 1 || texture.height > 1)
            && generateMipmaps.Any(r => r.IsMatch(texture.name)))
        {
          Color32[] pixels32 = texture.GetPixels32();
          int oldSize = textureSize(texture);

          // PNGs are always loaded as transparent, so we check if they actually contain any
          // transparent pixels. Convert non-transparent PNGs to RGB.
          bool hasAlpha = format == TextureFormat.RGBA32 || format == TextureFormat.DXT5;
          bool isTransparent = hasAlpha && pixels32.Any(p => p.a != 255);

          // Rebuild texture. This time with mipmaps.
          TextureFormat newFormat = isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24;
          texture.Resize(texture.width, texture.height, newFormat, true);
          texture.SetPixels32(pixels32);
          texture.Apply(true, false);

          int newSize = textureSize(texture);
          memorySpared += oldSize - newSize;

          hasGenMipmaps = true;
        }

        // Compress if necessary.
        if (isCompressionEnabled.Value
            && texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5
            && Util.isPow2(texture.width) && Util.isPow2(texture.height)
            && (texture.width >= 4 || texture.height >= 4))
        {
          int oldSize = textureSize(texture);

          texture.Compress(true);
          texInfos[i].isCompressed = true;

          int newSize = textureSize(texture);
          memorySpared += oldSize - newSize;

          hasCompressed = true;
        }

        if (hasGenMipmaps || hasCompressed)
        {
          Util.log("{0} {1} [{2}x{3} {4} -> {5}]",
                   hasGenMipmaps && hasCompressed ? "Generated mipmaps & compressed" :
                   hasGenMipmaps ? "Generated mipmaps for" : "Compressed",
                   texture.name, texture.width, texture.height, format, texture.format);
        }
      }

      lastTextureCount = texInfos.Count;
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;

      for (int i = 0; i < texInfos.Count; ++i)
      {
        GameDatabase.TextureInfo texInfo = texInfos[i];
        Texture2D texture = texInfo.texture;

        if (texture == null)
          continue;

        try
        {
          texture.GetPixel(0, 0);
        }
        catch (UnityException)
        {
          continue;
        }

        // Unload texture from RAM (a.k.a. "make it unreadable") unless set otherwise.
        if (!texture.name.StartsWith(Reflections.DIR_ENVMAP)
            && !keepReadable.Any(r => r.IsMatch(texture.name)))
        {
          int size = textureSize(texture);

          texture.Apply(false, true);
          texInfos[i].isReadable = false;

          memorySpared += size;

          Util.log("Unloaded {0}", texture.name);
        }
      }

      if (memorySpared > 0)
      {
        Util.log("Texture compression & unloading spared approximately {0:0.0} MiB = {1:0.0} MB",
                 memorySpared / 1024.0 / 1024.0, memorySpared / 1000.0 / 1000.0);
      }
    }
  }
}
