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
using System.Linq;
using UnityEngine;

namespace TextureReplacer
{
  public class TRReflection : PartModule
  {
    [KSPField(isPersistant = false)]
    public string shader = "";
    [KSPField(isPersistant = false)]
    public string colour = "";
    [KSPField(isPersistant = false)]
    public string interval = "";
    [KSPField(isPersistant = false)]
    public string meshes = "";

    // ReflectionPlugin parameters.
    [KSPField(isPersistant = false)]
    public string ReflectionColor = "";
    [KSPField(isPersistant = false)]
    public string MeshesToChange = "all";

    static readonly Log log = new Log(nameof(TRReflection));

    Reflections.Script script;

    public override void OnStart(StartState state)
    {
      Reflections reflections = Reflections.Instance;

      Shader reflectiveShader = shader.Length == 0 ? null : Shader.Find(shader);
      Color reflectionColour = new Color(0.7f, 0.7f, 0.7f);
      int updateInterval = 1;

      Util.Parse(ReflectionColor, ref reflectionColour);
      Util.Parse(colour, ref reflectionColour);
      Util.Parse(interval, ref updateInterval);

      updateInterval = Math.Max(updateInterval, 1);

      List<string> meshNames = Util.SplitConfigValue(meshes).ToList();
      if (MeshesToChange != "all") {
        meshNames.AddUniqueRange(Util.SplitConfigValue(MeshesToChange));
      }
      if (reflections.ReflectionType == Reflections.Type.None) {
        return;
      }
      if (reflections.LogReflectiveMeshes) {
        log.Print("Part \"{0}\"", part.name);
      }

      script = new Reflections.Script(part, updateInterval);

      bool success = false;

      foreach (MeshFilter meshFilter in part.FindModelComponents<MeshFilter>()) {
        Renderer renderer = meshFilter.GetComponent<Renderer>();
        if (renderer == null) {
          continue;
        }

        Material material = renderer.material;
        if (reflections.LogReflectiveMeshes) {
          log.Print("+ {0} [{1}]", meshFilter.name, material.shader.name);
        }
        if (meshNames.Count == 0 || meshNames.Contains(meshFilter.name)) {
          success |= script.Apply(material, reflectiveShader, reflectionColour);
        }
      }

      if (!success) {
        if (script != null) {
          script.Destroy();
          script = null;
        }
        log.Print("Failed to replace any shader on \"{0}\" with its reflective counterpart", part.name);
      }
    }

    public void OnDestroy()
    {
      if (script != null) {
        script.Destroy();
      }
    }
  }
}
