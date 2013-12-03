/*
 * Copyright © 2013 Davorin Učakar
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice (including the next
 * paragraph) shall be included in all copies or substantial portions of the
 * Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
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
  private int oldMaterialCount = 0;
  private bool isInitialised = false;

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

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;

        texture.filterMode = FilterMode.Trilinear;
        if (texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5)
        {
          Debug.Log("[TextureReplacer] Compressing " + texture.name);
          texture.Compress(true);
        }
      }

      isInitialised = true;
    }

    Material[] materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
    if (materials.Length == oldMaterialCount)
      return;

    oldMaterialCount = materials.Length;

    foreach (Material material in materials)
    {
      Texture texture = material.mainTexture;

      if (texture == null)
        continue;

      if (!mappedTextures.ContainsKey(texture.name))
      {
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        continue;
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
  }
}
