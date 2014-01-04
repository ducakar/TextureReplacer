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
using UnityEngine;

// headMesh01
// eyeballLeft, eyeballRight
// pupilLeft, pupilRight
// upTeeth01, upTeeth02, tongue
// headset_band01 (?)
//
[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class VegaTextureReplacer : MonoBehaviour
{
  private static readonly string DIR_PREFIX = "TextureReplacer/";
  private static readonly string DIR_CUSTOM_KERBALS = DIR_PREFIX + "CustomKerbals/";
  private Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
  private Dictionary<string, Texture2D> kerbalHeads = new Dictionary<string, Texture2D>();
  private int updateCounter = 0;
  private int lastMaterialsCount = 0;
  private Vessel lastVessel = null;
  private int lastVesselCount = 0;
  private int lastCrewCount = 0;
  private bool isReplaceScheduled = false;
  private int memorySpared = 0;
  private int lastTextureCount = 0;
  private bool hasCompressor = false;
  private bool isInitialised = false;

  private static void log(string s, params object[] args)
  {
    Debug.Log("[TextureReplacer] " + String.Format(s, args));
  }

  /**
   * Estimate image size.
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
  private void processTextures()
  {
    List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;

    for (int i = lastTextureCount; i < texInfos.Count; ++i)
    {
      Texture2D texture = texInfos[i].texture;
      TextureFormat format = texture.format;

      // Set trilinear filter.
      texture.filterMode = FilterMode.Trilinear;

      // `texture.GetPixel() throws an exception if the texture is not readable and hence it cannot
      // be compressed nor mipmaps generated.
      try
      {
        texture.GetPixel(0, 0);
      }
      catch (UnityException)
      {
        continue;
      }

      // Generate mipmaps if neccessary. Images that may be UI icons should be excluded to prevent
      // blurrines when using less-than-full texture quality.
      if (texture.mipmapCount == 1 && (texture.width | texture.height) != 1
          && (texture.name.StartsWith(DIR_PREFIX)
          || texture.name.IndexOf("/FX/", StringComparison.OrdinalIgnoreCase) >= 0
          || texture.name.IndexOf("/Parts/", StringComparison.OrdinalIgnoreCase) >= 0
          || texture.name.IndexOf("/Spaces/", StringComparison.OrdinalIgnoreCase) >= 0))
      {
        int oldSize = textureSize(texture);
        bool isTransparent = false;
        Color32[] pixels32 = null;

        if (format == TextureFormat.RGBA32 || format == TextureFormat.DXT5)
        {
          pixels32 = texture.GetPixels32();

          foreach (Color32 pixel in pixels32)
          {
            if (pixel.a != 255)
            {
              isTransparent = true;
              break;
            }
          }
        }

        if (isTransparent)
        {
          texture.Resize(texture.width, texture.height, TextureFormat.RGBA32, true);
          texture.SetPixels32(pixels32);
        }
        else
        {
          Color[] pixels24 = texture.GetPixels();

          texture.Resize(texture.width, texture.height, TextureFormat.RGB24, true);
          texture.SetPixels(pixels24);
        }

        texture.Apply(true, false);
        texture.Compress(true);

        int newSize = textureSize(texture);
        memorySpared += oldSize - newSize;

        log("Generated mipmaps for {0} [{1}x{2} {3} -> {4}]",
            texture.name, texture.width, texture.height, format, texture.format);
      }
      // Compress if neccessary.
      else if (!hasCompressor && format != TextureFormat.DXT1 && format != TextureFormat.DXT5)
      {
        int oldSize = textureSize(texture);

        texture.Compress(true);

        int newSize = textureSize(texture);
        memorySpared += oldSize - newSize;

        log("Compressed {0} [{1}x{2} {3} -> {4}]",
            texture.name, texture.width, texture.height, format, texture.format);
      }
    }

    lastTextureCount = texInfos.Count;
  }

  /**
   * Texture replacement step.
   *
   * This is run every 10 frames in main menu (because KSP resets twice when main menu opens) and in
   * the flight scene on scene start, vessel switch or docking. The vessel switch and docking runs
   * are required to fix IVA suit textures that are reset by KSP. Vessel switch also occurs on scene
   * start, so there's no need to explicitly cover that case.
   *
   * I'm not sure if unloading textures during flight is a good idea. Since some textures are often
   * reset to the old ones they would need to be re-loaded.
   */
  private void replaceTextures(Material[] materials, bool doUnload)
  {
    foreach (Material material in materials)
    {
      Texture texture = material.mainTexture;
      if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp"))
        continue;

      if (!mappedTextures.ContainsKey(texture.name))
      {
        // Set trilinear filter. Trilinear filter is also set in initialisation but it only iterates
        // through textures in `GameData/`.
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        continue;
      }

      Texture2D newTexture = mappedTextures[texture.name];
      if (newTexture == texture)
        continue;

      material.mainTexture = newTexture;
      if (doUnload)
        Resources.UnloadAsset(texture);

      Texture normalMap = material.GetTexture("_BumpMap");
      if (normalMap == null || !mappedTextures.ContainsKey(normalMap.name))
        continue;

      Texture2D newNormalMap = mappedTextures[normalMap.name];
      if (newNormalMap == normalMap)
        continue;

      material.SetTexture("_BumpMap", newNormalMap);
      if (doUnload)
        Resources.UnloadAsset(normalMap);
    }
  }

  /**
   * Set custom Kerbal head textures.
   *
   * This is run after texture replacement step on each vessel switch, docking and when number of
   * vessel on the scene changes (for the case when you approach a Kerbal on EVA).
   */
  private void replaceKerbalHeads()
  {
    foreach (KerbalEVA ke in KerbalEVA.FindObjectsOfType(typeof(KerbalEVA)))
    {
      GameObject go = ke.gameObject;
      Vessel vessel = go.GetComponent<Vessel>();

      if (vessel == null || !kerbalHeads.ContainsKey(vessel.vesselName))
        continue;

      foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
      {
        if (smr.name != "headMesh01")
          continue;

        Texture2D newTexture = kerbalHeads[vessel.vesselName];
        if (newTexture != smr.material.mainTexture)
          smr.material.mainTexture = newTexture;

        break;
      }
    }

    foreach (Kerbal k in Kerbal.FindObjectsOfType(typeof(Kerbal)))
    {
      GameObject go = k.gameObject;

      if (!kerbalHeads.ContainsKey(go.name))
        continue;

      foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
      {
        if (smr.name != "headMesh01")
          continue;

        Texture2D newTexture = kerbalHeads[go.name];
        if (newTexture != smr.material.mainTexture)
          smr.material.mainTexture = newTexture;

        break;
      }
    }
  }

  /**
   * Initialisation for textures replacement.
   */
  private void initialiseReplacer()
  {
    string lastTextureName = "";

    foreach (GameDatabase.TextureInfo texInfo
             in GameDatabase.Instance.databaseTexture.FindAll(ti => ti.name.StartsWith(DIR_PREFIX)))
    {
      Texture2D texture = texInfo.texture;
      if (texture == null)
        continue;

      // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
      // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides the
      // preceding texture.
      if (texture.name == lastTextureName)
      {
        log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
      }
      else if (texture.name.StartsWith(DIR_CUSTOM_KERBALS))
      {
        int lastSlash = texture.name.LastIndexOf('/');
        int kerbalNameLength = lastSlash - DIR_CUSTOM_KERBALS.Length;
        string originalName = texture.name.Substring(lastSlash + 1);
        string kerbalName = texture.name.Substring(DIR_CUSTOM_KERBALS.Length, kerbalNameLength);

        if (!kerbalHeads.ContainsKey(kerbalName))
        {
          log("Mapping {0}'s {1} -> {2}", kerbalName, originalName, texture.name);
          kerbalHeads.Add(kerbalName, texture);
        }
      }
      else
      {
        int lastSlash = texture.name.LastIndexOf('/');
        string originalName = texture.name.Substring(lastSlash + 1);

        // This in wrapped inside an 'if' clause just in case if corrupted GameDatabase contains
        // non-consecutive duplicated entries for some strange reason.
        if (!mappedTextures.ContainsKey(originalName))
        {
          log("Mapping {0} -> {1}", originalName, texture.name);
          mappedTextures.Add(originalName, texture);
        }
      }

      lastTextureName = texture.name;
    }

    // Replace textures (and apply trilinear filter). This doesn't reach some textures like skybox
    // and kerbalMainGrey. Those will be replaced later.
    replaceTextures((Material[]) Resources.FindObjectsOfTypeAll(typeof(Material)), true);
  }

  protected void Start()
  {
    DontDestroyOnLoad(this);

    // Prevent conficts with TextureCompressor. If it is found among loaded plugins, texture
    // compression step will be skipped since TextureCompressor should handle it (better).
    foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
    {
      if (go.name == "TextureCompressor")
      {
        log("Detected TextureCompressor, disabling texture compression");
        hasCompressor = true;
        break;
      }
    }
  }

  protected void Update()
  {
    if (!isInitialised)
    {
      processTextures();

      if (GameDatabase.Instance.IsReady())
      {
        if (memorySpared > 0)
        {
          log("Texture compression spared {0:0.0} MiB = {1:0.0} MB",
              memorySpared / 1024.0 / 1024.0, memorySpared / 1000.0 / 1000.0);
        }

        initialiseReplacer();
        isInitialised = true;
      }
    }
    else if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
    {
      int nVessels = 0;
      FlightGlobals.Vessels.ForEach(v => nVessels = v.loaded ? nVessels + 1 : nVessels);

      // When in flight, perform replacement on each vehicle switch, on docking and when a new
      // vessel appears on the scene. We have to do this to replace Kerbal heads and IVA suits that
      // are reset by KSP when new portraits appear (probably because it sets orange suits for
      // Jeb, Bill & Bob and grey to all others). Replacement is postponed for one frame to avoid
      // possible race conditions.
      if (FlightGlobals.ActiveVessel != lastVessel || lastVessel.GetCrewCount() != lastCrewCount
          || nVessels != lastVesselCount)
      {
        lastVessel = FlightGlobals.ActiveVessel;
        lastVesselCount = nVessels;
        lastCrewCount = lastVessel.GetCrewCount();
        isReplaceScheduled = true;

        lastMaterialsCount = 0;
        updateCounter = 0;
      }
      else if (isReplaceScheduled)
      {
        isReplaceScheduled = false;
        replaceTextures((Material[]) Resources.FindObjectsOfTypeAll(typeof(Material)), false);
        replaceKerbalHeads();
      }
    }
    else if (HighLogic.LoadedScene == GameScenes.MAINMENU)
    {
      if (--updateCounter <= 0)
      {
        updateCounter = 10;

        lastVessel = null;
        lastVesselCount = 0;
        lastCrewCount = 0;
        isReplaceScheduled = false;

        // For non-flight scenes we perform replacement once every 10 frames because the following
        // `Resources.FindObjectsOfTypeAll()` call is expensive and the replacement in the
        // initialisation doesn't replace certain textures, like skybox for example.
        Material[] materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
        if (materials.Length != lastMaterialsCount)
        {
          lastMaterialsCount = materials.Length;
          replaceTextures(materials, true);
        }
      }
    }
    else
    {
      lastMaterialsCount = 0;
      updateCounter = 0;

      lastVessel = null;
      lastVesselCount = 0;
      lastCrewCount = 0;
      isReplaceScheduled = false;
    }
  }
}
