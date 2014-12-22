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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TextureReplacer
{
  class Reflections
  {
    public enum Type
    {
      NONE,
      STATIC,
      REAL
    }

    public class Script
    {
      readonly RenderTexture envMap = null;
      readonly Transform partTransform = null;
      readonly int frameCountBias = Util.random.Next(instance.reflectionInterval);
      int currentFace = Util.random.Next(6);

      void updateFaces(int faceMask)
      {
        Transform spaceTransf = ScaledSpace.Instance.transform;
        Vector3 spacePos = spaceTransf.position;
        Vector3 partScale = partTransform.localScale;

        // Scaled space is neccessary to render skybox and high-altitude models of celestial bodies.
        spaceTransf.position = partTransform.position;
        // We set scale to zero to "hide" the part since it shouldn't reflect itself.
        partTransform.localScale = Vector3.zero;

        instance.camera.transform.root.position = partTransform.position;
        instance.camera.RenderToCubemap(envMap, faceMask);

        partTransform.localScale = partScale;
        spaceTransf.position = spacePos;
      }

      public Script(Part part)
      {
        instance.ensureCamera();

        envMap = new RenderTexture(instance.reflectionResolution,
                                   instance.reflectionResolution,
                                   24);
        envMap.hideFlags = HideFlags.HideAndDontSave;
        envMap.wrapMode = TextureWrapMode.Clamp;
        envMap.isCubemap = true;

        partTransform = part.transform;

        updateFaces(0x3f);
      }

      public bool apply(Material material, Color reflectionColour)
      {
        Shader reflectiveShader = instance.toReflective(material.shader);

        if (reflectiveShader != null)
        {
          material.shader = reflectiveShader;
          material.SetTexture(Util.CUBE_PROPERTY, envMap);
          material.SetColor(Util.REFLECT_COLOR_PROPERTY, reflectionColour);
          return true;
        }
        return false;
      }

      public void applyVisor(Material material)
      {
        material.shader = instance.visorShader;
        material.SetTexture(Util.CUBE_PROPERTY, envMap);
        material.SetColor(Util.REFLECT_COLOR_PROPERTY, instance.visorReflectionColour);
      }

      public void destroy()
      {
        Object.DestroyImmediate(envMap);
      }

      public void update(bool force = false)
      {
        if (force)
        {
          updateFaces(0x3f);
        }
        else if ((Time.frameCount + frameCountBias) % Reflections.instance.reflectionInterval == 0)
        {
          updateFaces(1 << currentFace);
          currentFace = (currentFace + 1) % 6;
        }
      }
    }

    public static readonly string DIR_ENVMAP = Util.DIR + "EnvMap/";
    // Reflective shader map.
    static readonly string[,] SHADER_MAP = {
      { "KSP/Diffuse", "Reflective/Bumped Diffuse" },
      { "KSP/Specular", "Reflective/Bumped Diffuse" },
      { "KSP/Bumped", "Reflective/Bumped Diffuse" },
      { "KSP/Bumped Specular", "Reflective/Bumped Diffuse" },
      { "KSP/Alpha/Translucent", "TR/Visor" },
      { "KSP/Alpha/Translucent Specular", "TR/Visor" }
    };
    readonly Dictionary<Shader, Shader> shaderMap = new Dictionary<Shader, Shader>();
    // Reflective shader material.
    Material shaderMaterial = null;
    // Reflection camera.
    Camera camera = null;
    // Environment map textures.
    public Cubemap staticEnvMap = null;
    // Reflection type.
    public Type reflectionType = Type.REAL;
    // Real reflection resolution.
    int reflectionResolution = 64;
    // Interval in frames for updating environment map faces.
    int reflectionInterval = 4;
    // Visor reflection feature.
    bool isVisorReflectionEnabled = true;
    // Reflection colour.
    Color visorReflectionColour = new Color(0.5f, 0.5f, 0.5f);
    // Print names of meshes and their shaders in parts with TRReflection module.
    public bool logReflectiveMeshes = false;
    // Reflective shader.
    public Shader visorShader = null;
    // Instance.
    public static Reflections instance = null;

    void ensureCamera()
    {
      if (camera == null)
      {
        camera = new GameObject("ReflectionCamera", new[] { typeof(Camera) }).camera;
        camera.enabled = false;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 3.0e7f;

        // Render layers:
        //  0 - parts
        //  1 - thrusters
        //  9 - sky/atmosphere
        // 10 - scaled space
        // 12 - navball
        // 15 - buildings, terrain
        // 18 - skybox
        // 23 - sun
        camera.cullingMask = 1 << 0 | 1 << 9 | 1 << 10 | 1 << 15 | 1 << 18 | 1 << 23;

        // Cull everything but scaled space at 100 m.
        float[] cullDistances = new float[32];
        cullDistances[0] = 100.0f;
        cullDistances[9] = 100.0f;
        cullDistances[15] = 100.0f;

        camera.layerCullDistances = cullDistances;
      }
    }

    /**
     * Get reflective version of a shader.
     */
    public Shader toReflective(Shader shader)
    {
      Shader newShader;
      shaderMap.TryGetValue(shader, out newShader);
      return newShader;
    }

    public bool applyStatic(Material material, Color reflectionColour)
    {
      Shader reflectiveShader = instance.toReflective(material.shader);

      if (reflectiveShader != null)
      {
        material.shader = reflectiveShader;
        material.SetTexture(Util.CUBE_PROPERTY, staticEnvMap);
        material.SetColor(Util.REFLECT_COLOR_PROPERTY, reflectionColour);
        return true;
      }
      return false;
    }

    public void setReflectionType(Type type)
    {
      if (type == Type.STATIC && staticEnvMap == null)
        type = Type.NONE;

      reflectionType = type;

      // Set visor texture and reflection on proto-EVA Kerbal.
      foreach (SkinnedMeshRenderer smr in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>())
      {
        if (smr.name != "visor")
          continue;

        bool isEva = smr.transform.root.GetComponent<KerbalEVA>() != null;
        if (isEva)
        {
          Material material = smr.sharedMaterial;
          bool enableStatic = isVisorReflectionEnabled && reflectionType == Type.STATIC;

          // We apply visor shader for real reflections later, through TREvaModule since we don't
          // want corrupted reflections in the main menu.
          material.shader = enableStatic ? visorShader : Util.transparentSpecularShader;
          material.SetTexture(Util.CUBE_PROPERTY, enableStatic ? staticEnvMap : null);
          material.SetColor(Util.REFLECT_COLOR_PROPERTY, visorReflectionColour);
        }
      }
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      Util.parse(rootNode.GetValue("reflectionType"), ref reflectionType);
      Util.parse(rootNode.GetValue("reflectionResolution"), ref reflectionResolution);
      Util.parse(rootNode.GetValue("reflectionInterval"), ref reflectionInterval);
      Util.parse(rootNode.GetValue("isVisorReflectionEnabled"), ref isVisorReflectionEnabled);
      Util.parse(rootNode.GetValue("visorReflectionColour"), ref visorReflectionColour);
      Util.parse(rootNode.GetValue("logReflectiveMeshes"), ref logReflectiveMeshes);
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      Texture2D[] envMapFaces = new Texture2D[6];

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null
            || !texture.name.StartsWith(DIR_ENVMAP, System.StringComparison.Ordinal))
        {
          continue;
        }

        string originalName = texture.name.Substring(DIR_ENVMAP.Length);

        switch (originalName)
        {
          case "PositiveX":
            envMapFaces[0] = texture;
            break;
          case "NegativeX":
            envMapFaces[1] = texture;
            break;
          case "PositiveY":
            envMapFaces[2] = texture;
            break;
          case "NegativeY":
            envMapFaces[3] = texture;
            break;
          case "PositiveZ":
            envMapFaces[4] = texture;
            break;
          case "NegativeZ":
            envMapFaces[5] = texture;
            break;
          default:
            Util.log("Invalid enironment map texture name {0}", texture.name);
            break;
        }
      }

      // Generate generic reflection cube map texture.
      if (envMapFaces.Any(t => t == null))
      {
        Util.log("Some environment map faces are missing. Static reflections disabled.");
      }
      else
      {
        int envMapSize = envMapFaces[0].width;

        if (envMapFaces.Any(t => t.width != envMapSize || t.height != envMapSize)
            || envMapFaces.Any(t => !Util.isPow2(t.width) || !Util.isPow2(t.height)))
        {
          Util.log("Invalid environment map faces. Static reflections disabled.");
        }
        else
        {
          try
          {
            staticEnvMap = new Cubemap(envMapSize, TextureFormat.RGB24, true);
            staticEnvMap.hideFlags = HideFlags.HideAndDontSave;
            staticEnvMap.wrapMode = TextureWrapMode.Clamp;
            staticEnvMap.SetPixels(envMapFaces[0].GetPixels(), CubemapFace.PositiveX);
            staticEnvMap.SetPixels(envMapFaces[1].GetPixels(), CubemapFace.NegativeX);
            staticEnvMap.SetPixels(envMapFaces[2].GetPixels(), CubemapFace.PositiveY);
            staticEnvMap.SetPixels(envMapFaces[3].GetPixels(), CubemapFace.NegativeY);
            staticEnvMap.SetPixels(envMapFaces[4].GetPixels(), CubemapFace.PositiveZ);
            staticEnvMap.SetPixels(envMapFaces[5].GetPixels(), CubemapFace.NegativeZ);
            staticEnvMap.Apply(true, false);

            Util.log("Static environment map cube texture generated.");
          }
          catch (UnityException)
          {
            if (staticEnvMap != null)
              Object.DestroyImmediate(staticEnvMap);

            staticEnvMap = null;

            Util.log("Failed to set up static reflections. Textures not readable?");
          }
        }
      }

      string shaderPath = KSP.IO.IOUtils.GetFilePathFor(GetType(), "Visor.shader");
      string shaderSource = null;

      try
      {
        shaderSource = File.ReadAllText(shaderPath);
        shaderMaterial = new Material(shaderSource);
        visorShader = shaderMaterial.shader;

        Util.log("Visor shader sucessfully compiled.");
      }
      catch (System.IO.IsolatedStorage.IsolatedStorageException)
      {
        isVisorReflectionEnabled = false;

        Util.log("Visor shader loading failed. Visor reflections disabled.");
      }

      for (int i = 0; i < SHADER_MAP.GetLength(0); ++i)
      {
        Shader original = Shader.Find(SHADER_MAP[i, 0]);
        Shader reflective = SHADER_MAP[i, 1] == visorShader.name ?
                            visorShader : Shader.Find(SHADER_MAP[i, 1]);

        if (original == null)
          Util.log("Shader \"{0}\" missing", SHADER_MAP[i, 0]);
        else if (reflective == null)
          Util.log("Shader \"{0}\" missing", SHADER_MAP[i, 1]);
        else
          shaderMap.Add(original, reflective);
      }

      setReflectionType(reflectionType);
    }

    public void destroy()
    {
      if (staticEnvMap != null)
        Object.DestroyImmediate(staticEnvMap);

      if (camera != null)
        Object.DestroyImmediate(camera);

      if (shaderMaterial != null)
        Object.DestroyImmediate(shaderMaterial);
    }

    public void loadScenario(ConfigNode node)
    {
      Type type = reflectionType;
      Util.parse(node.GetValue("reflectionType"), ref type);

      if (type != reflectionType)
        setReflectionType(type);
    }

    public void saveScenario(ConfigNode node)
    {
      node.AddValue("reflectionType", reflectionType);
    }
  }
}
