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
using System.Reflection;
using UnityEngine;

namespace TextureReplacer
{
  public class Replacer
  {
    enum VisorReflection
    {
      NEVER,
      ALWAYS,
      EVA_ONLY
    }

    private static readonly string DIR_TEXTURES = Main.DIR_PREFIX + "Default/";
    private static readonly string DIR_ENVMAP = Main.DIR_PREFIX + "EnvMap/";
    // General texture replacements.
    private Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
    // Reflective shader material.
    private Material shaderMaterial = null;
    // Environment map.
    private Cubemap envMap = null;
    // Reflective visor parameters.
    private VisorReflection visorReflection = VisorReflection.EVA_ONLY;
    // Visor colour specifies intensity and colour of the reflection.
    private Color visorReflectionColour = new Color(0.5f, 0.5f, 0.5f);
    // Generic texture replacement parameters.
    private int lastMaterialCount = 0;
    // General replacement has to be performed for more than one frame when a scene switch occurs
    // since textures and models may also be loaded with a few frame lag. `updateCounter` specifies
    // for how many frames it should run.
    private int updateCounter = 0;
    // Instance.
    public static Replacer instance = null;
    // Reflective shader.
    public Shader reflectiveShader = null;

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    private static void log(string s, params object[] args)
    {
      Debug.Log("[TR.Replacer] " + String.Format(s, args));
    }

    /**
     * General texture replacement step.
     */
    private void replaceTextures(Material[] materials)
    {
      foreach (Material material in materials)
      {
        Texture texture = material.mainTexture;
        if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp"))
          continue;

        Texture2D newTexture = null;
        mappedTextures.TryGetValue(texture.name, out newTexture);

        if (newTexture == null)
        {
          // Set trilinear filter. Trilinear filter is also set in initialisation but it only
          // iterates through textures in `GameData/`.
          if (texture.filterMode == FilterMode.Bilinear)
            texture.filterMode = FilterMode.Trilinear;

          continue;
        }
        else if (newTexture != texture)
        {
          material.mainTexture = newTexture;
          Resources.UnloadAsset(texture);
        }

        Texture normalMap = material.GetTexture("_BumpMap");
        if (normalMap == null)
          continue;

        Texture2D newNormalMap = null;
        mappedTextures.TryGetValue(normalMap.name, out newNormalMap);

        if (newNormalMap != null && newNormalMap != normalMap)
        {
          material.SetTexture("_BumpMap", newNormalMap);
          Resources.UnloadAsset(normalMap);
        }
      }
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public Replacer(ConfigNode rootNode)
    {
      string sVisorReflection = rootNode.GetValue("visorReflection");
      if (sVisorReflection != null)
      {
        if (sVisorReflection == "always")
          visorReflection = VisorReflection.ALWAYS;
        else if (sVisorReflection == "never")
          visorReflection = VisorReflection.NEVER;
        else if (sVisorReflection == "evaOnly")
          visorReflection = VisorReflection.EVA_ONLY;
        else
          log("Invalid value for visorReflection: {0}", sVisorReflection);
      }

      string sVisorReflectionColour = rootNode.GetValue("visorReflectionColour");
      if (sVisorReflectionColour != null)
      {
        string[] components = sVisorReflectionColour.Split(new char[] { ' ', ',' },
                                                           StringSplitOptions.RemoveEmptyEntries);
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

    private bool initialiseEnvMap(Texture2D[] envMapFaces)
    {
      if (envMapFaces.Any(t => t == null))
      {
        log("Some environment map faces are missing. Reflections disabled.");
        return false;
      }
      else
      {
        int envMapSize = envMapFaces[0].width;

        if (envMapFaces.Any(t => t.width != envMapSize || t.height != envMapSize))
        {
          log("Not all environment map faces are of the same dimension. Reflections disabled.");
          return false;
        }
        else if (envMapFaces.Any(t => !Builder.isPow2(t.width) || !Builder.isPow2(t.height)))
        {
          log("Environment map dimensions are not powers of two. Reflections disabled.");
          return false;
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

          return true;
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
        if (texture == null || !texture.name.StartsWith(Main.DIR_PREFIX))
          continue;

        int lastSlash = texture.name.LastIndexOf('/');
        string originalName = texture.name.Substring(lastSlash + 1);

        // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
        // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides
        // the preceding texture.
        if (texture.name == lastTextureName)
        {
          log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
        }
        else if (texture.name.StartsWith(DIR_ENVMAP))
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
        // Add a general texture replacement.
        else if (texture.name.StartsWith(DIR_TEXTURES))
        {
          // This in wrapped inside an 'if' clause just in case if corrupted GameDatabase contains
          // non-consecutive duplicated entries for some strange reason.
          if (!mappedTextures.ContainsKey(originalName))
          {
            if (originalName.StartsWith("GalaxyTex_"))
              texture.wrapMode = TextureWrapMode.Clamp;

            mappedTextures.Add(originalName, texture);

            log("Mapped {0} -> {1}", originalName, texture.name);
          }
        }

        lastTextureName = texture.name;
      }

      // Replace textures (and apply trilinear filter). This doesn't reach some textures like skybox
      // and kerbalMainGrey. Those will be replaced later.
      replaceTextures((Material[]) Resources.FindObjectsOfTypeAll(typeof(Material)));

      string shaderPath = KSP.IO.IOUtils.GetFilePathFor(GetType(), "Visor.shader");
      string shaderSource = null;

      try
      {
        shaderSource = File.ReadAllText(shaderPath);
        shaderMaterial = new Material(shaderSource);
        reflectiveShader = shaderMaterial.shader;
      }
      catch (System.IO.IsolatedStorage.IsolatedStorageException)
      {
        visorReflection = VisorReflection.NEVER;
        log("Visor shader loading failed. Reflections disabled.");
      }

      if (!initialiseEnvMap(envMapFaces))
        visorReflection = VisorReflection.NEVER;

      Texture2D ivaVisorTex = null, evaVisorTex = null;
      mappedTextures.TryGetValue("kerbalVisor", out ivaVisorTex);
      mappedTextures.TryGetValue("EVAvisor", out evaVisorTex);

      // Set visor texture and reflection on proto-IVA and -EVA Kerbal.
      foreach (SkinnedMeshRenderer smr
               in Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)))
      {
        if (smr.name != "visor")
          continue;

        bool isEVA = smr.transform.parent.parent.parent.parent == null;
        Texture2D newTexture = isEVA ? evaVisorTex : ivaVisorTex;

        if (newTexture != null)
        {
          smr.sharedMaterial.color = Color.white;
          smr.sharedMaterial.mainTexture = newTexture;
        }
        if (visorReflection == VisorReflection.ALWAYS
            || (isEVA && visorReflection == VisorReflection.EVA_ONLY))
        {
          smr.sharedMaterial.shader = reflectiveShader;
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
    }

    public void resetScene()
    {
      lastMaterialCount = 0;
      updateCounter = HighLogic.LoadedScene == GameScenes.MAINMENU ? 64 : 16;
    }

    public void updateScene()
    {
      if (updateCounter > 0)
      {
        --updateCounter;

        Material[] materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
        if (materials.Length != lastMaterialCount)
        {
          replaceTextures(materials);
          lastMaterialCount = materials.Length;
        }
      }
    }
  }
}
