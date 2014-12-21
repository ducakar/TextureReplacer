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
using System.Collections.Generic;
using UnityEngine;

namespace TextureReplacer
{
  class UI
  {
    static readonly string APP_ICON_PATH = Util.DIR + "Plugins/appIcon";
    static readonly string[] SUIT_ASSIGNMENTS = { "Random", "Consecutive", "Experience" };
    static readonly Color SELECTED_COLOUR = new Color(0.7f, 0.9f, 1.0f);
    static readonly Color PERK_COLOUR = new Color(1.0f, 0.8f, 1.0f);
    const int WINDOW_ID = 107056;
    // Perks from config files.
    readonly List<string> perks = new List<string>();
    // UI state.
    Rect windowRect = new Rect(Screen.width - 600, 60, 580, 575);
    Vector2 rosterScroll = Vector2.zero;
    ProtoCrewMember selectedKerbal = null;
    string selectedPerk = null;
    bool isEnabled = false;
    // Application launcher icon.
    Texture2D appIcon = null;
    ApplicationLauncherButton appButton = null;
    bool isGuiEnabled = true;
    bool isInitialised = false;
    // Instance.
    public static UI instance = null;

    void windowHandler(int id)
    {
      Personaliser personaliser = Personaliser.instance;

      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal(GUILayout.Height(430));

      // Roster area.
      rosterScroll = GUILayout.BeginScrollView(rosterScroll, GUILayout.Width(200));
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
        {
          selectedKerbal = kerbal;
          selectedPerk = null;
        }
      }
      GUI.contentColor = Color.white;
      GUI.color = PERK_COLOUR;

      // Perk suits.
      foreach (string perk in perks)
      {
        if (GUILayout.Button(perk))
        {
          selectedKerbal = null;
          selectedPerk = perk;
        }
      }

      GUI.color = Color.white;

      GUILayout.EndVertical();
      GUILayout.EndScrollView();

      // Textures.
      Personaliser.Head defaultHead = personaliser.defaultHead;
      Personaliser.Suit defaultSuit = personaliser.defaultSuit;
      Personaliser.KerbalData kerbalData = null;
      Personaliser.Head head = null;
      Personaliser.Suit suit = null;
      int headIndex = -1;
      int suitIndex = -1;

      if (selectedKerbal != null)
      {
        kerbalData = personaliser.getKerbalData(selectedKerbal.name);

        if (kerbalData != null)
        {
          head = personaliser.getKerbalHead(kerbalData);
          suit = personaliser.getKerbalSuit(selectedKerbal, kerbalData);

          headIndex = personaliser.heads.IndexOf(head);
          suitIndex = personaliser.suits.IndexOf(suit);
        }
      }
      else if (selectedPerk != null)
      {
        personaliser.perkSuits.TryGetValue(selectedPerk, out suit);

        if (suit != null)
          suitIndex = personaliser.suits.IndexOf(suit);
      }

      GUILayout.Space(10);
      GUILayout.BeginVertical();

      if (head != null)
      {
        GUILayout.Box(head.head, GUILayout.Width(200), GUILayout.Height(200));
        GUILayout.Space(20);
      }

      if (suit != null)
      {
        Texture2D suitTex = suit == defaultSuit && kerbalData != null && kerbalData.isVeteran ?
                            defaultSuit.suitVeteran : (suit.suit ?? defaultSuit.suit);
        Texture2D helmetTex = suit.helmet ?? defaultSuit.helmet;
        Texture2D evaSuitTex = suit.evaSuit ?? defaultSuit.evaSuit;
        Texture2D evaHelmetTex = suit.evaHelmet ?? defaultSuit.evaHelmet;

        GUILayout.BeginHorizontal();
        GUILayout.Box(suitTex, GUILayout.Width(100), GUILayout.Height(100));
        GUILayout.Space(10);
        GUILayout.Box(helmetTex, GUILayout.Width(100), GUILayout.Height(100));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Box(evaSuitTex, GUILayout.Width(100), GUILayout.Height(100));
        GUILayout.Space(10);
        GUILayout.Box(evaHelmetTex, GUILayout.Width(100), GUILayout.Height(100));
        GUILayout.EndHorizontal();
      }

