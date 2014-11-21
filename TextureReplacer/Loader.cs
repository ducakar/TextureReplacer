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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TextureReplacer
{
  class Loader
  {
    // Texture compression and mipmap generation parameters.
    int lastTextureCount = 0;
    // List of substrings for paths where mipmap generating is enabled.
    readonly List<Regex> generateMipmaps = new List<Regex> { new Regex("^" + Util.DIR) };
    // List of substrings for paths where textures shouldn't be unloaded.
    readonly List<Regex> keepLoaded = new List<Regex> { new Regex("^" + Reflections.DIR_ENVMAP) };
    // NavBall textures.
    static readonly string HUD_NAVBALL = Replacer.DIR_TEXTURES + Replacer.HUD_NAVBALL;
    static readonly string IVA_NAVBALL = Replacer.DIR_TEXTURES + Replacer.IVA_NAVBALL;
    // Features.
    bool? isCompressionEnabled = null;
    bool? isMipmapGenEnabled = null;
    bool? isUnloadingEnabled = null;
    // Mipmap bias for DDS loader.
    public int mipmapBias = 0;
    public int normalMipmapBias = 0;
    // Instance.
    public static Loader instance = null;

    /**
     * Estimate texture size in system RAM.
     *
     * This is only a rough estimate. It doesn't bother with details like the padding bytes.
     */
    static int textureSize(Texture2D texture)
    {
      int nPixels = texture.width * texture.height;
      return texture.format == TextureFormat.DXT1 || texture.format == TextureFormat.RGB24 ?
        nPixels * 3 : nPixels * 4;
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      string sMipmapBias = rootNode.GetValue("mipmapBias");
      if (sMipmapBias != null)
        Int32.TryParse(sMipmapBias, out mipmapBias);

      string sNormalMipmapBias = rootNode.GetValue("normalMipmapBias");
      if (sNormalMipmapBias != null)
        Int32.TryParse(sNormalMipmapBias, out normalMipmapBias);

      mipmapBias = Math.Max(mipmapBias, 0);
      normalMipmapBias = Math.Max(normalMipmapBias, 0);

      string sIsCompressionEnabled = rootNode.GetValue("isCompressionEnabled");
      if (sIsCompressionEnabled != null)
      {
        switch (sIsCompressionEnabled)
        {
          case "always":
            isCompressionEnabled = true;
            break;
          case "never":
            isCompressionEnabled = false;
            break;
          case "auto":
            isCompressionEnabled = null;
            break;
          default:
            Util.log("Invalid value for isCompressionEnabled: {0}", sIsCompressionEnabled);
            break;
        }
      }

      string sIsMipmapGenEnabled = rootNode.GetValue("isMipmapGenEnabled");
      if (sIsMipmapGenEnabled != null)
      {
        switch (sIsMipmapGenEnabled)
        {
          case "always":
            isMipmapGenEnabled = true;
            break;
          case "never":
            isMipmapGenEnabled = false;
            break;
          case "auto":
            isMipmapGenEnabled = null;
            break;
          default:
            Util.log("Invalid value for isMipmapGenEnabled: {0}", sIsMipmapGenEnabled);
            break;
        }
      }

      foreach (string sGenerateMipmaps in rootNode.GetValues("generateMipmaps"))
      {
        foreach (string s in Util.splitConfigValue(sGenerateMipmaps))
          generateMipmaps.Add(new Regex(s));
      }

      string sIsUnloadingEnabled = rootNode.GetValue("isUnloadingEnabled");
      if (sIsUnloadingEnabled != null)
      {
        switch (sIsUnloadingEnabled)
        {
          case "always":
            isUnloadingEnabled = true;
            break;
          case "never":
            isUnloadingEnabled = false;
            break;
          case "auto":
            isUnloadingEnabled = null;
            break;
          default:
            Util.log("Invalid value for isUnloadingEnabled: {0}", sIsUnloadingEnabled);
            break;
        }
      }

      foreach (string sKeepLoaded in rootNode.GetValues("keepLoaded"))
      {
        foreach (string s in Util.splitConfigValue(sKeepLoaded))
          keepLoaded.Add(new Regex(s));
      }
    }

    /**
     * This must be run only once after all configuration files are read.
     */
    public void configure()
    {
      // Prevent conflicts with TextureCompressor. If it is found among loaded plugins, texture
      // compression step will be skipped since TextureCompressor should handle it (better).
      bool isATMDetected =
        AssemblyLoader.loadedAssemblies.Any(a => a.name.StartsWith("ActiveTextureManagement"));

      if (isATMDetected)
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
        if (isUnloadingEnabled == null)
        {
          Util.log("Detected Active Texture Management, disabling texture unloading.");
          isUnloadingEnabled = false;
        }
      }
      else
      {
        if (isCompressionEnabled == null)
          isCompressionEnabled = true;
        if (isMipmapGenEnabled == null)
          isMipmapGenEnabled = true;
        if (isUnloadingEnabled == null)
          isUnloadingEnabled = true;
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
        GameDatabase.TextureInfo texInfo = texInfos[i];
        Texture2D texture = texInfo.texture;

        if (texture == null)
          continue;

        // Apply trilinear filter.
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        if (!texInfo.isReadable)
          continue;

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

        TextureFormat format = texture.format;
        bool hasGenMipmaps = false;
        bool hasCompressed = false;

        // Generate mipmaps if necessary. Images that may be UI icons should be excluded to prevent
        // blurriness when using less-than-full texture quality.
        if (isMipmapGenEnabled.Value && texture.mipmapCount == 1
            && (texture.width > 1 || texture.height > 1)
            && generateMipmaps.Any(r => r.IsMatch(texture.name))
            && texture.name != HUD_NAVBALL
            && texture.name != IVA_NAVBALL)
        {
          Color32[] pixels32 = texture.GetPixels32();

          // PNGs are always loaded as transparent, so we check if they actually contain any
          // transparent pixels. Convert non-transparent PNGs to RGB.
          bool hasAlpha = format == TextureFormat.RGBA32 || format == TextureFormat.DXT5;
          bool isTransparent = hasAlpha && pixels32.Any(p => p.a != 255);

          // Rebuild texture. This time with mipmaps.
          TextureFormat newFormat = isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24;
          texture.Resize(texture.width, texture.height, newFormat, true);
          texture.SetPixels32(pixels32);
          texture.Apply(true, false);

          hasGenMipmaps = true;
        }

        // Compress if necessary.
        if (isCompressionEnabled.Value
            && texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5)
        {
          texture.Compress(true);
          texInfos[i].isCompressed = true;

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
     * Unload textures.
     */
    public void initialise()
    {
      List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;
      int memorySpared = 0;

      foreach (GameDatabase.TextureInfo texInfo in texInfos)
      {
        Texture2D texture = texInfo.texture;

        if (texture == null || !texInfo.isReadable)
          continue;

        // Unload texture from RAM (a.k.a. "make it unreadable") unless set otherwise.
        if (isUnloadingEnabled.Value && !keepLoaded.Any(r => r.IsMatch(texture.name)))
        {
          try
          {
            texture.GetPixel(0, 0);
          }
          catch (UnityException)
          {
            continue;
          }

          memorySpared += textureSize(texture);

          texture.Apply(false, true);
          texInfo.isReadable = false;

          Util.log("Unloaded {0}", texture.name);
        }
      }

      generateMipmaps.Clear();
      generateMipmaps.TrimExcess();
      keepLoaded.Clear();
      keepLoaded.TrimExcess();

      if (memorySpared > 0)
      {
        Util.log("Texture unloading freed approximately {0:0.0} MiB = {1:0.0} MB of system RAM",
                 memorySpared / 1024.0 / 1024.0, memorySpared / 1000.0 / 1000.0);
      }
    }
  }
}
