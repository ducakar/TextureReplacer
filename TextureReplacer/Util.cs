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
using System.IO;
using UnityEngine;

namespace TextureReplacer
{
  static class Util
  {
    static readonly char[] ConfigDelimiters = { ' ', '\t', ',' };

    public const string Directory = "TextureReplacer/";
    public static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
    public static readonly int MainTextureProperty = Shader.PropertyToID("_MainTexture");
    public static readonly int BumpMapProperty = Shader.PropertyToID("_BumpMap");
    public static readonly int CubeProperty = Shader.PropertyToID("_Cube");
    public static readonly int ReflectColorProperty = Shader.PropertyToID("_ReflectColor");
    public static readonly System.Random Random = new System.Random();

    /// <summary>
    /// Split a space- and/or comma-separated configuration file value into its tokens.
    /// </summary>
    public static string[] SplitConfigValue(string value)
    {
      return value.Split(ConfigDelimiters, StringSplitOptions.RemoveEmptyEntries);
    }

    public static void Parse(string name, ref bool variable)
    {
      if (bool.TryParse(name, out bool value)) {
        variable = value;
      }
    }

    public static void Parse(string name, ref int variable)
    {
      if (int.TryParse(name, out int value)) {
        variable = value;
      }
    }

    public static void Parse(string name, ref double variable)
    {
      if (double.TryParse(name, out double value)) {
        variable = value;
      }
    }

    public static void Parse<T>(string name, ref T variable)
    {
      try {
        variable = (T)Enum.Parse(typeof(T), name, true);
      } catch (ArgumentException) {
      } catch (OverflowException) {
      }
    }

    public static void Parse(string name, ref Color variable)
    {
      if (name != null) {
        string[] components = SplitConfigValue(name);
        if (components.Length >= 3) {
          float.TryParse(components[0], out variable.r);
          float.TryParse(components[1], out variable.g);
          float.TryParse(components[2], out variable.b);
        }
        if (components.Length >= 4) {
          float.TryParse(components[3], out variable.a);
        }
      }
    }

    /// <summary>
    /// Add all space-or-comma-separated values from listInstances strings to jointList.
    /// </summary>
    public static void AddLists(string[] listInstances, ICollection<string> jointList)
    {
      foreach (string listInstance in listInstances) {
        foreach (string item in SplitConfigValue(listInstance)) {
          if (!jointList.Contains(item)) {
            jointList.Add(item);
          }
        }
      }
    }

    /// <summary>
    /// Print transform node with its attached objects.
    /// </summary>
    public static void LogTransform(Transform tf, string indent = "")
    {
      if (tf.gameObject != null) {
        Debug.Log(indent + "* " + tf.gameObject.name + ": " + tf.gameObject.GetType());
      }
      foreach (Component c in tf.GetComponents<Component>()) {
        Debug.Log(indent + " - " + c);

        if (c is Renderer r) {
          Debug.Log(indent + "   material: " + r.material.name);
          Debug.Log(indent + "   shader:   " + r.material.shader);

          if (r.material.HasProperty(MainTexProperty)) {
            Debug.Log(indent + "   maintex:  " + r.material.GetTexture(MainTexProperty));
          }
          if (r.material.HasProperty(BumpMapProperty)) {
            Debug.Log(indent + "   bumpmap:  " + r.material.GetTexture(BumpMapProperty));
          }
        }
      }
    }

    /// <summary>
    /// Print hierarchy under a transform.
    /// </summary>
    public static void LogDownHierarchy(Transform tf, string indent = "")
    {
      LogTransform(tf, indent);

      for (int i = 0; i < tf.childCount; ++i) {
        LogDownHierarchy(tf.GetChild(i), indent + "  ");
      }
    }

    // Development utilities.
#if false
    /// <summary>
    /// Print hierarchy from a transform up to the root.
    /// </summary>
    public static void LogUpHierarchy(Transform tf, string indent = "")
    {
      for (; tf != null; tf = tf.parent) {
        LogTransform(tf, indent);
      }
    }

    /// <summary>
    /// Export any texture (even if not loaded in RAM) as a PNG.
    /// </summary>
    public static void DumpToPNG(Texture texture, string path)
    {
      Texture2D targetTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
      RenderTexture renderTex = new RenderTexture(texture.width, texture.height, 32);
      RenderTexture originalRenderTex = RenderTexture.active;

      Graphics.Blit(texture, renderTex);
      RenderTexture.active = renderTex;
      targetTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
      RenderTexture.active = originalRenderTex;

      byte[] data = targetTex.EncodeToPNG();
      using (FileStream fs = new FileStream(path, FileMode.Create)) {
        fs.Write(data, 0, data.Length);
      }
    }

    static bool isDefaultVeteranIvaExported;
    static bool isDefaultIvaExported;
    static bool isDefaultEvaExported;
    static bool isVintageVeteranIvaExported;
    static bool isVintageIvaExported;
    static bool isVintageEvaExported;

    public static void DumpSuitOnce(ProtoCrewMember kerbal, Texture texture, bool isEva, string dir)
    {
      if (isEva) {
        if (kerbal.suit == ProtoCrewMember.KerbalSuit.Vintage) {
          if (!isVintageEvaExported) {
            Util.DumpToPNG(texture, dir + "EVAtexture.vintage.png");
            isVintageEvaExported = true;
          }
        } else {
          if (!isDefaultEvaExported) {
            Util.DumpToPNG(texture, dir + "EVAtexture.png");
            isDefaultEvaExported = true;
          }
        }
      } else {
        if (kerbal.suit == ProtoCrewMember.KerbalSuit.Vintage) {
          if (kerbal.veteran) {
            if (!isVintageVeteranIvaExported) {
              Util.DumpToPNG(texture, dir + "kerbalMain.vintage.png");
              isVintageVeteranIvaExported = true;
            }
          } else {
            if (!isVintageIvaExported) {
              Util.DumpToPNG(texture, dir + "kerbalMainGrey.vintage.png");
              isVintageIvaExported = true;
            }
          }
        } else {
          if (kerbal.veteran) {
            if (!isDefaultVeteranIvaExported) {
              Util.DumpToPNG(texture, dir + "kerbalMain.png");
              isDefaultVeteranIvaExported = true;
            }
          } else {
            if (!isDefaultIvaExported) {
              Util.DumpToPNG(texture, dir + "kerbalMainGrey.png");
              isDefaultIvaExported = true;
            }
          }
        }
      }
    }
#endif
  }
}
