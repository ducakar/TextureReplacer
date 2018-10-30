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
    public const string DefaultPrefix = "TextureReplacer/Default/";
    public const string NavBall = "NavBall";
    public static readonly Vector2 NavBallScale = new Vector2(-1.0f, 1.0f);
    public static readonly Shader StandardShader = Shader.Find("Standard");
    public static readonly Shader BumpedDiffuseShader = Shader.Find("Bumped Diffuse");
    public static readonly Shader BasicVisorShader = Shader.Find("Transparent/Specular");

    static readonly Log log = new Log(nameof(Replacer));

    // General texture replacements.
    readonly Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();

    // NavBall texture.
    Texture2D navBallTexture;
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

        if (newTexture != null && newTexture != texture) {
          newTexture.anisoLevel = texture.anisoLevel;
          newTexture.wrapMode = texture.wrapMode;

          material.mainTexture = newTexture;
          UnityEngine.Object.Destroy(texture);
        }

        if (!material.HasProperty(Util.BumpMapProperty)) {
          continue;
        }

        Texture normalMap = material.GetTexture(Util.BumpMapProperty);
        if (normalMap == null) {
          continue;
        }

        mappedTextures.TryGetValue(normalMap.name, out Texture2D newNormalMap);

        if (newNormalMap != null && newNormalMap != normalMap) {
          newNormalMap.anisoLevel = normalMap.anisoLevel;
          newNormalMap.wrapMode = normalMap.wrapMode;

          material.SetTexture(Util.BumpMapProperty, newNormalMap);
          UnityEngine.Object.Destroy(normalMap);
        }
      }
    }

    /// <summary>
    /// Replace NavBalls' textures.
    /// </summary>
    void UpdateNavBall()
    {
      if (navBallTexture != null) {
        NavBall hudNavBall = UnityEngine.Object.FindObjectOfType<NavBall>();
        if (hudNavBall != null) {
          Material material = hudNavBall.navBall.GetComponent<Renderer>().sharedMaterial;

          material.SetTexture(Util.MainTextureProperty, navBallTexture);
        }

        InternalNavBall ivaNavBall = InternalSpace.Instance.GetComponentInChildren<InternalNavBall>();
        if (ivaNavBall != null) {
          Material material = ivaNavBall.navBall.GetComponent<Renderer>().sharedMaterial;

          material.mainTexture = navBallTexture;
          material.SetTextureScale(Util.MainTexProperty, NavBallScale);
        }
      }
    }

    void FixKerbalModels()
    {
      mappedTextures.TryGetValue("kerbalHeadNRM", out Texture2D maleHeadNormalMap);
      mappedTextures.TryGetValue("kerbalGirl_06_BaseColorNRM", out Texture2D femaleHeadNormalMap);
      mappedTextures.TryGetValue("eyeballLeft", out Texture2D eyeballLeft);
      mappedTextures.TryGetValue("eyeballRight", out Texture2D eyeballRight);
      mappedTextures.TryGetValue("pupilLeft", out Texture2D pupilLeft);
      mappedTextures.TryGetValue("pupilRight", out Texture2D pupilRight);
      mappedTextures.TryGetValue("kerbalVisor", out Texture2D ivaVisorTexture);
      mappedTextures.TryGetValue("EVAvisor", out Texture2D evaVisorTexture);

      // Shaders between male and female models are inconsistent, female models are missing normal maps and specular
      // lighting. So, we copy shaders from male materials to respective female materials.
      Kerbal[] kerbals = Resources.FindObjectsOfTypeAll<Kerbal>();

      Kerbal maleIva = kerbals.First(k => k.transform.name == "kerbalMale");
      Kerbal femaleIva = kerbals.First(k => k.transform.name == "kerbalFemale");
      Part maleEva = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;
      Part femaleEva = PartLoader.getPartInfoByName("kerbalEVAfemale").partPrefab;
      Part maleEvaVintage = PartLoader.getPartInfoByName("kerbalEVAVintage").partPrefab;
      Part femaleEvaVintage = PartLoader.getPartInfoByName("kerbalEVAfemaleVintage").partPrefab;

      if (logKerbalHierarchy) {
        log.Print("Male IVA Hierarchy");
        Util.LogDownHierarchy(maleIva.transform);
        log.Print("Male EVA Hierarchy");
        Util.LogDownHierarchy(maleEva.transform);
        log.Print("Male EVA Vintage Hierarchy");
        Util.LogDownHierarchy(maleEvaVintage.transform);
        log.Print("Female IVA Hierarchy");
        Util.LogDownHierarchy(femaleIva.transform);
        log.Print("Female EVA Hierarchy");
        Util.LogDownHierarchy(femaleEva.transform);
        log.Print("Female EVA Vintage Hierarchy");
        Util.LogDownHierarchy(femaleEvaVintage.transform);
      }

      SkinnedMeshRenderer[][] maleMeshes = {
        maleIva.GetComponentsInChildren<SkinnedMeshRenderer>(true),
        maleEva.GetComponentsInChildren<SkinnedMeshRenderer>(true),
        maleEvaVintage.GetComponentsInChildren<SkinnedMeshRenderer>(true)
      };

      SkinnedMeshRenderer[][] femaleMeshes = {
        femaleIva.GetComponentsInChildren<SkinnedMeshRenderer>(true),
        femaleEva.GetComponentsInChildren<SkinnedMeshRenderer>(true),
        femaleEvaVintage.GetComponentsInChildren<SkinnedMeshRenderer>(true)
      };

      // Male materials to be copied to females to fix tons of female issues (missing normal maps, non-bumpmapped
      // shaders, missing teeth texture ...)
      Material headMaterial = null;
      Material[] visorMaterials = { null, null, null };

      for (int i = 0; i < 3; ++i) {
        foreach (SkinnedMeshRenderer smr in maleMeshes[i]) {
          // Many meshes share the same material, so it suffices to enumerate only one mesh for each material.
          switch (smr.name) {
            case "eyeballLeft":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = eyeballLeft;
              break;

            case "eyeballRight":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = eyeballRight;
              break;

            case "pupilLeft":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = pupilLeft;
              break;

            case "pupilRight":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = pupilRight;
              break;

            case "headMesh01":
            case "headMesh02":
              if (maleHeadNormalMap != null) {
                // Replace with bump-mapped shader so normal maps for heads will work.
                smr.sharedMaterial.shader = BumpedDiffuseShader;
                smr.sharedMaterial.SetTexture(Util.BumpMapProperty, maleHeadNormalMap);
              }

              headMaterial = smr.sharedMaterial;
              break;

            case "visor":
              // It will be raplaced with reflective shader later, if reflections are enabled.
              switch (i) {
                case 0: // maleIva
                  if (ivaVisorTexture != null) {
                    smr.sharedMaterial.shader = BasicVisorShader;
                    smr.sharedMaterial.mainTexture = ivaVisorTexture;
                    smr.sharedMaterial.color = Color.white;
                  }
                  break;

                case 1: // maleEva
                  if (evaVisorTexture != null) {
                    smr.sharedMaterial.shader = BasicVisorShader;
                    smr.sharedMaterial.mainTexture = evaVisorTexture;
                    smr.sharedMaterial.color = Color.white;
                  }
                  break;

                case 2: // maleEvaVintage
                  smr.sharedMaterial = visorMaterials[1];
                  break;
              }

              visorMaterials[i] = smr.sharedMaterial;
              break;
          }
        }
      }

      for (int i = 0; i < 3; ++i) {
        foreach (SkinnedMeshRenderer smr in femaleMeshes[i]) {
          // Here we must enumerate all meshes wherever we are replacing the material.
          switch (smr.name) {
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = eyeballLeft;
              break;

            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = eyeballRight;
              break;

            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = pupilLeft;
              break;

            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
              smr.sharedMaterial.shader = StandardShader;
              smr.sharedMaterial.mainTexture = pupilRight;
              break;

            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
              if (femaleHeadNormalMap != null) {
                // Replace with bump-mapped shader so normal maps for heads will work.
                smr.sharedMaterial.shader = BumpedDiffuseShader;
                smr.sharedMaterial.SetTexture(Util.BumpMapProperty, femaleHeadNormalMap);
              } else {
                // Some female heads use specular shader. Fix it.
                smr.sharedMaterial.shader = headMaterial.shader;
              }
              break;

            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
              // Females don't have textured teeth, they use the same material as for the eyeballs. Extending female
              // head material/texture to their teeth is not possible since teeth overlap with some ponytail subtexture.
              // However, female teeth map to the same texture coordinates as male teeth, so we fix this by applying
              // male head & teeth material for female teeth.
              smr.sharedMaterial = headMaterial;
              break;

            case "visor":
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
      Util.Parse(rootNode.GetValue("skinningQuality"), ref skinningQuality);
      Util.Parse(rootNode.GetValue("logTextures"), ref logTextures);
      Util.Parse(rootNode.GetValue("logKerbalHierarchy"), ref logKerbalHierarchy);
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
      if (skinningQuality != SkinQuality.Auto) {
        foreach (SkinnedMeshRenderer smr in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>()) {
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
        if (texture == null || texture.name.Length == 0) {
          continue;
        }

        int defaultPrefixIndex = texture.name.IndexOf(DefaultPrefix, StringComparison.Ordinal);
        if (defaultPrefixIndex != -1) {
          string originalName = texture.name.Substring(defaultPrefixIndex + DefaultPrefix.Length);
          log.Print("{0} {1}", texture.name, originalName);

          // Since we are merging multiple directories, we must expect conflicts.
          if (!mappedTextures.ContainsKey(originalName)) {
            if (originalName.StartsWith("GalaxyTex_", StringComparison.Ordinal)) {
              texture.wrapMode = TextureWrapMode.Clamp;
            }
            mappedTextures.Add(originalName, texture);
          }
        }
      }

      FixKerbalModels();

      // Find NavBall replacement textures if available.
      if (mappedTextures.TryGetValue(NavBall, out navBallTexture)) {
        mappedTextures.Remove(NavBall);

        if (navBallTexture.mipmapCount != 1) {
          log.Print("NavBall texture should not have mipmaps!");
        }
      }
    }

    public void OnBeginFlight()
    {
      if (navBallTexture != null) {
        UpdateNavBall();
      }
    }

    public void OnBeginScene()
    {
      ReplaceTextures();
    }
  }
}
