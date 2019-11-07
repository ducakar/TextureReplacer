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

//#define TR_ENABLE_TEXTURE_EXPORTING

using System;
using System.Collections.Generic;
using UnityEngine;

#if TR_ENABLE_TEXTURE_EXPORTING
using System.IO;
#endif

namespace TextureReplacer
{
  internal static class Util
  {
    private static readonly char[] ConfigDelimiters = {' ', '\t', ','};

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

    public static void Parse<T>(string name, ref T variable)
    {
      try {
        variable = (T) Enum.Parse(typeof(T), name, true);
      } catch (ArgumentException) { } catch (OverflowException) { }
    }

    public static void Parse(string name, ref Color variable)
    {
      if (name == null) {
        return;
      }

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

    /// <summary>
    /// Add all space-or-comma-separated values from listStrings to jointList.
    /// </summary>
    public static void JoinLists(IEnumerable<string> listStrings, ICollection<string> jointList)
    {
      foreach (string listString in listStrings) {
        foreach (string item in SplitConfigValue(listString)) {
          if (!jointList.Contains(item)) {
            jointList.Add(item);
          }
        }
      }
    }

    /// <summary>
    /// Print transform node with its attached objects.
    /// </summary>
    private static void LogTransform(Transform tf, string indent = "")
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
            Debug.Log(indent + "   mainTex:  " + r.material.GetTexture(MainTexProperty));
          }
          if (r.material.HasProperty(BumpMapProperty)) {
            Debug.Log(indent + "   bumpMap:  " + r.material.GetTexture(BumpMapProperty));
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

#if TR_ENABLE_TEXTURE_EXPORTING
    // Development utilities.
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
      var targetTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
      var renderTex = new RenderTexture(texture.width, texture.height, 32);
      RenderTexture originalRenderTex = RenderTexture.active;

      Graphics.Blit(texture, renderTex);
      RenderTexture.active = renderTex;
      targetTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
      RenderTexture.active = originalRenderTex;

      byte[] data = targetTex.EncodeToPNG();
      using var fs = new FileStream(path, FileMode.Create);
      fs.Write(data, 0, data.Length);
    }

    private static bool isDefaultVeteranIvaExported;
    private static bool isDefaultIvaExported;
    private static bool isDefaultEvaExported;
    private static bool isVintageVeteranIvaExported;
    private static bool isVintageIvaExported;
    private static bool isVintageEvaExported;

    public static void DumpSuitOnce(ProtoCrewMember kerbal, Texture texture, bool isEva, string dir)
    {
      if (isEva) {
        if (kerbal.suit == ProtoCrewMember.KerbalSuit.Vintage) {
          if (!isVintageEvaExported) {
            DumpToPNG(texture, dir + "EVAtexture.vintage.png");
            isVintageEvaExported = true;
          }
        } else {
          if (!isDefaultEvaExported) {
            DumpToPNG(texture, dir + "EVAtexture.png");
            isDefaultEvaExported = true;
          }
        }
      } else {
        if (kerbal.suit == ProtoCrewMember.KerbalSuit.Vintage) {
          if (kerbal.veteran) {
            if (!isVintageVeteranIvaExported) {
              DumpToPNG(texture, dir + "kerbalMain.vintage.png");
              isVintageVeteranIvaExported = true;
            }
          } else {
            if (!isVintageIvaExported) {
              DumpToPNG(texture, dir + "kerbalMainGrey.vintage.png");
              isVintageIvaExported = true;
            }
          }
        } else {
          if (kerbal.veteran) {
            if (!isDefaultVeteranIvaExported) {
              DumpToPNG(texture, dir + "kerbalMain.png");
              isDefaultVeteranIvaExported = true;
            }
          } else {
            if (!isDefaultIvaExported) {
              DumpToPNG(texture, dir + "kerbalMainGrey.png");
              isDefaultIvaExported = true;
            }
          }
        }
      }
    }
#endif
  }
}
