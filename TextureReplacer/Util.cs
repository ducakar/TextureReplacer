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

using System;
using UnityEngine;
using File = KSP.IO.File;
using FileStream = KSP.IO.FileStream;

namespace TextureReplacer
{
    internal static class Util
    {
        public const string Directory = "TextureReplacer/";

        public static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        public static readonly int MainTextureProperty = Shader.PropertyToID("_MainTexture");
        public static readonly int BumpMapProperty = Shader.PropertyToID("_BumpMap");
        public static readonly int EmissiveProperty = Shader.PropertyToID("_Emissive");
        public static readonly int CubeProperty = Shader.PropertyToID("_Cube");
        public static readonly int ReflectColorProperty = Shader.PropertyToID("_ReflectColor");
        public static readonly System.Random Random = new System.Random();

        private static readonly char[] ConfigDelimiters = { ' ', '\t', ',' };

        /// <summary>
        /// Split a space- and/or comma-separated configuration file value into its tokens.
        /// </summary>
        public static string[] SplitConfigValue(string value)
        {
            return value.Split(ConfigDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void Parse(string name, ref bool variable)
        {
            if (bool.TryParse(name, out bool value))
            {
                variable = value;
            }
        }

        public static void Parse(string name, ref int variable)
        {
            if (int.TryParse(name, out int value))
            {
                variable = value;
            }
        }

        public static void Parse<TEnum>(string name, ref TEnum variable)
            where TEnum : struct
        {
            if (Enum.TryParse(name, out TEnum value))
            {
                variable = value;
            }
        }

        public static void Parse(string name, ref Color variable)
        {
            if (name == null)
            {
                return;
            }

            string[] components = SplitConfigValue(name);
            if (components.Length >= 3)
            {
                float.TryParse(components[0], out variable.r);
                float.TryParse(components[1], out variable.g);
                float.TryParse(components[2], out variable.b);
            }

            if (components.Length >= 4)
            {
                float.TryParse(components[3], out variable.a);
            }
        }

        /// <summary>
        /// True, iff a given char is encountered after the last dot (the the extension). False if there is no extension.
        /// </summary>
        public static bool HasSuffix(string s, char c)
        {
            bool flag = false;
            for (int i = s.Length - 1; i >= 0; --i)
            {
                if (s[i] == c)
                {
                    flag = true;
                }
                else if (s[i] == '.')
                {
                    return flag;
                }
            }
            return false;
        }

        /// <summary>
        /// Print hierarchy under a transform.
        /// </summary>
        public static void LogDownHierarchy(Transform tf, string indent = "")
        {
            LogTransform(tf, indent);

            for (int i = 0; i < tf.childCount; ++i)
            {
                LogDownHierarchy(tf.GetChild(i), indent + "  ");
            }
        }

        /// <summary>
        /// Print transform node with its attached objects.
        /// </summary>
        private static void LogTransform(Component tf, string indent = "")
        {
            if (tf.gameObject != null)
            {
                Debug.Log($"{indent}* {tf.gameObject}");
            }

            foreach (Component c in tf.GetComponents<Component>())
            {
                Debug.Log($"{indent} - {c}");

                if (c is Renderer r)
                {
                    Debug.Log($"{indent}   material: {r.material.name}");
                    Debug.Log($"{indent}   shader:   {r.material.shader}");

                    foreach (string name in r.material.GetTexturePropertyNames())
                    {
                        Debug.Log($"{indent}   {name}: {r.material.GetTexture(name)}");
                    }
                }
            }
        }

        /// <summary>
        /// Export any texture (even if not loaded in RAM) as a PNG.
        /// </summary>
        public static void DumpToPng(Texture texture)
        {
            var targetTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            var renderTex = new RenderTexture(texture.width, texture.height, 32);

            RenderTexture originalRenderTex = RenderTexture.active;
            Graphics.Blit(texture, renderTex);
            RenderTexture.active = renderTex;
            targetTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = originalRenderTex;

            byte[] data = targetTex.EncodeToPNG();
            using FileStream fs = File.Create<TRActivator>(texture.name + ".png");
            fs.Write(data, 0, data.Length);
        }
    }
}
