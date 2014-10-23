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

#if false

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TextureReplacer
{
  public class TRReflection : PartModule
  {
    // Shader.
    Shader shader = null;
    // Reflection colour.
    Color colour = new Color(0.5f, 0.5f, 0.5f);
    // Configuration file parameters.
    [KSPField(isPersistant = false)]
    public string reflectionShader = "Reflective/VertexLit";
    [KSPField(isPersistant = false)]
    public string reflectionColour = "0.5 0.5 0.5";
    [KSPField(isPersistant = false)]
    public string meshList = "";

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    static void log(string s, params object[] args)
    {
      Debug.Log("[TR.TRReflection] " + String.Format(s, args));
    }

    public override void OnStart(StartState state)
    {
      shader = Shader.Find(reflectionShader);
      if (shader == null)
        return;

      string[] components = reflectionColour.Split(new char[] { ' ', ',' },
                                                   StringSplitOptions.RemoveEmptyEntries);
      if (components.Length < 3 || 4 < components.Length)
      {
        log("reflectionColour must have exactly 3 or 4 components");
      }
      else
      {
        float.TryParse(components[0], out colour.r);
        float.TryParse(components[1], out colour.g);
        float.TryParse(components[2], out colour.b);

        if (components.Length == 4)
          float.TryParse(components[3], out colour.a);
      }

      string[] meshNames = meshList.Split(new char[] { ' ', ',' },
                                          StringSplitOptions.RemoveEmptyEntries);

      foreach (MeshFilter meshFilter in part.FindModelComponents<MeshFilter>())
      {
        if (meshNames.Length != 0 && meshNames.Contains(meshFilter.name))
          continue;

        meshFilter.renderer.material.shader = shader;
        meshFilter.renderer.material.SetColor("_ReflectColor", colour);

        if (Reflections.instance.envMap != null)
          meshFilter.renderer.material.SetTexture("_Cube", Reflections.instance.envMap);
      }
    }
  }
}

#endif
