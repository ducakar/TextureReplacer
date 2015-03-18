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

using System;
using System.Reflection;
using UnityEngine;

namespace TextureReplacer
{
  [KSPAddon(KSPAddon.Startup.Instantly, true)]
  public class TextureReplacer : MonoBehaviour
  {
    GameScenes lastScene = GameScenes.LOADING;
    bool isInitialised = false;
    // Instance.
    public static TextureReplacer instance = null;

    public void Start()
    {
      Util.log("Started {0}", Assembly.GetExecutingAssembly().GetName().Version);

      if (instance != null)
        DestroyImmediate(instance);

      DontDestroyOnLoad(this);
      instance = this;

      UI.instance = new UI();
      Loader.instance = new Loader();
      Replacer.instance = new Replacer();
      Reflections.instance = new Reflections();
      Personaliser.instance = new Personaliser();

      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer"))
      {
        UI.instance.readConfig(file.config);
        Loader.instance.readConfig(file.config);
        Replacer.instance.readConfig(file.config);
        Reflections.instance.readConfig(file.config);
        Personaliser.instance.readConfig(file.config);
      }

      Loader.instance.configure();
    }

    public void LateUpdate()
    {
      if (!isInitialised)
      {
        // Compress textures, generate mipmaps, convert DXT5 -> DXT1 if necessary etc.
        Loader.instance.processTextures();

        if (GameDatabase.Instance.IsReady())
        {
          UI.instance.initialise();
          Loader.instance.initialise();
          Replacer.instance.initialise();
          Reflections.instance.initialise();
          Personaliser.instance.initialise();

          isInitialised = true;
        }
      }
      else
      {
        // Schedule general texture replacement pass at the beginning of each scene. Textures are
        // still loaded several frames after scene switch so this pass must be repeated multiple
        // times. Especially problematic is the main menu that resets skybox texture twice, second
        // time being several tens of frames after the load (depending on frame rate).
        if (HighLogic.LoadedScene != lastScene)
        {
          lastScene = HighLogic.LoadedScene;

          UI.instance.resetScene();
          Replacer.instance.resetScene();
          Personaliser.instance.resetScene();
        }

        Replacer.instance.updateScene();
        Personaliser.instance.updateScene();

        if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
          Reflections.Script.updateScripts();
      }
    }

    public void OnGUI()
    {
      if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
        UI.instance.draw();
    }

    public void OnDestroy()
    {
      if (Reflections.instance != null)
        Reflections.instance.destroy();
      if (UI.instance != null)
        UI.instance.destroy();
    }
  }
}
