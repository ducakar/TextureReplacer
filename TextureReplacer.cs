/*
 * Copyright © 2013 Davorin Učakar
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software in
 *    a product, an acknowledgement in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
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
public class TextureReplacer : MonoBehaviour
{
  private Dictionary<string, Texture2D> mappedTextures;
  private int updateCounter = 0;
  private int lastMaterialsLength = 0;
  private Vessel lastVessel = null;
  private bool isInitialised = false;
  private bool isReplaceScheduled = false;

  private void initialise()
  {
    // KSP has some issues when calling `new` in a constructor. Since I'm not sure when C# performs
    // static initialisation, it's safer if we allocate this array as a local variable rather than a
    // readonly class member.
    string[] textureNames = new string[] {
      "kerbalHead",
      "kerbalMain", "kerbalMainGrey", "kerbalHelmetGrey",
      "EVAtexture", "EVAhelmet", "EVAjetpack",
      "kerbalMainNRM", "kerbalHelmetNRM", "EVAtextureNRM", "EVAjetpackNRM",
      "GalaxyTex_NegativeX", "GalaxyTex_NegativeY", "GalaxyTex_NegativeZ",
      "GalaxyTex_PositiveX", "GalaxyTex_PositiveY", "GalaxyTex_PositiveZ",
      "suncoronanew",
      "moho00",
      "Eve2_00",
      "evemoon100",
      "KerbinScaledSpace300",
      "NewMunSurfaceMapDiffuse",
      "NewMunSurfaceMap00",
      "Duna5_00",
      "desertplanetmoon00",
      "dwarfplanet100",
      "gas1_clouds",
      "newoceanmoon00",
      "gp1icemoon00",
      "rockymoon100",
      "gp1minormoon100",
      "gp1minormoon200",
      "snowydwarfplanet00"
    };

    mappedTextures = new Dictionary<string, Texture2D>();
    foreach (string name in textureNames)
    {
      string url = "TextureReplacer/Textures/" + name;
      Texture2D texture = GameDatabase.Instance.GetTexture(url, false);

      if (texture != null)
      {
        print("[TextureReplacer] Mapping " + name + " -> " + url);
        mappedTextures.Add(name, texture);
      }
    }

    // This tries to compress all uncompressed textures inside `GameData/` directory. Compression
    // fails on read-only textures (i.e. those which are not loaded in RAM), but there's no way to
    // check whether a texture is read-only. `TextureInfo.isReadable` and `TextureInfo.isCompressed`
    // always return true, no matter whether the texture is readable or compressed. So we have to
    // live with a bunch of error messages in log.
    print("[TextureReplacer] Compressing textures in GameDatabase");

    foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
    {
      Texture2D texture = texInfo.texture;

      texture.filterMode = FilterMode.Trilinear;
      if (texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5)
        texture.Compress(true);
    }

    // Replace textures (and apply trilinear filter). This doesn't reach some textures like skybox
    // and kerbalMainGrey. Those will be replaced later.
    replaceTextures((Material[]) Material.FindObjectsOfTypeIncludingAssets(typeof(Material)));
  }

  private void replaceTextures(Material[] materials)
  {
    print("[TextureReplacer] Replacing textures/setting trilinear filter ...");

    foreach (Material material in materials)
    {
      Texture texture = material.mainTexture;
      if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp"))
        continue;

      if (!mappedTextures.ContainsKey(texture.name))
      {
        // Set trilinear filter. Trilinear filter is also set in initialisation but that loop only
        // iterates through textures in `GameData/`.
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        continue;
      }

      Texture2D newTexture = mappedTextures[texture.name];
      if (newTexture == null || newTexture == texture)
        continue;

      // Replace texture. No need to set trilinear filter here as replacement textures reside in
      // `GameData/` so that has already been set in initialisation.
      material.mainTexture = newTexture;
      Resources.UnloadAsset(texture);

      print("[TextureReplacer] " + texture.name + " replaced");

      Texture normalMap = material.GetTexture("_BumpMap");
      if (normalMap == null || !mappedTextures.ContainsKey(normalMap.name))
        continue;

      Texture2D newNormalMap = mappedTextures[normalMap.name];
      if (newNormalMap == null || newNormalMap == normalMap)
        continue;

      material.SetTexture("_BumpMap", normalMap);
      Resources.UnloadAsset(normalMap);

      print("[TextureReplacer] " + texture.name + " (normal map) replaced");
    }
  }

  public TextureReplacer()
  {
    DontDestroyOnLoad(this);
  }

  public void Update()
  {
    if (!isInitialised)
    {
      if (GameDatabase.Instance.IsReady())
      {
        initialise();
        isInitialised = true;
      }
    }
    else if (HighLogic.LoadedSceneIsFlight)
    {
      // When in flight, perform replacement on each vehicle switch. We have to do this at least
      // because of IVA suits that are reset by KSP on vehicle switch (probably because it sets
      // orange suits to Jeb, Bin & Bob and grey to all others). Replacement is postponed for 1
      // frame to avoid possible race conditions. (I experienced once that IVA textures were not
      // replaced. I suspect race condition as the most plausible cause).
      if (lastVessel != FlightGlobals.ActiveVessel)
      {
        lastVessel = FlightGlobals.ActiveVessel;
        isReplaceScheduled = true;
      }
      else if (isReplaceScheduled)
      {
        isReplaceScheduled = false;
        replaceTextures((Material[]) Resources.FindObjectsOfTypeAll(typeof(Material)));
      }
    }
    else
    {
      lastVessel = null;
      isReplaceScheduled = false;

      if (updateCounter > 0)
      {
        --updateCounter;
      }
      else
      {
        updateCounter = 16;

        // For non-flight scenes we perform replacement once every 10 frames because the next
        // `Resources.FindObjectsOfTypeAll()` call is expensive and the replacement in the
        // initialisation cannot replace certain textures, like skybox for example.
        Material[] materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
        if (materials.Length != lastMaterialsLength)
        {
          lastMaterialsLength = materials.Length;
          replaceTextures(materials);
        }
      }
    }
  }
}
