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

using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;
using KerbalSuit = ProtoCrewMember.KerbalSuit;

namespace TextureReplacer
{
  [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
  public class TRGui : MonoBehaviour
  {
    private const int WindowId = 107056;
    private const string AppIconPath = Util.Directory + "Plugins/appIcon";
    private static readonly Color SelectedColour = new Color(0.7f, 0.9f, 1.0f);
    private static readonly Color ClassColour = new Color(1.0f, 0.8f, 1.0f);

    private static readonly Log log = new Log(nameof(TRGui));

    // Application launcher icon.
    private Texture2D appIcon;
    private ApplicationLauncherButton appButton;
    private bool isGuiEnabled = true;
    // UI state.
    private Rect windowRect = new Rect(Screen.width - 600, 60, 580, 580);
    private Vector2 rosterScroll = Vector2.zero;
    private bool isEnabled;
    // Classes from config files.
    private readonly List<string> classes = new List<string>();
    // Selection.
    private ProtoCrewMember selectedKerbal;
    private string selectedClass;

    public void Awake()
    {
      foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("TextureReplacer")) {
        Util.Parse(node.GetValue("isGUIEnabled"), ref isGuiEnabled);
      }

      if (!isGuiEnabled) {
        return;
      }

      foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("EXPERIENCE_TRAIT")) {
        string className = node.GetValue("name");
        if (className != null) {
          classes.AddUnique(className);
        }
      }

      appIcon = GameDatabase.Instance.GetTexture(AppIconPath, false);
      if (appIcon == null) {
        log.Print("Application icon missing: {0}", AppIconPath);
      }

      GameEvents.onGUIApplicationLauncherReady.Add(AddAppButton);
      GameEvents.onGameSceneLoadRequested.Add(RemoveAppButton);
    }

    public void Start()
    {
      if (isGuiEnabled && ApplicationLauncher.Ready) {
        AddAppButton();
      }
    }

    public void OnGUI()
    {
      if (!isEnabled) {
        return;
      }

      GUI.skin = HighLogic.Skin;
      windowRect = GUILayout.Window(WindowId, windowRect, WindowHandler, "TextureReplacer");
      windowRect.x = Math.Max(0, Math.Min(Screen.width - 30, windowRect.x));
      windowRect.y = Math.Max(0, Math.Min(Screen.height - 30, windowRect.y));
    }

    public void OnDestroy()
    {
      GameEvents.onGUIApplicationLauncherReady.Remove(AddAppButton);
      GameEvents.onGameSceneLoadRequested.Remove(RemoveAppButton);
    }

