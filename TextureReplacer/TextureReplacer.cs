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

using System.Reflection;
using UnityEngine;

namespace TextureReplacer
{
  [KSPAddon(KSPAddon.Startup.Instantly, true)]
  public class TextureReplacer : MonoBehaviour
  {
    public static bool IsLoaded { get; private set; }

    static readonly Log log = new Log(nameof(TextureReplacer));

    public void Start()
    {
      log.Print("Started, Version {0}", Assembly.GetExecutingAssembly().GetName().Version);

      DontDestroyOnLoad(this);

      IsLoaded = false;

      if (Reflections.Instance != null) {
        Reflections.Instance.Destroy();
      }

      Replacer.Recreate();
      Reflections.Recreate();
      Personaliser.Recreate();

      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer")) {
        Replacer.Instance.ReadConfig(file.config);
        Reflections.Instance.ReadConfig(file.config);
        Personaliser.Instance.ReadConfig(file.config);
      }
    }

    public void LateUpdate()
    {
      if (PartLoader.Instance.IsReady()) {
        Replacer.Instance.Load();
        Reflections.Instance.Load();
        Personaliser.Instance.Load();

        IsLoaded = true;
        Destroy(this);
      }
    }
  }
}
