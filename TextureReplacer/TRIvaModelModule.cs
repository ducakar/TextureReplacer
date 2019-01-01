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

using UnityEngine;

namespace TextureReplacer
{
  /// <summary>
  /// Component bound to internal models that triggers adds another component to IVA Kerbals that actually personalises
  /// them.
  ///
  /// Directly personalising Kerbals doesn't work, they are reset to default suits after that in some situation
  /// (e.g. crew transfer). So we have to personalise Kerbals by using another component (TRIvaModule) bound directly
  /// to IVA Kerbals. So why don't we add this component to proto-models in first place? Once we did, but the vintage
  /// Kerbals don't have preexisting proto-models. Instead, they are created anew each time, so this is the only nice
  /// way to get TRIvaModule on all IVA Kerbals, both standard and vintage ones.
  /// </summary>
  class TRIvaModelModule : MonoBehaviour
  {
    public void Start()
    {
      foreach (Kerbal kerbal in GetComponentsInChildren<Kerbal>()) {
        if (kerbal.GetComponent<TRIvaModule>() == null) {
          kerbal.gameObject.AddComponent<TRIvaModule>();
        }
      }
      Destroy(this);
    }
  }
}
