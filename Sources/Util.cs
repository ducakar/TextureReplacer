/*
 * Copyright © 2013-2016 Davorin Učakar, RangeMachine
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

//#define TR_LOG_HIERARCHY

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TextureReplacer
{
    internal static class Util
    {
        private static readonly char[] CONFIG_DELIMITERS = { ' ', '\t', ',' };
        public static readonly string DIR = "TextureReplacer/";
        public static readonly int BUMPMAP_PROPERTY = Shader.PropertyToID("_BumpMap");
        public static readonly int CUBE_PROPERTY = Shader.PropertyToID("_Cube");
        public static readonly int REFLECT_COLOR_PROPERTY = Shader.PropertyToID("_ReflectColor");
        public static readonly System.Random random = new System.Random();

        /**
         * True iff `i` is a power of two.
         */

        public static bool isPow2(int i)
        {
            return i > 0 && (i & (i - 1)) == 0;
        }

        /**
         * Split a space- and/or comma-separated configuration file value into its tokens.
         */

        public static string[] splitConfigValue(string value)
        {
            return value.Split(CONFIG_DELIMITERS, StringSplitOptions.RemoveEmptyEntries);
        }

        /**
         * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
         */

        public static void log(string s, params object[] args)
        {
            Type callerClass = new StackTrace(1, false).GetFrame(0).GetMethod().DeclaringType;
            UnityEngine.Debug.Log("[TR." + callerClass.Name + "] " + String.Format(s, args));
        }

        public static void parse(string name, ref bool variable)
        {
            bool value;
            if (bool.TryParse(name, out value))
                variable = value;
        }

        public static void parse(string name, ref int variable)
        {
            int value;
            if (int.TryParse(name, out value))
                variable = value;
        }

        public static void parse(string name, ref double variable)
        {
            double value;
            if (double.TryParse(name, out value))
                variable = value;
        }

        public static void parse<E>(string name, ref E variable)
        {
            try
            {
                variable = (E)Enum.Parse(typeof(E), name, true);
            }
            catch (ArgumentException)
            {
            }
            catch (OverflowException)
            {
            }
        }

        public static void parse(string name, ref Color variable)
        {
            if (name != null)
            {
                string[] components = splitConfigValue(name);
                if (components.Length >= 3)
                {
                    float.TryParse(components[0], out variable.r);
                    float.TryParse(components[1], out variable.g);
                    float.TryParse(components[2], out variable.b);
                }
                if (components.Length >= 4)
                    float.TryParse(components[3], out variable.a);
            }
        }

        public static void addLists(string[] lists, ICollection<string> variable)
        {
            foreach (string list in lists)
            {
                foreach (string item in splitConfigValue(list))
                {
                    if (!variable.Contains(item))
                        variable.Add(item);
                }
            }
        }

        public static void addRELists(string[] lists, ICollection<Regex> variable)
        {
            foreach (string list in lists)
            {
                foreach (string item in splitConfigValue(list))
                    variable.Add(new Regex(item));
            }
        }

#if TR_LOG_HIERARCHY
    public static void logDownHierarchy(Transform tf, int indent = 0)
    {
      string sIndent = "";
      for (int i = 0; i < indent; ++i)
        sIndent += "  ";

      if (tf.gameObject != null)
        UnityEngine.Debug.Log(sIndent + "- " + tf.gameObject.name + ": " + tf.gameObject.GetType());

      foreach (Component c in tf.GetComponents<Component>())
      {
        UnityEngine.Debug.Log(sIndent + " * " + c);

        Renderer r = c as Renderer;
        if (r != null)
        {
          UnityEngine.Debug.Log(sIndent + "   shader:  " + r.material.shader);
          UnityEngine.Debug.Log(sIndent + "   texture: " + r.material.mainTexture);
        }
      }

      for (int i = 0; i < tf.childCount; ++i)
        logDownHierarchy(tf.GetChild(i), indent + 1);
    }

    /**
     * Print hierarchy up from a transform.
     */
    public static void logUpHierarchy(Transform tf)
    {
      for (; tf != null; tf = tf.parent)
      {
        if (tf.gameObject != null)
          UnityEngine.Debug.Log("+ " + tf.gameObject.name + ": " + tf.gameObject.GetType());

        foreach (Component c in tf.GetComponents<Component>())
        {
          UnityEngine.Debug.Log(" * " + c);

          Renderer r = c as Renderer;
          if (r != null)
          {
            UnityEngine.Debug.Log("   shader:  " + r.material.shader);
            UnityEngine.Debug.Log("   texture: " + r.material.mainTexture);
          }
        }
      }
    }
#endif
    }
}