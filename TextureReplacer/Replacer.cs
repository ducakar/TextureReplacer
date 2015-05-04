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
using System.Linq;
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

        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

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

        Texture normalMap = material.GetTexture(Util.BUMPMAP_PROPERTY);
        if (normalMap == null)
          continue;

        if (normalMap.filterMode == FilterMode.Bilinear)
          normalMap.filterMode = FilterMode.Trilinear;

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
      }
    }

    static void updateKerbalMeshes(Transform tf, Texture headNRM, Texture visorTexture, ref Material teethMaterial)
    {
      Shader headShader = Shader.Find("Bumped Diffuse");
      Shader suitShader = Shader.Find("KSP/Bumped Specular");

      foreach (SkinnedMeshRenderer smr in tf.GetComponentsInChildren<SkinnedMeshRenderer>(true))
      {
        // Many meshes share material, so it suffices to enumerate only one mesh for each material.
        switch (smr.name)
        {
          case "headMesh01":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
          case "headMesh":
          case "tongue":
            // Replace with bump-mapped shader so normal maps for heads will work.
            smr.sharedMaterial.shader = headShader;

            if (headNRM != null)
              smr.sharedMaterial.SetTexture(Util.BUMPMAP_PROPERTY, headNRM);

            // Save male head material, we need to apply it to female teeth.
            if (smr.name == "headMesh01")
              teethMaterial = smr.sharedMaterial;
            break;

          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
          case "upTeeth01":
          case "downTeeth01":
            // Females don't have textured teeth, they use the same material as for the eyeballs. Extending female head
            // material/texture to their teeth is not possible since teeth overlap with some ponytail subtexture.
            // However, female teeth map to the same texture coordinates as male teeth, so we fix this by applying male
            // head & teeth material for female teeth.
            if (teethMaterial != null && smr.sharedMaterial != teethMaterial)
              smr.sharedMaterial = teethMaterial;
            break;

          case "body01":
          case "mesh_female_kerbalAstronaut01_body01":
          case "helmet":
          case "mesh_female_kerbalAstronaut01_helmet":
          case "jetpack_base01":
            // Replace body shader for females, so alpha on suits defines specularity, same as for male Kerbals.
            smr.sharedMaterial.shader = suitShader;
            break;

          case "visor":
          case "mesh_female_kerbalAstronaut01_visor":
            if (visorTexture != null)
            {
              smr.sharedMaterial.mainTexture = visorTexture;
              smr.sharedMaterial.color = Color.white;
            }
            break;
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
    public void load()
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

      Texture2D[] headNormalMaps = { null, null };
      Texture2D ivaVisorTexture = null;
      Texture2D evaVisorTexture = null;

      if (mappedTextures.TryGetValue("kerbalHeadNRM", out headNormalMaps[0]))
        mappedTextures.Remove("kerbalHeadNRM");

      if (mappedTextures.TryGetValue("kerbalGirl_06_BaseColorNRM", out headNormalMaps[1]))
        mappedTextures.Remove("kerbalGirl_06_BaseColorNRM");

      if (mappedTextures.TryGetValue("kerbalVisor", out ivaVisorTexture))
        mappedTextures.Remove("kerbalVisor");

      if (mappedTextures.TryGetValue("EVAvisor", out evaVisorTexture))
        mappedTextures.Remove("EVAvisor");

      foreach (SkinnedMeshRenderer smr in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>())
      {
        if (skinningQuality != SkinQuality.Auto)
          smr.quality = skinningQuality;
      }

      // Fix female shaders, set normal-mapped shader for head and visor texture on proto-IVA and -EVA Kerbals.
      Kerbal[] kerbals = Resources.FindObjectsOfTypeAll<Kerbal>();
      Material teethMaterialMaterial = null;

      Kerbal maleIva = kerbals.First(k => k.transform.name == "kerbalMale");
      updateKerbalMeshes(maleIva.transform, headNormalMaps[0], ivaVisorTexture, ref teethMaterialMaterial);

      Kerbal femaleIva = kerbals.First(k => k.transform.name == "kerbalFemale");
      updateKerbalMeshes(femaleIva.transform, headNormalMaps[1], ivaVisorTexture, ref teethMaterialMaterial);

      Part maleEva = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;
      updateKerbalMeshes(maleEva.transform, headNormalMaps[0], evaVisorTexture, ref teethMaterialMaterial);

      Part femaleEva = PartLoader.getPartInfoByName("kerbalEVAfemale").partPrefab;
      updateKerbalMeshes(femaleEva.transform, headNormalMaps[1], evaVisorTexture, ref teethMaterialMaterial);

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
          Util.log("IVANavBall texture should not have mipmaps!");
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
