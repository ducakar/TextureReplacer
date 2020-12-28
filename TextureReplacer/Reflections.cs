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
            private static int currentScript;

            private readonly RenderTexture envMap;
            private readonly Transform transform;
            private readonly bool isEva;
            private readonly int interval;

            private int counter;
            private int currentFace;
            private bool isActive = true;

            public Script(Component part, int updateInterval)
            {
                envMap = new RenderTexture(reflectionResolution, reflectionResolution, 24)
                {
                    dimension = TextureDimension.Cube,
                    wrapMode  = TextureWrapMode.Clamp
                };

                transform = part.transform;
                isEva     = part.GetComponent<KerbalEVA>() != null;

                if (isEva)
                {
                    SkinnedMeshRenderer visor = transform.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .FirstOrDefault(m => m.name == "visor" || m.name == "mesh_female_kerbalAstronaut01_visor");

                    if (visor != null)
                    {
                        Material material = visor.material;

                        material.shader = Instance.visorShader;
                        material.SetTexture(Util.CubeProperty, envMap);
                        material.SetColor(Util.ReflectColorProperty, visorReflectionColour);
                    }
                }

                interval    = updateInterval;
                counter     = Util.Random.Next(updateInterval);
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

                if (reflectiveShader != null)
                {
                    material.shader = reflectiveShader;
                    material.SetTexture(Util.CubeProperty, envMap);
                    material.SetColor(Util.ReflectColorProperty, reflectionColour);
                    return true;
                }

                return false;
            }

            public void SetActive(bool value)
            {
                if (!isActive && value)
                {
                    Update(true);
                }

                isActive = value;
            }

            public static void UpdateScripts()
            {
                if (Scripts.Count == 0 || Time.frameCount % reflectionInterval != 0)
                {
                    return;
                }

                currentScript %= Scripts.Count;

                int startScript = currentScript;
                do
                {
                    Script script = Scripts[currentScript];
                    currentScript = (currentScript + 1) % Scripts.Count;

                    if (script.isActive)
                    {
                        script.counter = (script.counter + 1) % script.interval;

                        if (script.counter == 0)
                        {
                            script.Update(false);
                            break;
                        }
                    }
                }
                while (currentScript != startScript);
            }

            private void Update(bool force)
            {
                int faceMask = force ? 0x3f : 1 << currentFace;

                Transform cameraTransform = camera.transform;

                // Skybox.
                cameraTransform.position = GalaxyCubeControl.Instance.transform.position;
                camera.farClipPlane      = 100.0f;
                camera.cullingMask       = 1 << 18;
                camera.RenderToCubemap(envMap, faceMask);

                // Scaled space.
                cameraTransform.position = ScaledSpace.Instance.transform.position;
                camera.farClipPlane      = 3.0e7f;
                camera.cullingMask       = (1 << 9) | (1 << 10) | (1 << 23);
                camera.RenderToCubemap(envMap, faceMask);

                // Scene.
                cameraTransform.position = isEva ? transform.position + 0.4f * transform.up : transform.position;
                camera.farClipPlane      = 60000.0f;
                camera.cullingMask       = (1 << 0) | (1 << 15) | (1 << 17);
                camera.RenderToCubemap(envMap, faceMask);

                currentFace = (currentFace + 1) % 6;
            }
        }

        // Instance.
        public static Reflections Instance { get; private set; }

        // Print names of meshes and their shaders in parts with TRReflection module.
        public bool LogReflectiveMeshes { get; private set; }

        // Reflection type.
        public Type ReflectionType { get; set; }

        private static readonly Log log = new Log(nameof(Reflections));

        private static readonly float[] CullDistances =
        {
            100.0f, // 0 - parts (world space)
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            10.0f,  // 9 - atmosphere (scaled space)
            0.0f,   // 10 - celestial bodies (scaled space)
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   // 15 - buildings & terrain (world space)
            0.0f,   //
            100.0f, // 17 - kerbals (world space)
            0.0f,   // 18 - skybox (galaxy cube space)
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   // 23 - sun (scaled space)
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f,   //
            0.0f    //
        };

        // Reflective shader map.
        private static readonly string[,] ShaderNameMap =
        {
            {"KSP/Diffuse", "Reflective/Bumped Diffuse"},
            {"KSP/Specular", "Reflective/Bumped Diffuse"},
            {"KSP/Bumped", "Reflective/Bumped Diffuse"},
            {"KSP/Bumped Specular", "Reflective/Bumped Diffuse"},
            {"KSP/Alpha/Translucent", "Reflective/Bumped Diffuse"},
            {"KSP/Alpha/Translucent Specular", "Reflective/Bumped Diffuse"}
        };

        // Reflection camera.
        private static Camera camera;

        // Real reflection resolution.
        private static int reflectionResolution = 256;

        private static int reflectionInterval = 4;

        // Reflection colour.
        private static Color visorReflectionColour = Color.white;

        private readonly Dictionary<Shader, Shader> shaderMap = new Dictionary<Shader, Shader>();

        // Reflective visor shader.
        private Shader visorShader;

        public static Shader LoadShaderFromAsset(string path)
        {
            string shadersFileName = Application.platform switch
            {
                RuntimePlatform.WindowsPlayer => "shaders.windows",
                RuntimePlatform.OSXPlayer     => "shaders.osx",
                _                             => "shaders.linux"
            };
            string shadersPath = IOUtils.GetFilePathFor(typeof(Reflections), shadersFileName);

            AssetBundle shadersBundle = null;
            Shader      visorShader   = null;
            try
            {
                shadersBundle = AssetBundle.LoadFromFile(shadersPath);
                if (shadersBundle == null)
                {
                    log.Print("Failed to load shader asset file: {0}", shadersPath);
                }
                else
                {
                    visorShader = shadersBundle.LoadAsset<Shader>(path);
                    if (visorShader == null)
                    {
                        log.Print("Visor shader missing in the asset file");
                    }
                }
            }
            catch (System.Exception ex)
            {
                log.Print("Visor shader loading failed: {0}", ex);
            }
            finally
            {
                if (shadersBundle != null)
                {
                    shadersBundle.Unload(false);
                }
            }
            return visorShader;
        }

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

            Util.Parse(rootNode.GetValue("reflectionResolution"), ref reflectionResolution);
            Util.Parse(rootNode.GetValue("reflectionInterval"), ref reflectionInterval);
            Util.Parse(rootNode.GetValue("visorReflectionColour"), ref visorReflectionColour);
            Util.Parse(rootNode.GetValue("logReflectiveMeshes"), ref logReflectiveMeshes);

            LogReflectiveMeshes = logReflectiveMeshes;
        }

        /// <summary>
        /// Post-load initialisation.
        /// </summary>
        public void Load()
        {
            visorShader = LoadShaderFromAsset("assets/Visor.shader");

            for (int i = 0; i < ShaderNameMap.GetLength(0); ++i)
            {
                Shader original   = Shader.Find(ShaderNameMap[i, 0]);
                Shader reflective = Shader.Find(ShaderNameMap[i, 1]);

                if (original == null)
                {
                    log.Print("Shader \"{0}\" missing", ShaderNameMap[i, 0]);
                }
                else if (reflective == null)
                {
                    log.Print("Shader \"{0}\" missing", ShaderNameMap[i, 1]);
                }
                else
                {
                    shaderMap[original] = reflective;
                }
            }
        }

        public void OnLoadScenario(ConfigNode node)
        {
            var type = Type.Real;
            Util.Parse(node.GetValue("reflectionType"), ref type);
            ReflectionType = type;
        }

        public void OnSaveScenario(ConfigNode node)
        {
            node.AddValue("reflectionType", ReflectionType);
        }

        private void Destroy()
        {
            if (camera != null)
            {
                Object.DestroyImmediate(camera.gameObject);
            }

            if (visorShader != null)
            {
                Object.DestroyImmediate(visorShader);
            }
        }

        private static void EnsureCamera()
        {
            if (camera != null)
            {
                return;
            }

            camera            = new GameObject("TRReflectionCamera", typeof(Camera)).GetComponent<Camera>();
            camera.enabled    = false;
            camera.clearFlags = CameraClearFlags.Depth;
            // Any smaller number and visors will reflect internals of helmets.
            camera.nearClipPlane      = 0.4f;
            camera.layerCullDistances = CullDistances;
        }

        /// <summary>
        /// Get reflective version of a shader.
        /// </summary>
        private Shader ToReflective(Shader shader)
        {
            shaderMap.TryGetValue(shader, out Shader newShader);
            return newShader;
        }
    }
}
