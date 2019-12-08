/*
 * Copyright © 2013-2019 Davorin Učakar
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

using System.Collections.Generic;
using System.Linq;
using KSP.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace TextureReplacer
{
  internal class Reflections
  {
    public enum Type
    {
      None,
      Real
    }

    public class Script
    {
      // List of all created reflection scripts.
      private static readonly List<Script> Scripts = new List<Script>();
      private static int currentScript;

      private readonly RenderTexture envMap;
      private readonly Transform transform;
      private readonly Renderer[] meshes;
      private readonly bool[] meshStates;
      private readonly bool isEva;
      private readonly int interval;

      private int counter;
      private int currentFace;
      private bool isActive = true;

      public Script(Component part, int updateInterval)
      {
        envMap = new RenderTexture(reflectionResolution, reflectionResolution, 24) {
          hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, dimension = TextureDimension.Cube
        };

        transform = part.transform;
        isEva = part.GetComponent<KerbalEVA>() != null;

        if (isEva) {
          SkinnedMeshRenderer visor = transform.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(m => m.name == "visor" || m.name == "mesh_female_kerbalAstronaut01_visor");

          if (visor != null) {
            Material material = visor.material;

            material.shader = Instance.visorShader;
            material.SetTexture(Util.CubeProperty, envMap);
            material.SetColor(Util.ReflectColorProperty, visorReflectionColour);
          }
        }

        meshes = transform.GetComponentsInChildren<Renderer>();
        meshStates = new bool[meshes.Length];
        interval = updateInterval;

        counter = Util.Random.Next(updateInterval);
        currentFace = Util.Random.Next(6);

        EnsureCamera();
        Update(true);

        Scripts.Add(this);
      }

      public void Destroy()
      {
        Scripts.Remove(this);
        Object.DestroyImmediate(envMap);
      }

      public bool Apply(Material material, Shader shader, Color reflectionColour)
      {
        Shader reflectiveShader = shader ? shader : Instance.ToReflective(material.shader);

        if (reflectiveShader != null) {
          material.shader = reflectiveShader;
          material.SetTexture(Util.CubeProperty, envMap);
          material.SetColor(Util.ReflectColorProperty, reflectionColour);
          return true;
        }

        return false;
      }

      public void SetActive(bool value)
      {
        if (!isActive && value) {
          Update(true);
        }

        isActive = value;
      }

      public static void UpdateScripts()
      {
        if (Scripts.Count == 0 || Time.frameCount % reflectionInterval != 0) {
          return;
        }

        currentScript %= Scripts.Count;

        int startScript = currentScript;
        do {
          Script script = Scripts[currentScript];
          currentScript = (currentScript + 1) % Scripts.Count;

          if (script.isActive) {
            script.counter = (script.counter + 1) % script.interval;

            if (script.counter == 0) {
              script.Update(false);
              break;
            }
          }
        } while (currentScript != startScript);
      }

      private void Update(bool force)
      {
        int faceMask = force ? 0x3f : 1 << currentFace;

        // Hide all meshes of the current part.
        for (int i = 0; i < meshes.Length; ++i) {
          meshStates[i] = meshes[i].enabled;
          meshes[i].enabled = false;
        }

        Transform cameraTransform = camera.transform;

        // Skybox.
        cameraTransform.position = GalaxyCubeControl.Instance.transform.position;
        camera.farClipPlane = 100.0f;
        camera.cullingMask = 1 << 18;
        camera.RenderToCubemap(envMap, faceMask);

        // Scaled space.
        cameraTransform.position = ScaledSpace.Instance.transform.position;
        camera.farClipPlane = 3.0e7f;
        camera.cullingMask = (1 << 10) | (1 << 23);
        camera.RenderToCubemap(envMap, faceMask);

        // Scene.
        Vector3 pos = transform.position;
        cameraTransform.position = isEva ? pos + 0.4f * transform.up : pos;
        camera.farClipPlane = 60000.0f;
        camera.cullingMask = (1 << 0) | (1 << 1) | (1 << 5) | (1 << 15) | (1 << 17);
        camera.RenderToCubemap(envMap, faceMask);

        // Restore mesh visibility.
        for (int i = 0; i < meshes.Length; ++i) {
          meshes[i].enabled = meshStates[i];
        }

        currentFace = (currentFace + 1) % 6;
      }
    }

    // Instance.
    public static Reflections Instance { get; private set; }

    // Visor reflection feature.
    public bool IsVisorReflectionEnabled { get; private set; }
    // Print names of meshes and their shaders in parts with TRReflection module.
    public bool LogReflectiveMeshes { get; private set; }

    private static readonly Log log = new Log(nameof(Reflections));
    // Reflective shader map.
    private static readonly string[,] ShaderNameMap = {
      {"KSP/Diffuse", "Reflective/Bumped Diffuse"}, {"KSP/Specular", "Reflective/Bumped Diffuse"},
      {"KSP/Bumped", "Reflective/Bumped Diffuse"}, {"KSP/Bumped Specular", "Reflective/Bumped Diffuse"},
      {"KSP/Alpha/Translucent", "Reflective/Bumped Diffuse"},
      {"KSP/Alpha/Translucent Specular", "Reflective/Bumped Diffuse"}
    };

    // Render layers:
    //  0 - parts
    //  1 - RCS jets
    //  5 - engine exhaust
    //  9 - sky/atmosphere
    // 10 - scaled space bodies
    // 15 - buildings, terrain
    // 17 - kerbals
    // 18 - skybox
    // 23 - sun
    private static readonly float[] CullDistances = {
      500.0f, 5.0f, 0.0f, 0.0f, 0.0f, 50.0f, 0.0f, 0.0f, //
      0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,    //
      0.0f, 50.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,   //
      0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f     //
    };

    private readonly Dictionary<Shader, Shader> shaderMap = new Dictionary<Shader, Shader>();
    // Reflection camera.
    private static Camera camera;
    // Reflection type.
    public Type ReflectionType { get; set; }
    // Real reflection resolution.
    private static int reflectionResolution = 128;
    // Interval in frames for updating environment map faces.
    private static int reflectionInterval = 4;
    private static Type globalReflectionType = Type.Real;
    // Reflection colour.
    private static Color visorReflectionColour = Color.white;

    // Reflective visor shader.
    private Shader visorShader;

    public static void Recreate()
    {
      Instance?.Destroy();
      Instance = new Reflections();
    }

    /// <summary>
    /// Read configuration and perform pre-load initialisation.
    /// </summary>
    public void ReadConfig(ConfigNode rootNode)
    {
      bool isVisorReflectionEnabled = IsVisorReflectionEnabled;
      bool logReflectiveMeshes = LogReflectiveMeshes;

      Util.Parse(rootNode.GetValue("reflectionType"), ref globalReflectionType);
      Util.Parse(rootNode.GetValue("reflectionResolution"), ref reflectionResolution);
      Util.Parse(rootNode.GetValue("reflectionInterval"), ref reflectionInterval);
      Util.Parse(rootNode.GetValue("isVisorReflectionEnabled"), ref isVisorReflectionEnabled);
      Util.Parse(rootNode.GetValue("visorReflectionColour"), ref visorReflectionColour);
      Util.Parse(rootNode.GetValue("logReflectiveMeshes"), ref logReflectiveMeshes);

      IsVisorReflectionEnabled = isVisorReflectionEnabled;
      LogReflectiveMeshes = logReflectiveMeshes;
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
      IsVisorReflectionEnabled = false;

      string shadersFileName = Application.platform switch {
        RuntimePlatform.WindowsPlayer => "shaders.windows",
        RuntimePlatform.OSXPlayer     => "shaders.osx",
        _                             => "shaders.linux"
      };

      string shadersPath = IOUtils.GetFilePathFor(GetType(), shadersFileName);

      AssetBundle shadersBundle = null;
      try {
        shadersBundle = AssetBundle.LoadFromFile(shadersPath);
        if (shadersBundle == null) {
          log.Print("Failed to load shader asset file: {0}", shadersPath);
        } else {
          visorShader = shadersBundle.LoadAsset<Shader>("assets/visor.shader");
          if (visorShader == null) {
            log.Print("Visor shader missing in the asset file");
          } else {
            IsVisorReflectionEnabled = true;
          }
        }
      } catch (System.Exception ex) {
        log.Print("Visor shader loading failed: {0}", ex);
      } finally {
        if (shadersBundle != null) {
          shadersBundle.Unload(false);
        }
      }

      for (int i = 0; i < ShaderNameMap.GetLength(0); ++i) {
        Shader original = Shader.Find(ShaderNameMap[i, 0]);
        Shader reflective = Shader.Find(ShaderNameMap[i, 1]);

        if (original == null) {
          log.Print("Shader \"{0}\" missing", ShaderNameMap[i, 0]);
        } else if (reflective == null) {
          log.Print("Shader \"{0}\" missing", ShaderNameMap[i, 1]);
        } else {
          shaderMap[original] = reflective;
        }
      }
    }

    public void OnLoadScenario(ConfigNode node)
    {
      Type type = globalReflectionType;
      Util.Parse(node.GetValue("reflectionType"), ref type);
      ReflectionType = type;
    }

    public void OnSaveScenario(ConfigNode node)
    {
      node.AddValue("reflectionType", ReflectionType);
    }

    private void Destroy()
    {
      if (camera != null) {
        Object.DestroyImmediate(camera.gameObject);
      }

      if (visorShader != null) {
        Object.DestroyImmediate(visorShader);
      }
    }

    private static void EnsureCamera()
    {
      if (camera != null) {
        return;
      }

      camera = new GameObject("TRReflectionCamera", typeof(Camera)).GetComponent<Camera>();
      camera.enabled = false;
      camera.clearFlags = CameraClearFlags.Depth;
      // Any smaller number and visors will reflect internals of helmets.
      camera.nearClipPlane = 0.125f;
      camera.layerCullDistances = CullDistances;
    }

    /**
     * Get reflective version of a shader.
     */
    private Shader ToReflective(Shader shader)
    {
      shaderMap.TryGetValue(shader, out Shader newShader);
      return newShader;
    }
  }
}
