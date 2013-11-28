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

[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class TextureReplacer : MonoBehaviour
{
  private static readonly string[] TEXTURE_NAMES = {
    "kerbalHead", "kerbalMain", "kerbalMainGrey", "kerbalHelmetGrey",
    "EVAtexture", "EVAhelmet", "EVAjetpack",
    "GalaxyTex_NegativeX", "GalaxyTex_NegativeY", "GalaxyTex_NegativeZ",
    "GalaxyTex_PositiveX", "GalaxyTex_PositiveY", "GalaxyTex_PositiveZ"
  };
  private Dictionary<string, Texture2D> mappedTextures;
  private int oldTextureCount = 0;
  private bool areTexturesLoaded = false;

  public TextureReplacer()
  {
    DontDestroyOnLoad(this);
  }

  public void Update()
  {
    if (!areTexturesLoaded)
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
          texture.filterMode = FilterMode.Trilinear;
          mappedTextures.Add(name, texture);
        }
      }
      areTexturesLoaded = true;
    }

    Texture[] textures = Resources.FindObjectsOfTypeAll(typeof(Texture)) as Texture[];
    if (textures.Length == oldTextureCount)
      return;

    oldTextureCount = textures.Length;

    Material[] materials = Resources.FindObjectsOfTypeAll(typeof(Material)) as Material[];
    foreach (Material material in materials)
    {
      if (material.mainTexture == null)
        continue;

      if (!mappedTextures.ContainsKey(material.mainTexture.name))
      {
        if (material.mainTexture.filterMode == FilterMode.Bilinear)
          material.mainTexture.filterMode = FilterMode.Trilinear;

        continue;
      }

      Texture2D texture = mappedTextures[material.mainTexture.name];
      if (texture != null)
      {
        Resources.UnloadAsset(material.mainTexture);
        material.mainTexture = mappedTextures[material.mainTexture.name];
      }
    }
  }
}
