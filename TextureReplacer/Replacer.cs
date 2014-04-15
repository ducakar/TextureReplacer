/*
 * Copyright © 2014 Davorin Učakar
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

namespace TextureReplacer
{
  public class Replacer
  {
    private static readonly string DIR_TEXTURES = Util.DIR + "Default/";
    // General texture replacements.
    private Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
    // Generic texture replacement parameters.
    private int lastMaterialCount = 0;
    // General replacement has to be performed for more than one frame when a scene switch occurs
    // since textures and models may also be loaded with a few frame lag. `updateCounter` specifies
    // for how many frames it should run.
    private int updateCounter = 0;
    // Instance.
    public static Replacer instance = null;

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    private static void log(string s, params object[] args)
    {
      Debug.Log("[TR.Replacer] " + String.Format(s, args));
    }

    /**
     * General texture replacement step.
     */
    private void replaceTextures(Material[] materials)
    {
      foreach (Material material in materials)
      {
        Texture texture = material.mainTexture;
        if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp"))
          continue;

        Texture2D newTexture = null;
        mappedTextures.TryGetValue(texture.name, out newTexture);

        if (newTexture != null)
        {
          if (newTexture != texture)
          {
            newTexture.anisoLevel = texture.anisoLevel;
            newTexture.wrapMode = texture.wrapMode;

            material.mainTexture = newTexture;
            Resources.UnloadAsset(texture);
          }
        }
        // Trilinear filter have already been applied to replacement textures, here we apply it also
        // to original textures that are not being replaced.
        else if (texture.filterMode == FilterMode.Bilinear)
        {
          texture.filterMode = FilterMode.Trilinear;
        }

        Texture normalMap = material.GetTexture("_BumpMap");
        if (normalMap == null)
          continue;

        Texture2D newNormalMap = null;
        mappedTextures.TryGetValue(normalMap.name, out newNormalMap);

        if (newNormalMap != null)
        {
          if (newNormalMap != normalMap)
          {
            newNormalMap.anisoLevel = normalMap.anisoLevel;
            newNormalMap.wrapMode = normalMap.wrapMode;

            material.SetTexture("_BumpMap", newNormalMap);
            Resources.UnloadAsset(normalMap);
          }
        }
        else if (normalMap.filterMode == FilterMode.Bilinear)
        {
          normalMap.filterMode = FilterMode.Trilinear;
        }
      }
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      string lastTextureName = "";

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(DIR_TEXTURES))
          continue;

        string originalName = texture.name.Substring(DIR_TEXTURES.Length);

        // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
        // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides
        // the preceding texture.
        if (texture.name == lastTextureName)
        {
          log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
        }
        // Add a general texture replacement.
        else
        {
          // This in wrapped inside an 'if' clause just in case if corrupted GameDatabase contains
          // non-consecutive duplicated entries for some strange reason.
          if (!mappedTextures.ContainsKey(originalName))
          {
            if (originalName.StartsWith("GalaxyTex_"))
              texture.wrapMode = TextureWrapMode.Clamp;

            mappedTextures.Add(originalName, texture);

            log("Mapped \"{0}\" -> {1}", originalName, texture.name);
          }
        }

        lastTextureName = texture.name;
      }

      // Bumpmapped version of diffuse shader for head.
      Shader bumpedDiffuseShader = Shader.Find("Bumped Diffuse");

      Texture2D ivaVisorTex = null, evaVisorTex = null;
      mappedTextures.TryGetValue("kerbalVisor", out ivaVisorTex);
      mappedTextures.TryGetValue("EVAvisor", out evaVisorTex);

      // Set normal-mapped shader for head and visor texture and reflection on proto-IVA and -EVA
      // Kerbal.
      foreach (SkinnedMeshRenderer smr
               in Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)))
      {
        if (smr.name == "headMesh01")
        {
          smr.material.shader = bumpedDiffuseShader;
        }
        else if (smr.name == "visor")
        {
          bool isEVA = smr.transform.parent.parent.parent.parent == null;
          Texture2D newTexture = isEVA ? evaVisorTex : ivaVisorTex;

          if (newTexture != null)
          {
            smr.sharedMaterial.color = Color.white;
            smr.sharedMaterial.mainTexture = newTexture;
          }
        }
      }
    }

    public void resetScene()
    {
      lastMaterialCount = 0;
      updateCounter = HighLogic.LoadedScene == GameScenes.MAINMENU ? 64 : 16;
    }

    public void updateScene()
    {
      if (updateCounter > 0)
      {
        --updateCounter;

        Material[] materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
        if (materials.Length != lastMaterialCount)
        {
          replaceTextures(materials);
          lastMaterialCount = materials.Length;
        }
      }
    }
  }
}
