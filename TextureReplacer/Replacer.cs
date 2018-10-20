/*
 * Copyright © 2013-2018 Davorin Učakar
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
using KSP.UI.Screens.Flight;
using UnityEngine;

namespace TextureReplacer
{
  class Replacer
  {
    public const string TexturesDirectory = Util.Directory + "Default/";
    public const string HudNavball = "HUDNavBall";
    public const string IvaNavball = "IVANavBall";
    public static readonly Shader BumpedHeadShader = Shader.Find("Bumped Diffuse");

    static readonly Log log = new Log(nameof(Replacer));
    static readonly Shader BasicVisorShader = Shader.Find("Transparent/Diffuse");

    // General texture replacements.
    readonly List<string> paths = new List<string> { TexturesDirectory };
    readonly Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
    // NavBalls' textures.
    Texture2D hudNavBallTexture;
    Texture2D ivaNavBallTexture;
    // Change shinning quality.
    SkinQuality skinningQuality = SkinQuality.Auto;
    // Print material/texture names when performing texture replacement pass.
    bool logTextures;
    bool logKerbalHierarchy;

    // Instance.
    public static Replacer Instance { get; private set; }

    /// <summary>
    /// General texture replacement step.
    /// </summary>
    void ReplaceTextures()
    {
      foreach (Material material in Resources.FindObjectsOfTypeAll<Material>()) {
        if (!material.HasProperty(Util.MainTexProperty)) {
          continue;
        }

        Texture texture = material.mainTexture;
        if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp", StringComparison.Ordinal)) {
          continue;
        }

        if (logTextures) {
          log.Print("[{0}] {1}", material.name, texture.name);
        }

        mappedTextures.TryGetValue(texture.name, out Texture2D newTexture);

        if (newTexture != null) {
          if (newTexture != texture) {
            newTexture.anisoLevel = texture.anisoLevel;
            newTexture.wrapMode = texture.wrapMode;

            material.mainTexture = newTexture;
            UnityEngine.Object.Destroy(texture);
          }
        }

        if (!material.HasProperty(Util.BumpMapProperty)) {
          continue;
        }

        Texture normalMap = material.GetTexture(Util.BumpMapProperty);
        if (normalMap == null) {
          continue;
        }

        mappedTextures.TryGetValue(normalMap.name, out Texture2D newNormalMap);

        if (newNormalMap != null) {
          if (newNormalMap != normalMap) {
            newNormalMap.anisoLevel = normalMap.anisoLevel;
            newNormalMap.wrapMode = normalMap.wrapMode;

            material.SetTexture(Util.BumpMapProperty, newNormalMap);
            UnityEngine.Object.Destroy(normalMap);
          }
        }
      }
    }

    /// <summary>
    /// Replace NavBalls' textures.
    /// </summary>
    void UpdateNavball()
    {
      if (hudNavBallTexture != null) {
        NavBall hudNavball = UnityEngine.Object.FindObjectOfType<NavBall>();
        if (hudNavball != null) {
          hudNavball.navBall.GetComponent<Renderer>().sharedMaterial.SetTexture("_MainTexture", hudNavBallTexture);
        }
      }

      if (ivaNavBallTexture != null && InternalSpace.Instance != null) {
        InternalNavBall ivaNavball = InternalSpace.Instance.GetComponentInChildren<InternalNavBall>();
        if (ivaNavball != null) {
          ivaNavball.navBall.GetComponent<Renderer>().sharedMaterial.mainTexture = ivaNavBallTexture;
        }
      }
    }

    void FixKerbalModels()
    {
      mappedTextures.TryGetValue("kerbalHeadNRM", out Texture2D maleHeadNormalMap);
      mappedTextures.TryGetValue("kerbalGirl_06_BaseColorNRM", out Texture2D femaleHeadNormalMap);
      mappedTextures.TryGetValue("kerbalVisor", out Texture2D ivaVisorTexture);
      mappedTextures.TryGetValue("EVAvisor", out Texture2D evaVisorTexture);

      // Shaders between male and female models are inconsistent, female models are missing normal maps and specular
      // lighting. So, we copy shaders from male materials to respective female materials.
      Kerbal[] kerbals = Resources.FindObjectsOfTypeAll<Kerbal>();

      Kerbal maleIva = kerbals.First(k => k.transform.name == "kerbalMale");
      Kerbal femaleIva = kerbals.First(k => k.transform.name == "kerbalFemale");
      Part maleEva = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;
      Part femaleEva = PartLoader.getPartInfoByName("kerbalEVAfemale").partPrefab;

      if (logKerbalHierarchy) {
        log.Print("Male IVA Hierarchy");
        Util.LogDownHierarchy(maleIva.transform);
        log.Print("Male EVA Hierarchy");
        Util.LogDownHierarchy(maleEva.transform);
        log.Print("Female IVA Hierarchy");
        Util.LogDownHierarchy(femaleIva.transform);
        log.Print("Female EVA Hierarchy");
        Util.LogDownHierarchy(femaleEva.transform);
      }

      SkinnedMeshRenderer[][] maleMeshes = {
        maleIva.GetComponentsInChildren<SkinnedMeshRenderer>(true),
        maleEva.GetComponentsInChildren<SkinnedMeshRenderer>(true)
      };

      SkinnedMeshRenderer[][] femaleMeshes = {
        femaleIva.GetComponentsInChildren<SkinnedMeshRenderer>(true),
        femaleEva.GetComponentsInChildren<SkinnedMeshRenderer>(true)
      };

      // Male materials to be copied to females to fix tons of female issues (missing normal maps, non-bumpmapped
      // shaders, missing teeth texture ...)
      Material headMaterial = null;
      Material[] suitMaterials = { null, null };
      Material[] helmetMaterials = { null, null };
      Material[] visorMaterials = { null, null };
      Material jetpackMaterial = null;

      for (int i = 0; i < 2; ++i) {
        foreach (SkinnedMeshRenderer smr in maleMeshes[i]) {
          // Many meshes share the same material, so it suffices to enumerate only one mesh for each material.
          switch (smr.name) {
            case "headMesh01":
            case "headMesh02":
              if (maleHeadNormalMap != null) {
                // Replace with bump-mapped shader so normal maps for heads will work.
                smr.sharedMaterial.shader = BumpedHeadShader;
                smr.sharedMaterial.SetTexture(Util.BumpMapProperty, maleHeadNormalMap);
              }

              headMaterial = smr.sharedMaterial;
              break;

            case "body01":
              suitMaterials[i] = smr.sharedMaterial;
              break;

            case "helmet":
              helmetMaterials[i] = smr.sharedMaterial;
              break;

            case "jetpack_base01":
              jetpackMaterial = smr.sharedMaterial;
              break;

            case "visor":
              // It will be raplaced with reflective shader later, if reflections are enabled.
              if (smr.transform.root == maleIva.transform && ivaVisorTexture != null) {
                smr.sharedMaterial.shader = BasicVisorShader;
                smr.sharedMaterial.mainTexture = ivaVisorTexture;
                smr.sharedMaterial.color = Color.white;
              } else if (smr.transform.root == maleEva.transform && evaVisorTexture != null) {
                smr.sharedMaterial.shader = BasicVisorShader;
                smr.sharedMaterial.mainTexture = evaVisorTexture;
                smr.sharedMaterial.color = Color.white;
              }

              visorMaterials[i] = smr.sharedMaterial;
              break;
          }
        }
      }

      for (int i = 0; i < 2; ++i) {
        foreach (SkinnedMeshRenderer smr in femaleMeshes[i]) {
          // Here we must enumarate all meshes wherever we are replacing the material.
          switch (smr.name) {
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
              if (femaleHeadNormalMap != null) {
                // Replace with bump-mapped shader so normal maps for heads will work.
                smr.sharedMaterial.shader = BumpedHeadShader;
                smr.sharedMaterial.SetTexture(Util.BumpMapProperty, femaleHeadNormalMap);
              }

              smr.sharedMaterial = headMaterial;
              break;

            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
              // Females don't have textured teeth, they use the same material as for the eyeballs. Extending female
              // head material/texture to their teeth is not possible since teeth overlap with some ponytail subtexture.
              // However, female teeth map to the same texture coordinates as male teeth, so we fix this by applying
              // male head & teeth material for female teeth.
              smr.sharedMaterial = headMaterial;
              break;

            case "mesh_female_kerbalAstronaut01_body01":
              smr.sharedMaterial = suitMaterials[i];
              break;

            case "mesh_female_kerbalAstronaut01_helmet":
              smr.sharedMaterial = helmetMaterials[i];
              break;

            case "jetpack_base01":
              smr.sharedMaterial = jetpackMaterial;
              break;

            case "mesh_female_kerbalAstronaut01_visor":
              smr.sharedMaterial = visorMaterials[i];
              break;
          }
        }
      }
    }

    public static void Recreate()
    {
      Instance = new Replacer();
    }

    /// <summary>
    /// Read configuration and perform pre-load initialisation.
    /// </summary>
    public void ReadConfig(ConfigNode rootNode)
    {
      Util.AddLists(rootNode.GetValues("paths"), paths);
      Util.Parse(rootNode.GetValue("skinningQuality"), ref skinningQuality);
      Util.Parse(rootNode.GetValue("logTextures"), ref logTextures);
      Util.Parse(rootNode.GetValue("logKerbalHierarchy"), ref logKerbalHierarchy);
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
      foreach (SkinnedMeshRenderer smr in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>()) {
        if (skinningQuality != SkinQuality.Auto) {
          smr.quality = skinningQuality;
        }
      }

      foreach (Texture texture in Resources.FindObjectsOfTypeAll<Texture>()) {
        if (texture.filterMode == FilterMode.Bilinear) {
          texture.filterMode = FilterMode.Trilinear;
        }
      }

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture) {
        Texture2D texture = texInfo.texture;
        if (texture == null) {
          continue;
        }

        foreach (string path in paths) {
          if (!texture.name.StartsWith(path, StringComparison.Ordinal)) {
            continue;
          }

          string originalName = texture.name.Substring(path.Length);

          // Since we are merging multiple directories, we must expect conflicts.
          if (!mappedTextures.ContainsKey(originalName)) {
            if (originalName.StartsWith("GalaxyTex_", StringComparison.Ordinal)) {
              texture.wrapMode = TextureWrapMode.Clamp;
            }
            mappedTextures.Add(originalName, texture);
          }
          break;
        }
      }

      FixKerbalModels();

      // Find NavBall replacement textures if available.
      if (mappedTextures.TryGetValue(HudNavball, out hudNavBallTexture)) {
        mappedTextures.Remove(HudNavball);

        if (hudNavBallTexture.mipmapCount != 1) {
          log.Print("HUDNavBall texture should not have mipmaps!");
        }
      }

      if (mappedTextures.TryGetValue(IvaNavball, out ivaNavBallTexture)) {
        mappedTextures.Remove(IvaNavball);

        if (ivaNavBallTexture.mipmapCount != 1) {
          log.Print("IVANavBall texture should not have mipmaps!");
        }
      }
    }

    public void OnBeginFlight()
    {
      if (hudNavBallTexture != null || ivaNavBallTexture != null) {
        UpdateNavball();
      }
    }

    public void OnBeginScene()
    {
      ReplaceTextures();
    }
  }
}
