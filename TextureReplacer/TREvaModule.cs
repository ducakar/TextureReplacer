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

namespace TextureReplacer
{
  /// <summary>
  /// This class takes cares of EVA Kerbal to assign appropriate suit on creation and manage reflection script.
  /// </summary>
  class TREvaModule : PartModule
  {
    Reflections.Script reflectionScript;

    public override void OnStart(StartState state)
    {
      if (Reflections.Instance.IsVisorReflectionEnabled &&
          Reflections.Instance.ReflectionType == Reflections.Type.Real) {
        reflectionScript = new Reflections.Script(part, 1);
        reflectionScript.SetActive(false);
      }

      Personaliser.Instance.PersonaliseEva(part);
    }

    public void OnHelmetChanged(bool enabled)
    {
      if (reflectionScript != null) {
        reflectionScript.SetActive(enabled);
      }
    }

    public void OnDestroy()
    {
      if (reflectionScript != null) {
        reflectionScript.Destroy();
      }
    }
  }
}
