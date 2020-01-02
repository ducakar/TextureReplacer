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

      private readonly ReflectionProbe probe;

      public Script(Component part)
      {
        reflectionResolution = 1024;

        probe = part.gameObject.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
        probe.resolution = reflectionResolution;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.nearClipPlane = 0.4f;
        probe.shadowDistance = 0;
        probe.blendDistance = 0;
        probe.hdr = false;
        probe.center = new Vector3(0, 100, 0);
        // probe.size = new Vector3(100, 100, 100);
        probe.cullingMask = (1 << BuildingsLayer) | (1 << PartsLayer) | (1 << KerbalsLayer) |
                            (1 << AtmosphereLayer) | (1 << BodiesLayer) | (1 << SunLayer) | (1 << GalaxyLayer);
        probe.realtimeTexture = new RenderTexture(probe.resolution, probe.resolution, 24) {
          dimension = TextureDimension.Cube,
          wrapMode = TextureWrapMode.Clamp
        };

        Transform transform = part.transform;
        bool isEva = part.GetComponent<KerbalEVA>() != null;

        if (isEva) {
          SkinnedMeshRenderer visor = transform.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(m => m.name == "visor" || m.name == "mesh_female_kerbalAstronaut01_visor");

          if (visor != null) {
            Material material = visor.material;

            material.shader = Instance.visorShader;
            material.SetTexture(Util.CubeProperty, probe.realtimeTexture);
            material.SetColor(Util.ReflectColorProperty, visorReflectionColour);

            visor.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            visor.probeAnchor = visor.transform;
          }
        }

        Scripts.Add(this);
      }

      public void Destroy()
      {
        Scripts.Remove(this);
        Object.Destroy(probe.realtimeTexture);
        Object.Destroy(probe);
      }

      public bool Apply(Material material, Shader shader, Color reflectionColour)
      {
        Shader reflectiveShader = shader ? shader : Instance.ToReflective(material.shader);

        if (reflectiveShader != null) {
          material.shader = reflectiveShader;
          material.SetTexture(Util.CubeProperty, probe.realtimeTexture);
          material.SetColor(Util.ReflectColorProperty, reflectionColour);
          return true;
        }

        return false;
      }

      public void SetActive(bool value)
      {
        probe.enabled = value;
      }
    }

    // Instance.
    public static Reflections Instance { get; private set; }

    // Reflection type.
    public Type ReflectionType { get; set; }
    // Print names of meshes and their shaders in parts with TRReflection module.
    public bool LogReflectiveMeshes { get; private set; }

    // World space.
    private const int PartsLayer = 0;
    private const int KerbalsLayer = 17;
    private const int BuildingsLayer = 15;
    // Scaled space.
    private const int AtmosphereLayer = 9;
    private const int BodiesLayer = 10;
    private const int SunLayer = 23;
    // Galaxy cube space.
    private const int GalaxyLayer = 18;

    private static readonly Log log = new Log(nameof(Reflections));

    // Real reflection resolution.
    private static int reflectionResolution = 256;
    private static Type globalReflectionType = Type.None;
    // Reflection colour.
    private static Color visorReflectionColour = Color.white;

    // Reflective shader map.
    private static readonly string[,] ShaderNameMap = {
      {"KSP/Diffuse", "Reflective/Bumped Diffuse"},
      {"KSP/Specular", "Reflective/Bumped Diffuse"},
      {"KSP/Bumped", "Reflective/Bumped Diffuse"},
      {"KSP/Bumped Specular", "Reflective/Bumped Diffuse"},
      {"KSP/Alpha/Translucent", "Reflective/Bumped Diffuse"},
      {"KSP/Alpha/Translucent Specular", "Reflective/Bumped Diffuse"}
    };

    private readonly Dictionary<Shader, Shader> shaderMap = new Dictionary<Shader, Shader>();

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
      bool logReflectiveMeshes = LogReflectiveMeshes;

      Util.Parse(rootNode.GetValue("reflectionType"), ref globalReflectionType);
      Util.Parse(rootNode.GetValue("reflectionResolution"), ref reflectionResolution);
      Util.Parse(rootNode.GetValue("visorReflectionColour"), ref visorReflectionColour);
      Util.Parse(rootNode.GetValue("logReflectiveMeshes"), ref logReflectiveMeshes);

      LogReflectiveMeshes = logReflectiveMeshes;
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
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
      if (visorShader != null) {
        Object.DestroyImmediate(visorShader);
      }
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
