/*
 * Copyright © 2013-2015 Davorin Učakar
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

#define TR_LOG_HIERARCHY

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TextureReplacer
{
  static class Util
  {
    static readonly char[] ConfigDelimiters = { ' ', '\t', ',' };

    public const string Directory = "TextureReplacer/";
    public static readonly int BumpMapProperty = Shader.PropertyToID("_BumpMap");
    public static readonly int CubeProperty = Shader.PropertyToID("_Cube");
    public static readonly int ReflectColorProperty = Shader.PropertyToID("_ReflectColor");
    public static readonly System.Random Random = new System.Random();

    /**
     * True iff `i` is a power of two.
     */
    public static bool IsPow2(int i)
    {
      return i > 0 && (i & (i - 1)) == 0;
    }

    /**
     * Split a space- and/or comma-separated configuration file value into its tokens.
     */
    public static string[] SplitConfigValue(string value)
    {
      return value.Split(ConfigDelimiters, StringSplitOptions.RemoveEmptyEntries);
    }

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    public static void Log(string s, params object[] args)
    {
      Type callerClass = new StackFrame(1).GetMethod().DeclaringType;
      UnityEngine.Debug.Log("[TR." + callerClass.Name + "] " + String.Format(s, args));
    }

    public static void Parse(string name, ref bool variable)
    {
      bool value;
      if (bool.TryParse(name, out value)) {
        variable = value;
      }
    }

    public static void Parse(string name, ref int variable)
    {
      int value;
      if (int.TryParse(name, out value)) {
        variable = value;
      }
    }

    public static void Parse(string name, ref double variable)
    {
      double value;
      if (double.TryParse(name, out value)) {
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

    public static void Parse(string name, ref bool? variable)
    {
      switch (name) {
        case "always":
          variable = true;
          break;

        case "never":
          variable = false;
          break;

        default:
          variable = null;
          break;
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

    /**
     * Add all space-or-comma-separated values from listInstances strings to jointList.
     */
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

    /**
     * Add all space-or-comma-separated regex values from listInstances strings to jointList.
     */
    public static void AddRELists(string[] listInstances, ICollection<Regex> jointList)
    {
      foreach (string listInstance in listInstances) {
        foreach (string item in SplitConfigValue(listInstance)) {
          jointList.Add(new Regex(item));
        }
      }
    }

    #if TR_LOG_HIERARCHY
    /**
     * Print hierarchy under a fransform.
     */
    public static void LogDownHierarchy(Transform tf, int indent)
    {
      string sIndent = "";
      for (int i = 0; i < indent; ++i) {
        sIndent += "  ";
      }

      if (tf.gameObject != null) {
        UnityEngine.Debug.Log(sIndent + "- " + tf.gameObject.name + ": " + tf.gameObject.GetType());
      }

      foreach (Component c in tf.GetComponents<Component>()) {
        UnityEngine.Debug.Log(sIndent + " * " + c);

        Renderer r = c as Renderer;
        if (r != null) {
          UnityEngine.Debug.Log(sIndent + "   shader:  " + r.material.shader);
          UnityEngine.Debug.Log(sIndent + "   texture: " + r.material.mainTexture);
        }
      }

      for (int i = 0; i < tf.childCount; ++i) {
        LogDownHierarchy(tf.GetChild(i), indent + 1);
      }
    }

    /**
     * Print hierarchy from a transform up to the root.
     */
    public static void LogUpHierarchy(Transform tf)
    {
      for (; tf != null; tf = tf.parent) {
        if (tf.gameObject != null) {
          UnityEngine.Debug.Log("+ " + tf.gameObject.name + ": " + tf.gameObject.GetType());
        }
        foreach (Component c in tf.GetComponents<Component>()) {
          UnityEngine.Debug.Log(" * " + c);

          Renderer r = c as Renderer;
          if (r != null) {
            UnityEngine.Debug.Log("   shader:  " + r.material.shader);
            UnityEngine.Debug.Log("   texture: " + r.material.mainTexture);
          }
        }
      }
    }
    #endif
  }
}
