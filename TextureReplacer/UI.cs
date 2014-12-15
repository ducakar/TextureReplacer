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
    static readonly string APP_ICON_PATH = Util.DIR + "Plugins/appIcon";
    static readonly string[] SUIT_ASSIGNMENTS = { "Random", "Consecutive", "Experience" };
    const int WINDOW_ID = 107056;
    // UI state.
    Rect windowRect = new Rect(Screen.width - 620, 60, 600, 580);
    Vector2 rosterScroll = Vector2.zero;
    ProtoCrewMember selectedKerbal = null;
    bool isEnabled = false;
    // Application launcher icon.
    Texture2D appIcon = null;
    ApplicationLauncherButton appButton = null;
    bool isGuiEnabled = true;
    // Instance.
    public static UI instance = null;

    void windowHandler(int id)
    {
      Personaliser personaliser = Personaliser.instance;

      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal(GUILayout.Height(430));
      GUILayout.BeginVertical(GUILayout.Width(200));

      // Roster area.
      rosterScroll = GUILayout.BeginScrollView(rosterScroll);
      GUILayout.BeginVertical();

      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
      {
        switch (kerbal.rosterStatus)
        {
          case ProtoCrewMember.RosterStatus.Assigned:
            GUI.contentColor = Color.cyan;
            break;
          case ProtoCrewMember.RosterStatus.Dead:
            continue;
          case ProtoCrewMember.RosterStatus.Missing:
            GUI.contentColor = Color.yellow;
            break;
          default:
            GUI.contentColor = Color.white;
            break;
        }

        if (GUILayout.Button(kerbal.name))
          selectedKerbal = kerbal;
      }
      GUI.contentColor = Color.white;

      GUILayout.EndVertical();
      GUILayout.EndScrollView();

      // Task suits.
      if (GUILayout.Button("Pilot"))
      {
        selectedKerbal = null;
      }
      if (GUILayout.Button("Engineer"))
      {
        selectedKerbal = null;
      }
      if (GUILayout.Button("Scientist"))
      {
        selectedKerbal = null;
      }
      if (GUILayout.Button("Passenger"))
      {
        selectedKerbal = null;
      }

      GUILayout.EndVertical();

      // Textures.
      if (selectedKerbal != null)
      {
        Personaliser.KerbalData kerbalData = personaliser.getKerbalData(selectedKerbal.name);
        int headIndex = -1;
        int suitIndex = -1;

        if (kerbalData.head != null)
          headIndex = personaliser.heads.IndexOf(kerbalData.head);

        if (kerbalData.suit != null)
          suitIndex = personaliser.suits.IndexOf(kerbalData.suit);

        GUILayout.Space(20);
        GUILayout.BeginVertical();

        if (kerbalData.head != null)
          GUILayout.Box(kerbalData.head.head, GUILayout.Width(200), GUILayout.Height(200));
        else
          GUILayout.Box("Generic", GUILayout.Width(200), GUILayout.Height(200));

        GUILayout.Space(20);

        if (kerbalData.suit != null)
        {
          GUILayout.BeginHorizontal();
          GUILayout.Box(kerbalData.isVeteran ? kerbalData.suit.suitVeteran : kerbalData.suit.suit,
                        GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.Space(10);
          GUILayout.Box(kerbalData.suit.helmet, GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.EndHorizontal();

          GUILayout.Space(10);

          GUILayout.BeginHorizontal();
          GUILayout.Box(kerbalData.suit.evaSuit, GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.Space(10);
          GUILayout.Box(kerbalData.suit.evaHelmet, GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.EndHorizontal();
        }
        else
        {
          GUILayout.BeginHorizontal();
          GUILayout.Box("Generic", GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.Space(10);
          GUILayout.Box("Generic", GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.EndHorizontal();

          GUILayout.Space(10);

          GUILayout.BeginHorizontal();
          GUILayout.Box("Generic", GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.Space(10);
          GUILayout.Box("Generic", GUILayout.Width(100), GUILayout.Height(100));
          GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.heads.Count != 0;

        if (GUILayout.Button("<"))
        {
          headIndex = headIndex == -1 ? 0 : headIndex;
          headIndex = (personaliser.heads.Count + headIndex - 1) % personaliser.heads.Count;

          kerbalData.head = personaliser.heads[headIndex];
        }
        if (GUILayout.Button(">"))
        {
          headIndex = (headIndex + 1) % personaliser.heads.Count;

          kerbalData.head = personaliser.heads[headIndex];
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Default"))
          kerbalData.head = personaliser.defaultHead;

        if (GUILayout.Button("Unset"))
          kerbalData.head = null;

        GUILayout.Space(120);

        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.suits.Count != 0;

        if (GUILayout.Button("<"))
        {
          suitIndex = suitIndex == -1 ? 0 : suitIndex;
          suitIndex = (personaliser.suits.Count + suitIndex - 1) % personaliser.suits.Count;

          kerbalData.suit = personaliser.suits[suitIndex];
          kerbalData.cabinSuit = null;
        }
        if (GUILayout.Button(">"))
        {
          suitIndex = (suitIndex + 1) % personaliser.suits.Count;

          kerbalData.suit = personaliser.suits[suitIndex];
          kerbalData.cabinSuit = null;
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Default"))
        {
          kerbalData.suit = personaliser.defaultSuit;
          kerbalData.cabinSuit = null;
        }
        if (GUILayout.Button("Unset"))
        {
          kerbalData.suit = null;
          kerbalData.cabinSuit = null;
        }

        GUILayout.EndVertical();
      }

      GUILayout.EndHorizontal();

      personaliser.isHelmetRemovalEnabled = GUILayout.Toggle(
        personaliser.isHelmetRemovalEnabled, "Remove IVA helmets in safe situations");

      personaliser.isAtmSuitEnabled = GUILayout.Toggle(
        personaliser.isAtmSuitEnabled, "Spawn Kerbals in IVA suits when in breathable atmosphere");

      GUILayout.BeginHorizontal();
      GUILayout.Label("Generic suits:");
      personaliser.suitAssignment = (Personaliser.SuitAssignment) GUILayout.SelectionGrid(
        (int) personaliser.suitAssignment, SUIT_ASSIGNMENTS, 3);
      GUILayout.EndHorizontal();

      GUILayout.EndVertical();
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

    public void readConfig(ConfigNode rootNode)
    {
      string sIsGuiEnabled = rootNode.GetValue("isGUIEnabled");
      if (sIsGuiEnabled != null)
        bool.TryParse(sIsGuiEnabled, out isGuiEnabled);
    }

    public void initialise()
    {
      appIcon = GameDatabase.Instance.GetTexture(APP_ICON_PATH, false);

      Util.log(APP_ICON_PATH);
      if (appIcon == null)
        Util.log("Application icon missing: {0}", APP_ICON_PATH);
    }

    public void resetScene()
    {
      disable();

      if (HighLogic.LoadedScene == GameScenes.MAINMENU)
        appButton = null;
    }

    public void draw()
    {
      if (isGuiEnabled && ApplicationLauncher.Ready)
      {
        bool hidden;

        if (!ApplicationLauncher.Instance.Contains(appButton, out hidden))
        {
          appButton = ApplicationLauncher.Instance
            .AddModApplication(enable, disable, null, null, null, null,
                               ApplicationLauncher.AppScenes.SPACECENTER, appIcon);
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
