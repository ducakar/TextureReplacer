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

using System;
using UnityEngine;

namespace TextureReplacer
{
  class UI
  {
    const int WINDOW_ID = 107056;
    // Empty texture, shown when unset or a stock texture is selected.
    readonly Texture2D EMPTY_TEX = new Texture2D(1, 1);
    // UI properties.
    bool isEnabled = false;
    Rect windowRect = new Rect(Screen.width - 640, 80, 600, 560);
    Vector2 rosterScroll = Vector2.zero;
    ApplicationLauncherButton appButton = null;
    // Current selections in UI.
    ProtoCrewMember selectedKerbal = null;
    // Instance.
    public static UI instance = null;

    void windowHandler(int id)
    {
      GUILayout.BeginHorizontal();

      // Roster area.
      rosterScroll = GUILayout.BeginScrollView(rosterScroll, GUILayout.Width(200));
      GUILayout.BeginVertical();

      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
      {
        if (GUILayout.Button(kerbal.name))
          selectedKerbal = kerbal;
      }

      GUILayout.EndVertical();
      GUILayout.EndScrollView();

      // Textures.
      if (selectedKerbal != null)
      {
        Personaliser personaliser = Personaliser.instance;

        GUILayout.BeginVertical();

        Personaliser.Head defaultHead = personaliser.defaultHead;
        Personaliser.Suit defaultSuit = personaliser.defaultSuit;
        Personaliser.Head head = null;
        Personaliser.Suit suit = null;

        bool hasCustomHead = false;
        bool hasCustomSuit = false;
        int headIndex = -1;
        int suitIndex = -1;

        if (personaliser.customHeads.ContainsKey(selectedKerbal.name))
        {
          head = personaliser.customHeads[selectedKerbal.name];
          hasCustomHead = true;
          headIndex = personaliser.heads.IndexOf(head);
        }

        if (personaliser.customSuits.ContainsKey(selectedKerbal.name))
        {
          suit = personaliser.customSuits[selectedKerbal.name];
          hasCustomSuit = true;
          suitIndex = personaliser.suits.IndexOf(suit);
        }

        head = head ?? defaultHead;
        suit = suit ?? defaultSuit;

        Texture2D headTex = head.head ?? defaultHead.head ?? EMPTY_TEX;
        Texture2D suitTex = suit.suit ?? defaultSuit.suit ?? EMPTY_TEX;
        Texture2D helmetTex = suit.helmet ?? defaultSuit.helmet ?? EMPTY_TEX;
        Texture2D evaSuitTex = suit.evaSuit ?? defaultSuit.evaSuit ?? EMPTY_TEX;
        Texture2D evaHelmetTex = suit.evaHelmet ?? defaultSuit.evaHelmet ?? EMPTY_TEX;

        if (hasCustomHead)
          GUILayout.Box(headTex, GUILayout.Width(250), GUILayout.Height(250));
        else
          GUILayout.Box("Generic", GUILayout.Width(250), GUILayout.Height(250));

        GUILayout.Space(20);

        if (hasCustomSuit)
        {
          GUILayout.BeginHorizontal();
          GUILayout.Box(suitTex, GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.Space(10);
          GUILayout.Box(helmetTex, GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.EndHorizontal();

          GUILayout.Space(10);

          GUILayout.BeginHorizontal();
          GUILayout.Box(evaSuitTex, GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.Space(10);
          GUILayout.Box(evaHelmetTex, GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.EndHorizontal();
        }
        else
        {
          GUILayout.BeginHorizontal();
          GUILayout.Box("Generic", GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.Space(10);
          GUILayout.Box("Generic", GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.EndHorizontal();

          GUILayout.Space(10);

          GUILayout.BeginHorizontal();
          GUILayout.Box("Generic", GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.Space(10);
          GUILayout.Box("Generic", GUILayout.Width(120), GUILayout.Height(120));
          GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.heads.Count != 0;

        if (GUILayout.Button("<"))
        {
          headIndex = (personaliser.heads.Count + headIndex - 1) % personaliser.heads.Count;
          personaliser.customHeads[selectedKerbal.name] = personaliser.heads[headIndex];
        }
        if (GUILayout.Button(">"))
        {
          headIndex = (headIndex + 1) % personaliser.heads.Count;
          personaliser.customHeads[selectedKerbal.name] = personaliser.heads[headIndex];
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Default"))
          Personaliser.instance.customHeads[selectedKerbal.name] = null;

        if (GUILayout.Button("Unset"))
          Personaliser.instance.customHeads.Remove(selectedKerbal.name);

        GUILayout.Space(170);

        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.suits.Count != 0;

        if (GUILayout.Button("<"))
        {
          suitIndex = (personaliser.suits.Count + suitIndex - 1) % personaliser.suits.Count;
          personaliser.customSuits[selectedKerbal.name] = personaliser.suits[suitIndex];
        }
        if (GUILayout.Button(">"))
        {
          suitIndex = (personaliser.suits.Count + suitIndex - 1) % personaliser.suits.Count;
          personaliser.customSuits[selectedKerbal.name] = personaliser.suits[suitIndex];
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Default"))
          Personaliser.instance.customSuits[selectedKerbal.name] = null;

        if (GUILayout.Button("Unset"))
          Personaliser.instance.customSuits.Remove(selectedKerbal.name);

        GUILayout.EndVertical();
      }

      GUILayout.EndHorizontal();
      GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
    }

    void enable()
    {
      isEnabled = true;
      selectedKerbal = null;
    }

    void disable()
    {
      isEnabled = false;
      selectedKerbal = null;
    }

    public void initialise()
    {
    }

    public void resetScene()
    {
      selectedKerbal = null;

      if (HighLogic.LoadedScene == GameScenes.MAINMENU)
        appButton = null;
    }

    public void draw()
    {
      if (ApplicationLauncher.Ready)
      {
        bool hidden;

        if (!ApplicationLauncher.Instance.Contains(appButton, out hidden))
        {
          appButton = ApplicationLauncher.Instance
            .AddModApplication(enable, disable, null, null, null, null,
                               ApplicationLauncher.AppScenes.SPACECENTER, EMPTY_TEX);
        }
        else if (isEnabled)
        {
          GUI.skin = HighLogic.Skin;
          windowRect = GUILayout.Window(WINDOW_ID, windowRect, windowHandler, "TextureReplacer");
          windowRect.x = Math.Max(0, Math.Min(Screen.width - 30, windowRect.x));
          windowRect.y = Math.Max(0, Math.Min(Screen.height - 30, windowRect.y));
        }
      }
    }
  }
}