    private void WindowHandler(int id)
    {
      var mapper = Mapper.Instance;
      var reflections = Reflections.Instance;

      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();

      GUILayout.BeginVertical(GUILayout.Width(200));

      // Roster area.
      rosterScroll = GUILayout.BeginScrollView(rosterScroll);
      GUILayout.BeginVertical();

      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew) {
        switch (kerbal.rosterStatus) {
          case ProtoCrewMember.RosterStatus.Assigned: {
            GUI.contentColor = Color.cyan;
            break;
          }
          case ProtoCrewMember.RosterStatus.Dead: {
            continue;
          }
          case ProtoCrewMember.RosterStatus.Missing: {
            GUI.contentColor = Color.yellow;
            break;
          }
          default: {
            GUI.contentColor = Color.white;
            break;
          }
        }

        if (GUILayout.Button(kerbal.name)) {
          selectedKerbal = kerbal;
          selectedClass = null;
        }
      }

      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned) {
        if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead) {
          continue;
        }

        GUI.contentColor = Color.cyan;
        if (GUILayout.Button(kerbal.name)) {
          selectedKerbal = kerbal;
          selectedClass = null;
        }
      }

      GUI.contentColor = Color.white;
      GUI.color = ClassColour;

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
        mapper.ResetKerbals();
      }

      GUILayout.EndVertical();

      // Textures.
      Appearance appearance = null;
      Skin skin = null;
      Suit suit = null;
      int skinIndex = -1;
      int suitIndex = -1;
      Skin defaultSkin = mapper.DefaultSkin[0];
      Suit defaultSuit = mapper.DefaultSuit;

      if (selectedKerbal != null) {
        appearance = mapper.GetAppearance(selectedKerbal);

        skin = mapper.GetKerbalSkin(selectedKerbal, appearance);
        suit = mapper.GetKerbalSuit(selectedKerbal, appearance);

        skinIndex = mapper.Skins.IndexOf(skin);
        suitIndex = mapper.Suits.IndexOf(suit);

        defaultSkin = mapper.DefaultSkin[(int) selectedKerbal.gender];
        defaultSuit = selectedKerbal.suit switch {
          KerbalSuit.Vintage => mapper.VintageSuit,
          KerbalSuit.Future  => mapper.FutureSuit,
          _                  => mapper.DefaultSuit
        };
      } else if (selectedClass != null) {
        mapper.ClassSuits.TryGetValue(selectedClass, out suit);

        if (suit != null) {
          suitIndex = mapper.Suits.IndexOf(suit);
        }
      }

      GUILayout.Space(10);
      GUILayout.BeginVertical();

      if (skin != null) {
        GUILayout.Box(skin.Head, GUILayout.Width(200), GUILayout.Height(200));

        GUILayout.Label(skin.Name);
      }

      if (suit != null) {
        Texture2D ivaSuitTex = suit.GetSuit(false, selectedKerbal) ?? defaultSuit.GetSuit(false, selectedKerbal);
        Texture2D evaSuitTex = suit.GetSuit(true, selectedKerbal) ?? defaultSuit.GetSuit(true, selectedKerbal);

        GUILayout.BeginHorizontal();
        GUILayout.Box(ivaSuitTex, GUILayout.Width(100), GUILayout.Height(100));
        GUILayout.Space(10);
        GUILayout.Box(evaSuitTex, GUILayout.Width(100), GUILayout.Height(100));
        GUILayout.EndHorizontal();

        GUILayout.Label(suit.Name);
      }

      GUILayout.EndVertical();
      GUILayout.BeginVertical(GUILayout.Width(120));

      if (appearance != null) {
        GUILayout.BeginHorizontal();
        GUI.enabled = mapper.Skins.Count != 0;

        if (GUILayout.Button("<")) {
          skinIndex = skinIndex == -1 ? 0 : skinIndex;
          skinIndex = (mapper.Skins.Count + skinIndex - 1) % mapper.Skins.Count;

          appearance.Skin = mapper.Skins[skinIndex];
        }

        if (GUILayout.Button(">")) {
          skinIndex = (skinIndex + 1) % mapper.Skins.Count;

          appearance.Skin = mapper.Skins[skinIndex];
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUI.color = appearance.Skin == defaultSkin ? SelectedColour : Color.white;
        if (GUILayout.Button("Default")) {
          appearance.Skin = defaultSkin;
        }

        GUI.color = appearance.Skin == null ? SelectedColour : Color.white;
        if (GUILayout.Button("Unset/Generic")) {
          appearance.Skin = null;
        }

        GUI.color = Color.white;

        selectedKerbal.veteran = GUILayout.Toggle(selectedKerbal.veteran, "Veteran");
      }

      if (appearance != null || selectedClass != null) {
        bool isVintage = false;
        bool isFuture = false;

        if (appearance != null) {
          isVintage = selectedKerbal.suit == KerbalSuit.Vintage;
          isFuture = selectedKerbal.suit == KerbalSuit.Future;

          GUILayout.Space(100);
        }

        GUILayout.BeginHorizontal();
        GUI.enabled = mapper.Suits.Count != 0;

        if (GUILayout.Button("<")) {
          suitIndex = suitIndex == -1 ? 0 : suitIndex;
          suitIndex = (mapper.Suits.Count + suitIndex - 1) % mapper.Suits.Count;

          if (appearance != null) {
            appearance.Suit = mapper.Suits[suitIndex];
          } else if (selectedClass != null) {
            mapper.ClassSuits[selectedClass] = mapper.Suits[suitIndex];
          }
        }

        if (GUILayout.Button(">")) {
          suitIndex = (suitIndex + 1) % mapper.Suits.Count;

          if (appearance != null) {
            appearance.Suit = mapper.Suits[suitIndex];
          } else if (selectedClass != null) {
            mapper.ClassSuits[selectedClass] = mapper.Suits[suitIndex];
          }
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        bool hasKerbalGenericSuit = appearance != null && appearance.Suit == null;

        GUI.color = suit == defaultSuit && !hasKerbalGenericSuit ? SelectedColour : Color.white;
        if (GUILayout.Button("Default")) {
          if (selectedClass != null) {
            mapper.ClassSuits[selectedClass] = defaultSuit;
          } else {
            appearance.Suit = defaultSuit;
          }
        }

        GUI.color = suit == null || hasKerbalGenericSuit ? SelectedColour : Color.white;
        if (GUILayout.Button("Unset/Generic")) {
          if (selectedClass != null) {
            mapper.ClassSuits[selectedClass] = null;
          } else {
            appearance.Suit = null;
          }
        }

        GUI.color = Color.white;

        if (appearance != null) {
          isVintage = GUILayout.Toggle(isVintage, "Vintage");
          isFuture = GUILayout.Toggle(isFuture, "Future");

          selectedKerbal.suit = isVintage && isFuture
            ? selectedKerbal.suit == KerbalSuit.Vintage
              ? KerbalSuit.Future
              : KerbalSuit.Vintage
            : isVintage
              ? KerbalSuit.Vintage
              : isFuture
                ? KerbalSuit.Future
                : KerbalSuit.Default;

          // Ensure default suits are switched, otherwise we'd get stuck on a specific default suit, unable to switch
          // suit kind as `Mapper.GetKerbalSuit()` would keep resetting it.
          if (appearance.Suit == mapper.DefaultSuit || appearance.Suit == mapper.VintageSuit ||
              appearance.Suit == mapper.FutureSuit) {
            appearance.Suit = mapper.GetDefaultSuit(selectedKerbal);
          }
        }
      }

      GUILayout.EndVertical();
      GUILayout.EndHorizontal();
      GUILayout.Space(5);

      bool enableReflections = reflections.ReflectionType == Reflections.Type.Real;
      enableReflections = GUILayout.Toggle(enableReflections, "Enable real-time reflections");
      reflections.ReflectionType = enableReflections ? Reflections.Type.Real : Reflections.Type.None;

      bool hideBackpack = mapper.HideBackpack;
      hideBackpack = GUILayout.Toggle(hideBackpack, "Hide parachute/cargo backpack");
      mapper.HideBackpack = hideBackpack;

      GUILayout.EndVertical();
      GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
    }

    private void Enable()
    {
      isEnabled = true;
      selectedKerbal = null;
      selectedClass = null;
    }

    private void Disable()
    {
      isEnabled = false;
      selectedKerbal = null;
      selectedClass = null;

      rosterScroll = Vector2.zero;
    }

    private void AddAppButton()
    {
      if (appButton == null) {
        appButton = ApplicationLauncher.Instance.AddModApplication(Enable, Disable, null, null, null, null,
          ApplicationLauncher.AppScenes.SPACECENTER, appIcon);
      }
    }

    private void RemoveAppButton(GameScenes scenes)
    {
      if (appButton != null) {
        ApplicationLauncher.Instance.RemoveModApplication(appButton);
        appButton = null;
      }
    }
  }
}
