/*
 * Copyright © 2013 Davorin Učakar
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

[KSPAddon(KSPAddon.Startup.EveryScene, true)]
public class VegaFPSCounter : MonoBehaviour
{
  private Rect windowPos;
  private long lastTicks = 0;
  private long refreshTicks = 0;
  private double fps = 0.0;
  private string fpsText = "";

  private void windowFunc(int id)
  {
    GUILayout.BeginHorizontal();
    GUI.Label(new Rect(0, 0, 44, 24), fpsText);
    GUILayout.EndHorizontal();
    GUI.DragWindow();
  }

  protected void Start()
  {
    DontDestroyOnLoad(this);

    lastTicks = DateTime.Now.Ticks;
  }

  protected void OnGUI()
  {
    if (HighLogic.LoadedScene == GameScenes.LOADING)
    {
      // The launch after the fullscreen setting is change switches fullscreen mode during loading,
      // which in turn changes screen size. That's why we continuously 'fix' position while loading.
      windowPos = new Rect(Screen.width - 42, -18, 44, 24);
    }
    else
    {
      GUI.skin = HighLogic.Skin;
      windowPos = GUILayout.Window(29457, windowPos, windowFunc, "FPS");
    }
  }

  protected void Update()
  {
    long deltaTicks = DateTime.Now.Ticks - lastTicks;

    fps = 0.66 * fps + 0.34 * 10000000.0 / (deltaTicks + 1);
    lastTicks += deltaTicks;
    refreshTicks += deltaTicks;

    if (refreshTicks > 3000000)
    {
      refreshTicks = 0;
      fpsText = String.Format("\n {0,5:##0.0}", fps);
    }
  }
}
