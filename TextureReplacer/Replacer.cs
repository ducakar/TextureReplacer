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
  class Replacer
  {
    static readonly string DIR_TEXTURES = Util.DIR + "Default/";
    // General texture replacements.
    readonly Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
    // NavBalls' textures.
    Texture2D hudNavBallTexture = null;
    Texture2D ivaNavBallTexture = null;
    // Generic texture replacement parameters.
    int lastMaterialCount = 0;
    // General replacement has to be performed for more than one frame when a scene switch occurs
    // since textures and models may also be loaded with a timed lag.
    float replaceTimer = -1.0f;
    // Print material/texture names when performing texture replacement pass.
    bool logTextures = false;
    // Instance.
    public static Replacer instance = null;

    /**
     * General texture replacement step.
     */
    void replaceTextures(Material[] materials)
    {
      foreach (Material material in materials)
      {
        Texture texture = material.mainTexture;
        if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp"))
          continue;

        if (logTextures)
          Util.log("[{0}] {1}", material.name, texture.name);

        Texture2D newTexture;
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

        Texture2D newNormalMap;
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
     * Replace NavBalls' textures.
     */
    void updateNavball(Vessel vessel)
    {
      if (hudNavBallTexture != null)
      {
        NavBall hudNavball = UnityEngine.Object.FindObjectOfType<NavBall>();

        if (hudNavball != null)
          hudNavball.navBall.renderer.sharedMaterial.mainTexture = hudNavBallTexture;
      }

      if (ivaNavBallTexture != null)
      {
        InternalNavBall ivaNavball = UnityEngine.Object.FindObjectOfType<InternalNavBall>();

        if (ivaNavball != null)
          ivaNavball.navBall.renderer.sharedMaterial.mainTexture = ivaNavBallTexture;
      }
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      string sLogTextures = rootNode.GetValue("logTextures");
      if (sLogTextures != null)
        Boolean.TryParse(sLogTextures, out logTextures);
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(DIR_TEXTURES))
          continue;

        string originalName = texture.name.Substring(DIR_TEXTURES.Length);

        // This in wrapped inside an 'if' clause just in case if corrupted GameDatabase contains
        // non-consecutive duplicated entries for some strange reason.
        if (!mappedTextures.ContainsKey(originalName))
        {
          if (originalName.StartsWith("GalaxyTex_"))
            texture.wrapMode = TextureWrapMode.Clamp;

          mappedTextures.Add(originalName, texture);

          Util.log("Mapped \"{0}\" -> {1}", originalName, texture.name);
        }
      }

      // Bumpmapped version of diffuse shader for head.
      Shader bumpedDiffuseShader = Shader.Find("Bumped Diffuse");

      Texture2D headNormalMap = null;
      Texture2D ivaVisorTexture = null;
      Texture2D evaVisorTexture = null;

      if (mappedTextures.TryGetValue("kerbalHeadNRM", out headNormalMap))
        mappedTextures.Remove("kerbalHeadNRM");

      if (mappedTextures.TryGetValue("kerbalVisor", out ivaVisorTexture))
        mappedTextures.Remove("kerbalVisor");

      if (mappedTextures.TryGetValue("EVAvisor", out evaVisorTexture))
        mappedTextures.Remove("EVAvisor");

      // Set normal-mapped shader for head and visor texture and reflection on proto-IVA and -EVA
      // Kerbal.
      foreach (SkinnedMeshRenderer smr
               in Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)))
      {
        if (smr.name == "headMesh01")
        {
          smr.material.shader = bumpedDiffuseShader;

          if (headNormalMap != null)
            smr.material.SetTexture("_BumpMap", headNormalMap);
        }
        else if (smr.name == "visor")
        {
          bool isEVA = smr.transform.parent.parent.parent.parent == null;
          Texture2D newTexture = isEVA ? evaVisorTexture : ivaVisorTexture;

          if (newTexture != null)
          {
            smr.sharedMaterial.color = Color.white;
            smr.sharedMaterial.mainTexture = newTexture;
          }
        }
      }

      // Find NavBall replacement textures if available.
      if (mappedTextures.TryGetValue("HUDNavBall", out hudNavBallTexture))
        mappedTextures.Remove("HUDNavBall");

      if (mappedTextures.TryGetValue("IVANavBall", out ivaNavBallTexture))
        mappedTextures.Remove("IVANavBall");
    }

    public void resetScene()
    {
      lastMaterialCount = 0;

      GameScenes scene = HighLogic.LoadedScene;

      if (scene == GameScenes.MAINMENU || scene == GameScenes.SPACECENTER)
        replaceTimer = 2.0f;
      else
        replaceTimer = 0.1f;

      if (hudNavBallTexture != null || ivaNavBallTexture != null)
      {
        if (HighLogic.LoadedSceneIsFlight)
          GameEvents.onVesselChange.Add(updateNavball);
        else
          GameEvents.onVesselChange.Remove(updateNavball);
      }
    }

    public void updateScene()
    {
      if (replaceTimer >= 0.0f)
      {
        var materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
        if (materials.Length != lastMaterialCount)
        {
          replaceTextures(materials);
          lastMaterialCount = materials.Length;
        }

        if (replaceTimer == 0.0f)
        {
          GC.Collect();
          replaceTimer = -1.0f;
        }
        else
        {
          replaceTimer = Math.Max(0.0f, replaceTimer - Time.deltaTime);
        }
      }
    }
  }
}
