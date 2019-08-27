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
  [KSPAddon(KSPAddon.Startup.EveryScene, false)]
  public class TRActivator : MonoBehaviour
  {
    /// <summary>
    /// Reflection updater. We don't want this to run every frame unless real reflections are enabled so it's wrapped
    /// inside another component and enabled only when needed.
    /// </summary>
    public class TRReflectionUpdater : MonoBehaviour
    {
      public void Update()
      {
        Reflections.Script.UpdateScripts();
      }
    }

    private static readonly Log log = new Log(nameof(TRActivator));

    private static bool isLoaded;

    private bool isFlightScene;
    private TRReflectionUpdater reflectionUpdater;

    private static void Load()
    {
      log.Print("Started, Version {0}", Assembly.GetExecutingAssembly().GetName().Version);

      Reflections.Instance?.Destroy();

      Replacer.Recreate();
      Reflections.Recreate();
      Personaliser.Recreate();

      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer")) {
        Replacer.Instance.ReadConfig(file.config);
        Reflections.Instance.ReadConfig(file.config);
        Personaliser.Instance.ReadConfig(file.config);
      }

      Replacer.Instance.Load();
      Reflections.Instance.Load();
      Personaliser.Instance.Load();

      isLoaded = true;
    }

    public void Start()
    {
      if (!isLoaded) {
        if (PartLoader.Instance.IsReady()) {
          Load();
        }
      } else {
        Replacer.Instance.OnBeginScene();

        if (HighLogic.LoadedSceneIsFlight) {
          Replacer.Instance.OnBeginFlight();
          Personaliser.Instance.OnBeginFlight();
          isFlightScene = true;
        }

        if ((HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) &&
            Reflections.Instance.ReflectionType == Reflections.Type.Real) {
          reflectionUpdater = gameObject.AddComponent<TRReflectionUpdater>();
        }
      }
    }

    public void OnDestroy()
    {
      if (reflectionUpdater != null) {
        Destroy(reflectionUpdater);
      }

      if (isFlightScene) {
        Personaliser.Instance.OnEndFlight();
      }
    }
  }
}
