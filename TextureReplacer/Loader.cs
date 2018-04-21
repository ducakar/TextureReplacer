/*
 * Copyright © 2013-2017 Davorin Učakar
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
    const string HudNavball = Replacer.TexturesDirectory + Replacer.HudNavball;
    const string IvaNavball = Replacer.TexturesDirectory + Replacer.IvaNavball;

    // Instance.
    public static Loader Instance { get; private set; }

    // List of substrings for paths where mipmap generating is enabled.
    readonly List<Regex> generateMipmapsPaths = new List<Regex> {
      new Regex("^" + Util.Directory + "(Default|Heads|Suits)/")
    };
    // List of substrings for paths where textures shouldn't be unloaded.
    readonly List<Regex> keepLoadedPaths = new List<Regex> {
      new Regex("^" + Reflections.EnvMapDirectory)
    };
    // Number of loaded textures it the previous update.
    int lastTextureCount;
    // Features.
    bool? isCompressionEnabled;
    bool? isMipmapGenEnabled;
    bool? isUnloadingEnabled;

    /**
     * Estimate texture size in system RAM.
     *
     * This is only a rough estimate. It doesn't bother with details like the padding bytes.
     */
    static int TextureSize(Texture2D texture)
    {
      int nPixels = texture.width * texture.height;
      return texture.format == TextureFormat.DXT1 || texture.format == TextureFormat.RGB24 ? nPixels * 3 : nPixels * 4;
    }

    public static void Recreate()
    {
      Instance = new Loader();
    }

    public static void Destroy()
    {
      Instance = null;
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void ReadConfig(ConfigNode rootNode)
    {
      Util.Parse(rootNode.GetValue("isCompressionEnabled"), ref isCompressionEnabled);
      Util.Parse(rootNode.GetValue("isMipmapGenEnabled"), ref isMipmapGenEnabled);
      Util.AddRELists(rootNode.GetValues("generateMipmaps"), generateMipmapsPaths);
      Util.Parse(rootNode.GetValue("isUnloadingEnabled"), ref isUnloadingEnabled);
      Util.AddRELists(rootNode.GetValues("keepLoaded"), keepLoadedPaths);
    }

    /**
     * This must be run only once after all configuration files are read.
     */
    public void Configure()
    {
      // Prevent conflicts with TextureCompressor. If it is found among loaded plugins, texture
      // compression step will be skipped since TextureCompressor should handle it (better).
      bool isATMDetected = AssemblyLoader.loadedAssemblies
        .Any(a => a.name.StartsWith("ActiveTextureManagement", StringComparison.Ordinal));

      if (isATMDetected) {
        if (isCompressionEnabled == null) {
          isCompressionEnabled = false;
          Util.Log("Detected Active Texture Management, disabling texture compression.");
        }
        if (isMipmapGenEnabled == null) {
          isMipmapGenEnabled = false;
          Util.Log("Detected Active Texture Management, disabling mipmap generation.");
        }
        if (isUnloadingEnabled == null) {
          isUnloadingEnabled = false;
          Util.Log("Detected Active Texture Management, disabling texture unloading.");
        }
      } else {
        isCompressionEnabled = isCompressionEnabled ?? true;
        isMipmapGenEnabled = isMipmapGenEnabled ?? true;
        isUnloadingEnabled = isUnloadingEnabled ?? true;
      }
    }

    /**
     * Texture compression & mipmap generation pass.
     *
     * This is run on each game update until game database is loaded.
     */
    public void ProcessTextures()
    {
      List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;

      for (int i = lastTextureCount; i < texInfos.Count; ++i) {
        GameDatabase.TextureInfo texInfo = texInfos[i];
        Texture2D texture = texInfo.texture;

        if (texture == null) {
          continue;
        }
        // Apply trilinear filter.
        if (texture.filterMode == FilterMode.Bilinear) {
          texture.filterMode = FilterMode.Trilinear;
        }
        if (!texInfo.isReadable) {
          continue;
        }

        // `texture.GetPixel() throws an exception if the texture is not readable and hence it
        // cannot be compressed nor mipmaps generated.
        try {
          texture.GetPixel(0, 0);
        } catch (UnityException) {
          continue;
        }

        TextureFormat format = texture.format;
        bool hasGenMipmaps = false;
        bool hasCompressed = false;

        // Generate mipmaps if necessary. Images that may be UI icons should be excluded to prevent
        // blurriness when using less-than-full texture quality.
        if (isMipmapGenEnabled.Value && texture.mipmapCount == 1 && (texture.width > 1 || texture.height > 1) &&
            generateMipmapsPaths.Any(r => r.IsMatch(texture.name)) &&
            texture.name != HudNavball && texture.name != IvaNavball) {
          Color32[] pixels32 = texture.GetPixels32();

          // PNGs are always loaded as transparent, so we check if they actually contain any
          // transparent pixels. Convert non-transparent PNGs to RGB.
          bool hasAlpha = format == TextureFormat.RGBA32 || format == TextureFormat.DXT5;
          bool isTransparent = hasAlpha && pixels32.Any(p => p.a != 255);

          // Workaround for a Unity + D3D bug.
          int quality = QualitySettings.masterTextureLimit;

          if (isCompressionEnabled.Value && quality > 0 &&
              (texture.width >> quality) % 4 != 0 && (texture.width >> quality) % 4 != 0) {
            QualitySettings.masterTextureLimit = 0;
          }

          // Rebuild texture. This time with mipmaps.
          TextureFormat newFormat = isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24;
          texture.Resize(texture.width, texture.height, newFormat, true);
          texture.SetPixels32(pixels32);
          texture.Apply(true, false);

          if (quality != QualitySettings.masterTextureLimit) {
            QualitySettings.masterTextureLimit = quality;
          }

          hasGenMipmaps = true;
        }

        // Compress if necessary.
        if (isCompressionEnabled.Value &&
            texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5) {
          texture.Compress(true);
          texInfos[i].isCompressed = true;

          hasCompressed = true;
        }

        if (hasGenMipmaps || hasCompressed) {
          Util.Log("{0} {1} [{2}x{3} {4} -> {5}]",
            hasGenMipmaps && hasCompressed ? "Generated mipmaps & compressed"
            : hasGenMipmaps ? "Generated mipmaps for"
            : "Compressed",
            texture.name, texture.width, texture.height, format, texture.format);
        }
      }

      lastTextureCount = texInfos.Count;
    }

    /**
     * Unload textures.
     */
    public void Initialise()
    {
      List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;
      int memorySpared = 0;

      foreach (GameDatabase.TextureInfo texInfo in texInfos) {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texInfo.isReadable) {
          continue;
        }

        // Unload texture from RAM (a.k.a. "make it unreadable") unless set otherwise.
        if (isUnloadingEnabled.Value && !keepLoadedPaths.Any(r => r.IsMatch(texture.name))) {
          try {
            texture.GetPixel(0, 0);
          } catch (UnityException) {
            continue;
          }

          memorySpared += TextureSize(texture);

          texture.Apply(false, true);
          texInfo.isReadable = false;

          Util.Log("Unloaded {0}", texture.name);
        }
      }

      generateMipmapsPaths.Clear();
      generateMipmapsPaths.TrimExcess();
      keepLoadedPaths.Clear();
      keepLoadedPaths.TrimExcess();

      if (memorySpared > 0) {
        Util.Log("Texture unloading freed approximately {0:0.0} MiB = {1:0.0} MB of system RAM",
          memorySpared / 1024.0 / 1024.0,
          memorySpared / 1000.0 / 1000.0);
      }
    }
  }
}
