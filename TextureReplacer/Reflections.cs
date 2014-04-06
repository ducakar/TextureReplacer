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

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TextureReplacer
{
  public class Reflections
  {
    private static readonly string DIR_ENVMAP = TextureReplacer.DIR + "EnvMap/";
    // Reflective shader material.
    private Material shaderMaterial = null;
    // Visor reflection feature.
    private bool isVisorReflectionEnabled = true;
    // Reflection colour.
    private Color visorReflectionColour = new Color(0.5f, 0.5f, 0.5f);
    // Instance.
    public static Reflections instance = null;
    // Reflective shader.
    public Shader shader = null;
    // Environment map.
    public Cubemap envMap = null;

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    private static void log(string s, params object[] args)
    {
      Debug.Log("[TR.Reflections] " + String.Format(s, args));
    }

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
        string[] components = TextureReplacer.splitConfigValue(sVisorReflectionColour);
        if (components.Length != 3)
        {
          log("visorReplectionColour must have exactly 3 components");
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
      string lastTextureName = "";

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(DIR_ENVMAP))
          continue;

        string originalName = texture.name.Substring(DIR_ENVMAP.Length);

        // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
        // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides
        // the preceding texture.
        if (texture.name == lastTextureName)
        {
          log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
        }
        else
        {
          switch (originalName)
          {
            case "PositiveX":
            {
              envMapFaces[0] = texture;
              log("Environment map +x -> {0}", texture.name);
              break;
            }
            case "NegativeX":
            {
              envMapFaces[1] = texture;
              log("Environment map -x -> {0}", texture.name);
              break;
            }
            case "PositiveY":
            {
              envMapFaces[2] = texture;
              log("Environment map +y -> {0}", texture.name);
              break;
            }
            case "NegativeY":
            {
              envMapFaces[3] = texture;
              log("Environment map -y -> {0}", texture.name);
              break;
            }
            case "PositiveZ":
            {
              envMapFaces[4] = texture;
              log("Environment map +z -> {0}", texture.name);
              break;
            }
            case "NegativeZ":
            {
              envMapFaces[5] = texture;
              log("Environment map -z -> {0}", texture.name);
              break;
            }
            default:
            {
              log("Invalid enironment map texture name {0}", texture.name);
              break;
            }
          }
        }

        lastTextureName = texture.name;
      }

      // Generate generic reflection cube map texture.
      if (envMapFaces.Any(t => t == null))
      {
        log("Some environment map faces are missing. Reflections disabled.");
      }
      else
      {
        int envMapSize = envMapFaces[0].width;

        if (envMapFaces.Any(t => t.width != envMapSize || t.height != envMapSize))
        {
          log("Not all environment map faces are of the same dimension. Reflections disabled.");
        }
        else if (envMapFaces.Any(
                   t => !TextureReplacer.isPow2(t.width) || !TextureReplacer.isPow2(t.height)))
        {
          log("Environment map dimensions are not powers of two. Reflections disabled.");
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
      }
      catch (System.IO.IsolatedStorage.IsolatedStorageException)
      {
        log("Visor shader loading failed. Reflections disabled.");
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