      GUILayout.EndVertical();
      GUILayout.BeginVertical(GUILayout.Width(120));

      if (kerbalData != null)
      {
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

        GUI.color = kerbalData.head == defaultHead ? SELECTED_COLOUR : Color.white;
        if (GUILayout.Button("Default"))
          kerbalData.head = defaultHead;

        GUI.color = kerbalData.head == null ? SELECTED_COLOUR : Color.white;
        if (GUILayout.Button("Unset/Generic"))
          kerbalData.head = null;

        GUI.color = Color.white;

        GUILayout.Space(120);
      }

      if (kerbalData != null || selectedPerk != null)
      {
        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.suits.Count != 0;

        if (GUILayout.Button("<"))
        {
          suitIndex = suitIndex == -1 ? 0 : suitIndex;
          suitIndex = (personaliser.suits.Count + suitIndex - 1) % personaliser.suits.Count;

          if (kerbalData != null)
          {
            kerbalData.suit = personaliser.suits[suitIndex];
            kerbalData.cabinSuit = null;
          }
          else
          {
            personaliser.perkSuits[selectedPerk] = personaliser.suits[suitIndex];
          }
        }
        if (GUILayout.Button(">"))
        {
          suitIndex = (suitIndex + 1) % personaliser.suits.Count;

          if (kerbalData != null)
          {
            kerbalData.suit = personaliser.suits[suitIndex];
            kerbalData.cabinSuit = null;
          }
          else
          {
            personaliser.perkSuits[selectedPerk] = personaliser.suits[suitIndex];
          }
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUI.color = suit == defaultSuit && (kerbalData == null || kerbalData.suit != null) ?
                    SELECTED_COLOUR : Color.white;
        if (GUILayout.Button("Default"))
        {
          if (kerbalData != null)
          {
            kerbalData.suit = defaultSuit;
            kerbalData.cabinSuit = null;
          }
          else
          {
            personaliser.perkSuits[selectedPerk] = defaultSuit;
          }
        }

        GUI.color = suit == null || (kerbalData != null && kerbalData.suit == null) ?
                    SELECTED_COLOUR : Color.white;
        if (GUILayout.Button("Unset/Generic"))
        {
          if (kerbalData != null)
          {
            kerbalData.suit = null;
            kerbalData.cabinSuit = null;
          }
          else
          {
            personaliser.perkSuits[selectedPerk] = null;
          }
        }

        GUI.color = Color.white;
      }

      GUILayout.EndVertical();
      GUILayout.EndHorizontal();
      GUILayout.Space(10);

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
      selectedPerk = null;
    }

    void disable()
    {
      isEnabled = false;
      selectedKerbal = null;
      selectedPerk = null;

      rosterScroll = Vector2.zero;
    }

    public void readConfig(ConfigNode rootNode)
    {
      Util.parse(rootNode.GetValue("isGUIEnabled"), ref isGuiEnabled);

      foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("EXPERIENCE_TRAIT"))
      {
        string name = node.GetValue("name");
        if (name != null)
          perks.AddUnique(name);
      }
    }

    public void initialise()
    {
      appIcon = GameDatabase.Instance.GetTexture(APP_ICON_PATH, false);
      if (appIcon == null)
        Util.log("Application icon missing: {0}", APP_ICON_PATH);

      isInitialised = true;
    }

    public void destroy()
    {
      disable();

      if (ApplicationLauncher.Ready && appButton != null)
        ApplicationLauncher.Instance.RemoveModApplication(appButton);
    }

    public void resetScene()
    {
      disable();

      if (HighLogic.LoadedScene == GameScenes.MAINMENU)
        appButton = null;
    }

    public void draw()
    {
      if (ApplicationLauncher.Ready && isGuiEnabled && isInitialised)
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
