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
  private static readonly string[] TEXTURE_NAMES = {
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
  private Dictionary<string, Texture2D> mappedTextures;
  private Vessel lastVessel = null;
  private bool isInitialised = false;

  private void replaceTexture(Material material)
  {
    Texture texture = material.mainTexture;

    if (texture == null)
      return;

    if (!mappedTextures.ContainsKey(texture.name))
    {
      if (texture.filterMode == FilterMode.Bilinear)
        texture.filterMode = FilterMode.Trilinear;

      return;
    }

    Texture2D newTexture = mappedTextures[texture.name];
    if (newTexture != null && newTexture != texture)
    {
      material.mainTexture = newTexture;
      Resources.UnloadAsset(texture);

      Texture normalMap = material.GetTexture("_BumpMap");
      if (normalMap != null && mappedTextures.ContainsKey(normalMap.name))
      {
        Texture2D newNormalMap = mappedTextures[normalMap.name];
        if (newNormalMap != null && newNormalMap != normalMap)
        {
          material.SetTexture("_BumpMap", normalMap);
          Resources.UnloadAsset(normalMap);
        }
      }
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
      if (!GameDatabase.Instance.IsReady())
        return;

      mappedTextures = new Dictionary<string, Texture2D>();
      foreach (string name in TEXTURE_NAMES)
      {
        string url = "TextureReplacer/Textures/" + name;
        Texture2D texture = GameDatabase.Instance.GetTexture(url, false);

        if (texture != null)
        {
          Debug.Log("[TextureReplacer] Mapping " + name + " -> " + url);
          mappedTextures.Add(name, texture);
        }
      }

      Debug.Log("[TextureReplacer] Compressing textures in GameDatabase");

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;

        texture.filterMode = FilterMode.Trilinear;
        if (texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5)
          texture.Compress(true);
      }

      foreach (Material material in GameObject.FindObjectsOfTypeIncludingAssets(typeof(Material)))
        replaceTexture(material);

      isInitialised = true;
    }

    if (lastVessel != FlightGlobals.ActiveVessel)
    {
      lastVessel = FlightGlobals.ActiveVessel;

      foreach (Material material in Resources.FindObjectsOfTypeAll(typeof(Material)))
        replaceTexture(material);
    }
  }
}
