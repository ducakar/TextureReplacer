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
using UnityEngine;

namespace TextureReplacer
{
  public class TRReflection : PartModule
  {
    // Configuration file parameters.
    [KSPField(isPersistant = false)]
    public string colour = "0.5 0.5 0.5";
    [KSPField(isPersistant = false)]
    public string meshes = "";

    public override void OnStart(StartState state)
    {
      if (Reflections.instance.envMap == null)
        return;

      Color reflectColour = new Color(0.5f, 0.5f, 0.5f);

      string[] components = Util.splitConfigValue(colour);
      if (components.Length < 3 || 4 < components.Length)
      {
        Util.log("reflectionColour must have exactly 3 or 4 components");
      }
      else
      {
        float.TryParse(components[0], out reflectColour.r);
        float.TryParse(components[1], out reflectColour.g);
        float.TryParse(components[2], out reflectColour.b);

        if (components.Length == 4)
          float.TryParse(components[3], out reflectColour.a);
      }

      string[] meshNames = Util.splitConfigValue(meshes);

      if (Reflections.instance.logReflectiveMeshes)
        Util.log("Part \"{0}\"", part.name);

      foreach (MeshFilter meshFilter in part.FindModelComponents<MeshFilter>())
      {
        if (meshFilter.renderer == null)
          continue;

        if (Reflections.instance.logReflectiveMeshes)
          Util.log("+ {0} [{1}]", meshFilter.name, meshFilter.renderer.material.shader.name);

        if (meshNames.Length != 0 && !meshNames.Contains(meshFilter.name))
          continue;

        Material material = meshFilter.renderer.material;
        Shader newShader = Reflections.instance.toReflective(material.shader);

        if (newShader != null)
        {
          material.shader = newShader;
          material.SetTexture(Util.CUBE_PROPERTY, Reflections.instance.envMap);
          material.SetColor(Util.REFLECT_COLOR_PROPERTY, reflectColour);
        }
      }
    }
  }
}
