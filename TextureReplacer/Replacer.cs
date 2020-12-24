/*
 * Copyright © 2013-2020 Davorin Učakar
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
using KSP.UI.Screens.Flight;
using UnityEngine;

namespace TextureReplacer
{
  internal class Replacer
  {
    public const string DefaultPrefix = "TextureReplacer/Default/";
    public static readonly Shader EyeShader = Shader.Find("Specular");
    public static readonly Shader HeadShader = Shader.Find("Diffuse");
    public static readonly Shader BumpedHeadShader = Shader.Find("Bumped Diffuse");

    public static Replacer Instance { get; private set; }

    public Material TeethMaterial;

    private static readonly Vector2 NavBallScale = new Vector2(-1.0f, 1.0f);

    private static readonly Log log = new Log(nameof(Replacer));

    // General texture replacements.
    private readonly Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
    // Non-reflective visor shader.
    private Shader basicVisorShader;
    // NavBall texture.
    private Texture2D navBallTexture;
    private Texture2D navBallTextureEmissive;
    // Print material/texture names when performing texture replacement pass.
    private bool logTextures;

    public static void Recreate()
    {
      Instance = new Replacer();
    }

    /// <summary>
    /// Read configuration and perform pre-load initialisation.
    /// </summary>
    public void ReadConfig(ConfigNode rootNode)
    {
      Util.Parse(rootNode.GetValue("logTextures"), ref logTextures);
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
      basicVisorShader = Reflections.LoadShaderFromAsset("assets/BasicVisor.shader");
      LoadDefaultTextures();
      LoadNavBallTextures();
      FixKerbalModels();
    }

    public void OnBeginFlight()
    {
      if (navBallTexture != null || navBallTextureEmissive != null) {
        UpdateNavBall();
      }
    }

    public void OnBeginScene()
    {
      ReplaceTextures();
    }

    /// <summary>
    /// General texture replacement step.
    /// </summary>
    private void ReplaceTextures()
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
        } else if (texture.filterMode == FilterMode.Bilinear) {
          // Some textures are instantiated after `Load()` has run, so we have to change their filter here.
          texture.filterMode = FilterMode.Trilinear;
        }

        if (material.HasProperty(Util.BumpMapProperty)) {
          Texture normalMap = material.GetTexture(Util.BumpMapProperty);
          if (normalMap != null) {
            mappedTextures.TryGetValue(normalMap.name, out Texture2D newNormalMap);

            if (newNormalMap != null && newNormalMap != normalMap) {
              newNormalMap.anisoLevel = normalMap.anisoLevel;
              newNormalMap.wrapMode = normalMap.wrapMode;
              material.SetTexture(Util.BumpMapProperty, newNormalMap);
              UnityEngine.Object.Destroy(normalMap);
            }
          }
        }

        if (material.HasProperty(Util.EmissiveProperty)) {
          Texture emissive = material.GetTexture(Util.EmissiveProperty);
          if (emissive != null) {
            mappedTextures.TryGetValue(emissive.name, out Texture2D newEmissive);

            if (newEmissive != null && newEmissive != emissive) {
              newEmissive.anisoLevel = emissive.anisoLevel;
              newEmissive.wrapMode = emissive.wrapMode;
              material.SetTexture(Util.EmissiveProperty, newEmissive);
              UnityEngine.Object.Destroy(emissive);
            }
          }
        }
      }
    }

    /// <summary>
    /// Replace NavBalls' textures.
    /// </summary>
    private void UpdateNavBall()
    {
      var hudNavBall = UnityEngine.Object.FindObjectOfType<NavBall>();
      if (hudNavBall != null) {
        Material material = hudNavBall.navBall.GetComponent<Renderer>().sharedMaterial;

        if (navBallTexture != null) {
          material.SetTexture(Util.MainTextureProperty, navBallTexture);
        }
      }

      var ivaNavBall = InternalSpace.Instance.GetComponentInChildren<InternalNavBall>();
      if (ivaNavBall != null) {
        Material material = ivaNavBall.navBall.GetComponent<Renderer>().sharedMaterial;

        if (navBallTexture != null) {
          material.mainTexture = navBallTexture;
          material.SetTextureScale(Util.MainTexProperty, NavBallScale);
        }

        if (navBallTextureEmissive != null) {
          material.SetTexture(Util.EmissiveProperty, navBallTextureEmissive);
          material.SetTextureScale(Util.EmissiveProperty, NavBallScale);
        }
      }
    }

    private void LoadDefaultTextures()
    {
      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture) {
        Texture2D texture = texInfo.texture;
        if (texture == null || texture.name.Length == 0) {
          continue;
        }

        if (texture.filterMode == FilterMode.Bilinear) {
          texture.filterMode = FilterMode.Trilinear;
        }

        int defaultPrefixIndex = texture.name.IndexOf(DefaultPrefix, StringComparison.Ordinal);
        if (defaultPrefixIndex == -1)
          continue;

        string originalName = texture.name.Substring(defaultPrefixIndex + DefaultPrefix.Length);
        // Since we are merging multiple directories, we must expect conflicts.
        if (mappedTextures.ContainsKey(originalName))
          continue;

        if (originalName.StartsWith("GalaxyTex_", StringComparison.Ordinal)) {
          texture.wrapMode = TextureWrapMode.Clamp;
        }

        mappedTextures[originalName] = texture;
        log.Print("Mapping {0} -> {1}", originalName, texture.name);
      }
    }

    private void LoadNavBallTextures()
    {
      // Find NavBall replacement textures if available.
      if (mappedTextures.TryGetValue("NavBall", out navBallTexture)) {
        mappedTextures.Remove("NavBall");

        if (navBallTexture.mipmapCount != 1) {
          log.Print("NavBall texture should not have mipmaps!");
        }
      }

      if (mappedTextures.TryGetValue("NavBallEmissive", out navBallTextureEmissive)) {
        mappedTextures.Remove("NavBallEmissive");

        if (navBallTextureEmissive.mipmapCount != 1) {
          log.Print("NavBallEmissive texture should not have mipmaps!");
        }
      }
    }

    private void FixKerbalModels()
    {
      mappedTextures.TryGetValue("eyeballLeft", out Texture2D eyeballLeft);
      mappedTextures.TryGetValue("eyeballRight", out Texture2D eyeballRight);
      mappedTextures.TryGetValue("pupilLeft", out Texture2D pupilLeft);
      mappedTextures.TryGetValue("pupilRight", out Texture2D pupilRight);
      mappedTextures.TryGetValue("kerbalHeadNRM", out Texture2D kerbalHeadNRM);
      mappedTextures.TryGetValue("kerbalVisor", out Texture2D ivaVisorTexture);
      mappedTextures.TryGetValue("EVAvisor", out Texture2D evaVisorTexture);

      // Shaders between male and female models are inconsistent, female models are missing normal maps and specular
      // lighting. So, we copy shaders from male materials to respective female materials.
      var prefab = Prefab.Instance;

      SkinnedMeshRenderer[][] maleMeshes = {
        prefab.MaleIva.GetComponentsInChildren<SkinnedMeshRenderer>(false),
        prefab.MaleEva.GetComponentsInChildren<SkinnedMeshRenderer>(false),
        prefab.MaleEvaVintage ? prefab.MaleEvaVintage.GetComponentsInChildren<SkinnedMeshRenderer>(false) : null,
        prefab.MaleEvaFuture ? prefab.MaleEvaFuture.GetComponentsInChildren<SkinnedMeshRenderer>(false) : null
      };

      SkinnedMeshRenderer[][] femaleMeshes = {
        prefab.FemaleIva.GetComponentsInChildren<SkinnedMeshRenderer>(false),
        prefab.FemaleEva.GetComponentsInChildren<SkinnedMeshRenderer>(false),
        prefab.FemaleEvaVintage ? prefab.FemaleEvaVintage.GetComponentsInChildren<SkinnedMeshRenderer>(false) : null,
        prefab.FemaleEvaFuture ? prefab.FemaleEvaFuture.GetComponentsInChildren<SkinnedMeshRenderer>(false) : null
      };

      // Male materials to be copied to females to fix tons of female issues (missing normal maps, non-bump-mapped
      // shaders, missing teeth texture ...). There are also other inconsistencies in models, e.g. normal and vintage
      // Kerbals using different materials for eyes (specular vs. non-specular). We try to unify and fix the mess here.
      //
      // Note, though, Vintage and Modern IVA Kerbals are created on the fly, not cloned from prefabricated model, hence
      // we need to fix them each time, in Personaliser.PersonaliseKerbal().
      Material[] visorMaterials = {null, null, null, null};

      for (int i = 0; i < maleMeshes.Length; ++i) {
        if (maleMeshes[i] == null) {
          continue;
        }

        foreach (SkinnedMeshRenderer smr in maleMeshes[i]) {
          // Many meshes share the same material, so it suffices to perform fixes only on one mesh for each material.
          Material material = smr.material;

          switch (smr.name) {
            case "eyeballLeft": {
              material.shader = EyeShader;
              material.mainTexture = eyeballLeft;
              break;
            }
            case "eyeballRight": {
              material.shader = EyeShader;
              material.mainTexture = eyeballRight;
              break;
            }
            case "pupilLeft": {
              material.shader = EyeShader;
              material.mainTexture = pupilLeft;
              if (pupilLeft != null) {
                material.color = Color.white;
              }
              break;
            }
            case "pupilRight": {
              material.shader = EyeShader;
              material.mainTexture = pupilRight;
              if (pupilRight != null) {
                material.color = Color.white;
              }
              break;
            }
            case "headMesh01":
            case "headMesh02": {
              // Replace with bump-mapped shader, so normal maps for heads will work.
              material.shader = kerbalHeadNRM == null ? HeadShader : BumpedHeadShader;
              break;
            }
            case "tongue":
            case "upTeeth01":
            case "upTeeth02": {
              // Replace with bump-mapped shader, so normal maps for heads will work.
              material.shader = kerbalHeadNRM == null ? HeadShader : BumpedHeadShader;
              material.SetTexture(Util.BumpMapProperty, kerbalHeadNRM);
              TeethMaterial = material;
              break;
            }
            case "visor": {
              // It will be replaced with reflective shader later, if reflections are enabled.
              switch (i) {
                case 0: { // maleIva
                  if (ivaVisorTexture != null) {
                    material.shader = basicVisorShader;
                    material.mainTexture = ivaVisorTexture;
                    material.color = Color.white;
                  }
                  break;
                }
                case 1: { // maleEva
                  if (evaVisorTexture != null) {
                    material.shader = basicVisorShader;
                    material.mainTexture = evaVisorTexture;
                    material.color = Color.white;
                  }
                  break;
                }
                case 2: { // maleEvaVintage
                  if (evaVisorTexture != null) {
                    material.shader = basicVisorShader;
                    material.mainTexture = evaVisorTexture;
                    material.color = Color.white;
                  }
                  break;
                }
                case 3: { // maleEvaFuture
                  if (evaVisorTexture != null) {
                    material.shader = basicVisorShader;
                    material.mainTexture = evaVisorTexture;
                    material.color = Color.white;
                  }
                  break;
                }
              }

              visorMaterials[i] = material;
              break;
            }
          }
        }
      }

      for (int i = 0; i < femaleMeshes.Length; ++i) {
        if (femaleMeshes[i] == null) {
          continue;
        }

        foreach (SkinnedMeshRenderer smr in femaleMeshes[i]) {
          // Here we must enumerate all meshes wherever we are replacing the material.
          Material material = smr.material;

          switch (smr.name) {
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft": {
              material.shader = EyeShader;
              material.mainTexture = eyeballLeft;
              break;
            }
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight": {
              material.shader = EyeShader;
              material.mainTexture = eyeballRight;
              break;
            }
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft": {
              material.shader = EyeShader;
              material.mainTexture = pupilLeft;
              if (pupilLeft != null) {
                material.color = Color.white;
              }
              break;
            }
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight": {
              material.shader = EyeShader;
              material.mainTexture = pupilRight;
              if (pupilRight != null) {
                material.color = Color.white;
              }
              break;
            }
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51": {
              // Replace with bump-mapped shader, so normal maps for heads will work.
              material.shader = kerbalHeadNRM == null ? HeadShader : BumpedHeadShader;
              break;
            }
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01": {
              // Females don't have textured teeth, they use the same material as for the eyeballs. Extending female
              // head material/texture to their teeth is not possible since teeth overlap with some ponytail subtexture.
              // However, female teeth map to the same texture coordinates as male teeth, so we fix this by applying
              // male head & teeth material for female teeth.
              smr.material = TeethMaterial;
              break;
            }
            case "visor":
            case "mesh_female_kerbalAstronaut01_visor": {
              smr.material = visorMaterials[i];
              break;
            }
          }
        }
      }
    }
  }
}
