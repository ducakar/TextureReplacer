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

using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TextureReplacer
{
  [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
  public class TRGui : MonoBehaviour
  {
    const int windowId = 107056;
    static readonly string appIconPath = Util.Directory + "Plugins/AppIcon";
    static readonly string[] reflectionTypes = { "None", "Static", "Real" };
    static readonly Color selectedColour = new Color(0.7f, 0.9f, 1.0f);
    static readonly Color classColour = new Color(1.0f, 0.8f, 1.0f);

    // Application launcher icon.
    Texture2D appIcon;
    ApplicationLauncherButton appButton;
    bool isGuiEnabled = true;
    // UI state.
    Rect windowRect = new Rect(Screen.width - 600, 60, 580, 610);
    Vector2 rosterScroll = Vector2.zero;
    bool isEnabled;
    // Classes from config files.
    readonly List<string> classes = new List<string>();
    // Selection.
    ProtoCrewMember selectedKerbal;
    string selectedClass;

    void WindowHandler(int id)
    {
      Reflections reflections = Reflections.Instance;
      Personaliser personaliser = Personaliser.Instance;

      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();

      GUILayout.BeginVertical(GUILayout.Width(200));

      // Roster area.
      rosterScroll = GUILayout.BeginScrollView(rosterScroll);
      GUILayout.BeginVertical();

      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew) {
        switch (kerbal.rosterStatus) {
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

        if (GUILayout.Button(kerbal.name)) {
          selectedKerbal = kerbal;
          selectedClass = null;
        }
      }

      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned) {
        switch (kerbal.rosterStatus) {
          case ProtoCrewMember.RosterStatus.Dead:
            GUI.contentColor = Color.cyan;
            break;
          default:
            continue;
        }

        if (GUILayout.Button(kerbal.name)) {
          selectedKerbal = kerbal;
          selectedClass = null;
        }
      }

      GUI.contentColor = Color.white;
      GUI.color = classColour;

      // Class suits.
      foreach (string clazz in classes) {
        if (GUILayout.Button(clazz)) {
          selectedKerbal = null;
          selectedClass = clazz;
        }
      }

      GUI.color = Color.white;

      GUILayout.EndVertical();
      GUILayout.EndScrollView();

      if (GUILayout.Button("Reset to Defaults")) {
        personaliser.ResetKerbals();
      }

      GUILayout.EndVertical();

      // Textures.
      Personaliser.Skin defaultSkin = personaliser.DefaultSkin[0];
      Personaliser.Suit defaultSuit = personaliser.DefaultSuit;
      Personaliser.KerbalData kerbalData = null;
      Personaliser.Skin skin = null;
      Personaliser.Suit suit = null;
      int skinIndex = -1;
      int suitIndex = -1;

      if (selectedKerbal != null) {
        kerbalData = personaliser.GetKerbalData(selectedKerbal);
        defaultSkin = personaliser.DefaultSkin[(int) selectedKerbal.gender];

        skin = personaliser.GetKerbalSkin(selectedKerbal, kerbalData);
        suit = personaliser.GetKerbalSuit(selectedKerbal, kerbalData);

        skinIndex = personaliser.Skins.IndexOf(skin);
        suitIndex = personaliser.Suits.IndexOf(suit);
      }
      else if (selectedClass != null) {
        personaliser.ClassSuits.TryGetValue(selectedClass, out suit);

        if (suit != null) {
          suitIndex = personaliser.Suits.IndexOf(suit);
        }
      }

      GUILayout.Space(10);
      GUILayout.BeginVertical();

      if (skin != null) {
        GUILayout.Box(skin.Head, GUILayout.Width(200), GUILayout.Height(200));

        GUILayout.Label(skin.Name);
      }

      if (suit != null) {
        Texture2D suitTex = suit == defaultSuit && kerbalData != null && kerbalData.IsVeteran
          ? defaultSuit.BodyVeteran
          : (suit.Body ?? defaultSuit.Body);
        Texture2D helmetTex = suit.Helmet ?? defaultSuit.Helmet;
        Texture2D evaSuitTex = suit.EvaBody ?? defaultSuit.EvaBody;
        Texture2D evaHelmetTex = suit.EvaHelmet ?? defaultSuit.EvaHelmet;

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

        GUILayout.Label(suit.Name);
      }

      GUILayout.EndVertical();
      GUILayout.BeginVertical(GUILayout.Width(120));

      bool isKerbalSelected = kerbalData != null;
      bool isClassSelected = selectedClass != null;

      if (isKerbalSelected) {
        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.Skins.Count != 0;

        if (GUILayout.Button("<")) {
          skinIndex = skinIndex == -1 ? 0 : skinIndex;
          skinIndex = (personaliser.Skins.Count + skinIndex - 1) % personaliser.Skins.Count;

          kerbalData.Skin = personaliser.Skins[skinIndex];
        }
        if (GUILayout.Button(">")) {
          skinIndex = (skinIndex + 1) % personaliser.Skins.Count;

          kerbalData.Skin = personaliser.Skins[skinIndex];
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUI.color = kerbalData.Skin == defaultSkin ? selectedColour : Color.white;
        if (GUILayout.Button("Default")) {
          kerbalData.Skin = defaultSkin;
        }

        GUI.color = kerbalData.Skin == null ? selectedColour : Color.white;
        if (GUILayout.Button("Unset/Generic")) {
          kerbalData.Skin = null;
        }

        GUI.color = Color.white;
      }

      if (isKerbalSelected || isClassSelected) {
        GUILayout.Space(130);

        GUILayout.BeginHorizontal();
        GUI.enabled = personaliser.Suits.Count != 0;

        if (GUILayout.Button("<")) {
          suitIndex = suitIndex == -1 ? 0 : suitIndex;
          suitIndex = (personaliser.Suits.Count + suitIndex - 1) % personaliser.Suits.Count;

          if (isClassSelected) {
            personaliser.ClassSuits[selectedClass] = personaliser.Suits[suitIndex];
          }
          else {
            kerbalData.Suit = personaliser.Suits[suitIndex];
            kerbalData.CabinSuit = null;
          }
        }
        if (GUILayout.Button(">")) {
          suitIndex = (suitIndex + 1) % personaliser.Suits.Count;

          if (isClassSelected) {
            personaliser.ClassSuits[selectedClass] = personaliser.Suits[suitIndex];
          }
          else {
            kerbalData.Suit = personaliser.Suits[suitIndex];
            kerbalData.CabinSuit = null;
          }
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        bool hasKerbalGenericSuit = isKerbalSelected && kerbalData.Suit == null;

        GUI.color = suit == defaultSuit && !hasKerbalGenericSuit ? selectedColour : Color.white;
        if (GUILayout.Button("Default")) {
          if (isClassSelected) {
            personaliser.ClassSuits[selectedClass] = defaultSuit;
          }
          else {
            kerbalData.Suit = defaultSuit;
            kerbalData.CabinSuit = null;
          }
        }

        GUI.color = suit == null || hasKerbalGenericSuit ? selectedColour : Color.white;
        if (GUILayout.Button("Unset/Generic")) {
          if (isClassSelected) {
            personaliser.ClassSuits[selectedClass] = null;
          }
          else {
            kerbalData.Suit = null;
            kerbalData.CabinSuit = null;
          }
        }

        GUI.color = Color.white;
      }

      GUILayout.EndVertical();
      GUILayout.EndHorizontal();
      GUILayout.Space(10);

      personaliser.IsHelmetRemovalEnabled = GUILayout.Toggle(personaliser.IsHelmetRemovalEnabled,
                                                             "Remove IVA helmets in safe situations");

      personaliser.IsAtmSuitEnabled = GUILayout.Toggle(personaliser.IsAtmSuitEnabled,
                                                       "Spawn Kerbals in IVA suits when in breathable atmosphere");

      Reflections.Type reflectionType = reflections.ReflectionType;

      GUILayout.BeginHorizontal();
      GUILayout.Label("Reflections", GUILayout.Width(120));
      reflectionType = (Reflections.Type) GUILayout.SelectionGrid((int) reflectionType, reflectionTypes, 3);
      GUILayout.EndHorizontal();

      if (reflectionType != reflections.ReflectionType) {
        reflections.SetReflectionType(reflectionType);
      }

      GUILayout.EndVertical();
      GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
    }

    void Enable()
    {
      isEnabled = true;
      selectedKerbal = null;
      selectedClass = null;
    }

    void Disable()
    {
      isEnabled = false;
      selectedKerbal = null;
      selectedClass = null;

      rosterScroll = Vector2.zero;
    }

    void AddAppButton()
    {
      if (appButton == null) {
        appButton = ApplicationLauncher.Instance.AddModApplication(
          Enable, Disable, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER, appIcon);
      }
    }

    void RemoveAppButton(GameScenes scenes)
    {
      if (appButton != null) {
        ApplicationLauncher.Instance.RemoveModApplication(appButton);
        appButton = null;
      }
    }

    public void Awake()
    {
      foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("TextureReplacer"))
        Util.Parse(node.GetValue("isGUIEnabled"), ref isGuiEnabled);

      if (isGuiEnabled) {
        foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("EXPERIENCE_TRAIT")) {
          string className = node.GetValue("name");
          if (className != null) {
            classes.AddUnique(className);
          }
        }

        appIcon = GameDatabase.Instance.GetTexture(appIconPath, false);
        if (appIcon == null) {
          Util.Log("Application icon missing: {0}", appIconPath);
        }

        GameEvents.onGUIApplicationLauncherReady.Add(AddAppButton);
        GameEvents.onGameSceneLoadRequested.Add(RemoveAppButton);
      }
    }

    public void Start()
    {
      if (isGuiEnabled && ApplicationLauncher.Ready) {
        AddAppButton();
      }
    }

    public void OnGUI()
    {
      if (isEnabled) {
        GUI.skin = HighLogic.Skin;
        windowRect = GUILayout.Window(windowId, windowRect, WindowHandler, "TextureReplacer");
        windowRect.x = Math.Max(0, Math.Min(Screen.width - 30, windowRect.x));
        windowRect.y = Math.Max(0, Math.Min(Screen.height - 30, windowRect.y));
      }
    }

    public void OnDestroy()
    {
      GameEvents.onGUIApplicationLauncherReady.Remove(AddAppButton);
      GameEvents.onGameSceneLoadRequested.Remove(RemoveAppButton);
    }
  }
}
