/*
 * Copyright © 2013-2015 Davorin Učakar
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
    public static readonly string DIR_TEXTURES = Util.DIR + "Default/";
    public static readonly string HUD_NAVBALL = "HUDNavBall";
    public static readonly string IVA_NAVBALL = "IVANavBall";
    // General texture replacements.
    readonly Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
    // NavBalls' textures.
    Texture2D hudNavBallTexture = null;
    Texture2D ivaNavBallTexture = null;
    // Print material/texture names when performing texture replacement pass.
    bool logTextures = false;
    // Change shinning quality.
    SkinQuality skinningQuality = SkinQuality.Auto;
    // Instance.
    public static Replacer instance = null;

    /**
     * General texture replacement step.
     */
    void replaceTextures()
    {
      foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
      {
        Texture texture = material.mainTexture;
        if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp", StringComparison.Ordinal))
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
            UnityEngine.Object.Destroy(texture);
          }
        }
        // Trilinear filter have already been applied to replacement textures, here we apply it also
        // to original textures that are not being replaced.
        else if (texture.filterMode == FilterMode.Bilinear)
        {
          texture.filterMode = FilterMode.Trilinear;
        }

        Texture normalMap = material.GetTexture(Util.BUMPMAP_PROPERTY);
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

            material.SetTexture(Util.BUMPMAP_PROPERTY, newNormalMap);
            UnityEngine.Object.Destroy(normalMap);
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

      if (ivaNavBallTexture != null && InternalSpace.Instance != null)
      {
        InternalNavBall ivaNavball = InternalSpace.Instance.GetComponentInChildren<InternalNavBall>();

        if (ivaNavball != null)
          ivaNavball.navBall.renderer.sharedMaterial.mainTexture = ivaNavBallTexture;
      }
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      Util.parse(rootNode.GetValue("skinningQuality"), ref skinningQuality);
      Util.parse(rootNode.GetValue("logTextures"), ref logTextures);
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(DIR_TEXTURES, StringComparison.Ordinal))
          continue;

        string originalName = texture.name.Substring(DIR_TEXTURES.Length);

        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        if (originalName.StartsWith("GalaxyTex_", StringComparison.Ordinal))
          texture.wrapMode = TextureWrapMode.Clamp;

        // This in wrapped inside an 'if' clause just in case if corrupted GameDatabase contains
        // non-consecutive duplicated entries for some strange reason.
        if (!mappedTextures.ContainsKey(originalName))
          mappedTextures.Add(originalName, texture);
      }

      Texture2D headNormalMap = null;
      Texture2D ivaVisorTexture = null;
      Texture2D evaVisorTexture = null;

      if (mappedTextures.TryGetValue("kerbalHeadNRM", out headNormalMap))
        mappedTextures.Remove("kerbalHeadNRM");

      if (mappedTextures.TryGetValue("kerbalVisor", out ivaVisorTexture))
        mappedTextures.Remove("kerbalVisor");

      if (mappedTextures.TryGetValue("EVAvisor", out evaVisorTexture))
        mappedTextures.Remove("EVAvisor");

      // Set normal-mapped shader for head and visor texture on proto-IVA and -EVA Kerbals.
      foreach (SkinnedMeshRenderer smr in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>())
      {
        if (skinningQuality != SkinQuality.Auto)
          smr.quality = skinningQuality;

        if (smr.name == "headMesh01")
        {
          // Replace with bump-mapped shader so normal maps for heads will work.
          smr.sharedMaterial.shader = Util.bumpedDiffuseShader;

          if (headNormalMap != null)
            smr.sharedMaterial.SetTexture(Util.BUMPMAP_PROPERTY, headNormalMap);
        }
        else if (smr.name == "visor")
        {
          bool isEVA = smr.transform.root.GetComponent<KerbalEVA>() != null;
          Texture2D newTexture = isEVA ? evaVisorTexture : ivaVisorTexture;

          if (newTexture != null)
          {
            smr.sharedMaterial.mainTexture = newTexture;
            smr.sharedMaterial.color = Color.white;
          }
        }
      }

      // Find NavBall replacement textures if available.
      if (mappedTextures.TryGetValue(HUD_NAVBALL, out hudNavBallTexture))
      {
        mappedTextures.Remove(HUD_NAVBALL);

        if (hudNavBallTexture.mipmapCount != 1)
          Util.log("HUDNavBall texture should not have mipmaps!");
      }

      if (mappedTextures.TryGetValue(IVA_NAVBALL, out ivaNavBallTexture))
      {
        mappedTextures.Remove(IVA_NAVBALL);

        if (ivaNavBallTexture.mipmapCount != 1)
          Util.log("IVANavBall texture shouldn't have mipmaps!");
      }
    }

    public void beginFlight()
    {
      if (hudNavBallTexture != null || ivaNavBallTexture != null)
      {
        updateNavball(FlightGlobals.ActiveVessel);
        GameEvents.onVesselChange.Add(updateNavball);
      }
    }

    public void endFlight()
    {
      if (hudNavBallTexture != null || ivaNavBallTexture != null)
        GameEvents.onVesselChange.Remove(updateNavball);
    }

    public void beginScene()
    {
      replaceTextures();
    }
  }
}
