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

using System.Linq;
using System.IO;
using UnityEngine;

namespace TextureReplacer
{
  class Reflections
  {
    public static readonly string DIR_ENVMAP = Util.DIR + "EnvMap/";
    // Reflective shader material.
    Material shaderMaterial = null;
    // Visor reflection feature.
    bool isVisorReflectionEnabled = true;
    // Reflection colour.
    Color visorReflectionColour = new Color(1.0f, 1.0f, 1.0f);
    // Instance.
    public static Reflections instance = null;
    // Reflective shader.
    public Shader shader = null;
    // Environment map.
    public Cubemap envMap = null;

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      string sIsVisorReflectionEnabled = rootNode.GetValue("isVisorReflectionEnabled");
      if (sIsVisorReflectionEnabled != null)
        bool.TryParse(sIsVisorReflectionEnabled, out isVisorReflectionEnabled);

      string sVisorReflectionColour = rootNode.GetValue("visorReflectionColour");
      if (sVisorReflectionColour != null)
      {
        string[] components = Util.splitConfigValue(sVisorReflectionColour);
        if (components.Length != 3)
        {
          Util.log("visorReplectionColour must have exactly 3 components");
        }
        else
        {
          float.TryParse(components[0], out visorReflectionColour.r);
          float.TryParse(components[1], out visorReflectionColour.g);
          float.TryParse(components[2], out visorReflectionColour.b);
        }
      }
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
        if (texture == null || !texture.name.StartsWith(DIR_ENVMAP))
          continue;

        string originalName = texture.name.Substring(DIR_ENVMAP.Length);

        switch (originalName)
        {
          case "PositiveX":
            envMapFaces[0] = texture;
            Util.log("Environment map +x -> {0}", texture.name);
            break;
          case "NegativeX":
            envMapFaces[1] = texture;
            Util.log("Environment map -x -> {0}", texture.name);
            break;
          case "PositiveY":
            envMapFaces[2] = texture;
            Util.log("Environment map +y -> {0}", texture.name);
            break;
          case "NegativeY":
            envMapFaces[3] = texture;
            Util.log("Environment map -y -> {0}", texture.name);
            break;
          case "PositiveZ":
            envMapFaces[4] = texture;
            Util.log("Environment map +z -> {0}", texture.name);
            break;
          case "NegativeZ":
            envMapFaces[5] = texture;
            Util.log("Environment map -z -> {0}", texture.name);
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

      if (!isVisorReflectionEnabled)
        return;

      // Set visor texture and reflection on proto-IVA and -EVA Kerbal.
      foreach (SkinnedMeshRenderer smr
               in Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)))
      {
        if (smr.name != "visor")
          continue;

        bool isEva = smr.transform.parent.parent.parent.parent == null;
        if (isEva)
        {
          smr.sharedMaterial.shader = shader;
          smr.sharedMaterial.SetColor("_ReflectColor", visorReflectionColour);
          smr.sharedMaterial.SetTexture("_Cube", envMap);
        }
      }
    }

    public void destroy()
    {
      if (envMap != null)
        Resources.UnloadAsset(envMap);

      if (shaderMaterial != null)
        Resources.UnloadAsset(shaderMaterial);

      envMap = null;
      shader = null;
      shaderMaterial = null;
      isVisorReflectionEnabled = false;
    }
  }
}
