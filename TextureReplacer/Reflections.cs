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
    // Visor reflection feature.
    bool isVisorReflectionEnabled = true;
    // Reflection colour.
    Color visorReflectionColour = Color.white;
    // Print names of meshes and their shaders in parts with TRReflection module.
    public bool logReflectiveMeshes = false;
    // Reflective shader.
    public Shader shader = null;
    // Environment map.
    public Cubemap envMap = null;
    // Instance.
    public static Reflections instance = null;

    /**
     * Get reflective version of a shader.
     */
    public Shader toReflective(Shader shader)
    {
      Shader newShader;
      shaderMap.TryGetValue(shader, out newShader);
      return newShader;
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      Util.parse(rootNode.GetValue("logReflectiveMeshes"), ref logReflectiveMeshes);
      Util.parse(rootNode.GetValue("isVisorReflectionEnabled"), ref isVisorReflectionEnabled);
      Util.parse(rootNode.GetValue("visorReflectionColour"), ref visorReflectionColour);
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
        Util.log("Some environment map faces are missing. Reflections disabled.");
      }
      else
      {
        int envMapSize = envMapFaces[0].width;

        if (envMapFaces.Any(t => t.width != envMapSize || t.height != envMapSize))
        {
          Util.log("Environment map faces have different dimensions. Reflections disabled.");
        }
        else if (envMapFaces.Any(t => !Util.isPow2(t.width) || !Util.isPow2(t.height)))
        {
          Util.log("Environment map dimensions are not powers of two. Reflections disabled.");
        }
        else
        {
          try
          {
            envMap = new Cubemap(envMapSize, TextureFormat.RGB24, true);
            envMap.wrapMode = TextureWrapMode.Clamp;
            envMap.SetPixels(envMapFaces[0].GetPixels(), CubemapFace.PositiveX);
            envMap.SetPixels(envMapFaces[1].GetPixels(), CubemapFace.NegativeX);
            envMap.SetPixels(envMapFaces[2].GetPixels(), CubemapFace.PositiveY);
            envMap.SetPixels(envMapFaces[3].GetPixels(), CubemapFace.NegativeY);
            envMap.SetPixels(envMapFaces[4].GetPixels(), CubemapFace.PositiveZ);
            envMap.SetPixels(envMapFaces[5].GetPixels(), CubemapFace.NegativeZ);
            envMap.Apply(true, true);

            Util.log("Environment map cube texture generated.");
          }
          catch (UnityException)
          {
            envMap = null;
            Util.log("Environment map texture is not readable. Reflections disabled.");
          }
        }
      }

      foreach (Texture2D face in envMapFaces)
      {
        if (face != null)
          GameDatabase.Instance.RemoveTexture(face.name);
      }

      if (envMap == null)
      {
        destroy();
        return;
      }

      string shaderPath = KSP.IO.IOUtils.GetFilePathFor(GetType(), "Visor.shader");
      string shaderSource = null;

      try
      {
        shaderSource = File.ReadAllText(shaderPath);
        shaderMaterial = new Material(shaderSource);
        shader = shaderMaterial.shader;

        Util.log("Visor shader sucessfully compiled.");
      }
      catch (System.IO.IsolatedStorage.IsolatedStorageException)
      {
        Util.log("Visor shader loading failed. Reflections disabled.");
        destroy();
        return;
      }

      if (isVisorReflectionEnabled)
      {
        // Set visor texture and reflection on proto-IVA and -EVA Kerbal.
        foreach (SkinnedMeshRenderer smr in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>())
        {
          if (smr.name != "visor")
            continue;

          bool isEva = smr.transform.parent.parent.parent.parent == null;
          if (isEva)
          {
            smr.sharedMaterial.shader = shader;
            smr.sharedMaterial.SetTexture(Util.CUBE_PROPERTY, envMap);
            smr.sharedMaterial.SetColor(Util.REFLECT_COLOR_PROPERTY, visorReflectionColour);
          }
        }
      }

      for (int i = 0; i < SHADER_MAP.GetLength(0); ++i)
      {
        Shader original = Shader.Find(SHADER_MAP[i, 0]);
        Shader reflective = SHADER_MAP[i, 1] == shader.name ?
                            shader : Shader.Find(SHADER_MAP[i, 1]);

        if (original == null)
          Util.log("Shader \"{0}\" missing", SHADER_MAP[i, 0]);
        else if (reflective == null)
          Util.log("Shader \"{0}\" missing", SHADER_MAP[i, 1]);
        else
          shaderMap.Add(original, reflective);
      }
    }

    public void destroy()
    {
      if (envMap != null)
        Object.Destroy(envMap);

      if (shaderMaterial != null)
        Object.Destroy(shaderMaterial);
    }
  }
}
