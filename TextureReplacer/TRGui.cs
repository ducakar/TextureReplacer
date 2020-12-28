/*
 * Copyright © 2013-2020 Davorin Učakar
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
using System.Linq;
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
        private Rect windowRect = new Rect(Screen.width - 600, 60, 580, 600);
        private Vector2 rosterScroll = Vector2.zero;

        private bool isEnabled;

        // Classes from config files.
        private readonly List<string> classes = new List<string>();

        // Selection.
        private ProtoCrewMember selectedKerbal;
        private string selectedClass;
        private IList<Skin> availableSkins;
        private IList<Suit> availableSuits;
        private string dumpTextureName = "";

        public void Awake()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("TextureReplacer"))
            {
                Util.Parse(node.GetValue("isGUIEnabled"), ref isGuiEnabled);
            }

            if (!isGuiEnabled)
            {
                return;
            }

            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("EXPERIENCE_TRAIT"))
            {
                string className = node.GetValue("name");
                if (className != null)
                {
                    classes.AddUnique(className);
                }
            }

            appIcon = GameDatabase.Instance.GetTexture(AppIconPath, false);
            if (appIcon == null)
            {
                log.Print("Application icon missing: {0}", AppIconPath);
            }

            GameEvents.onGUIApplicationLauncherReady.Add(AddAppButton);
            GameEvents.onGameSceneLoadRequested.Add(RemoveAppButton);
        }

        public void Start()
        {
            UpdateKerbalSuitKinds();

            if (isGuiEnabled && ApplicationLauncher.Ready)
            {
                AddAppButton();
            }
        }

        public void OnGUI()
        {
            if (!isEnabled)
            {
                return;
            }

            GUI.skin     = HighLogic.Skin;
            windowRect   = GUILayout.Window(WindowId, windowRect, WindowHandler, "TextureReplacer");
            windowRect.x = Math.Max(0, Math.Min(Screen.width - 30, windowRect.x));
            windowRect.y = Math.Max(0, Math.Min(Screen.height - 30, windowRect.y));
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddAppButton);
            GameEvents.onGameSceneLoadRequested.Remove(RemoveAppButton);
        }

        private void AddAppButton()
        {
            if (appButton == null)
            {
                appButton = ApplicationLauncher.Instance.AddModApplication(
                    Enable,
                    Disable,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.SPACECENTER,
                    appIcon);
            }
        }

        private void RemoveAppButton(GameScenes scenes)
        {
            if (appButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
                appButton = null;
            }
        }

        private void Enable()
        {
            isEnabled = true;
        }

        private void Disable()
        {
            isEnabled       = false;
            selectedKerbal  = null;
            selectedClass   = null;
            availableSkins  = null;
            availableSuits  = null;
            dumpTextureName = "";

            rosterScroll = Vector2.zero;
        }

        private void WindowHandler(int id)
        {
            var mapper = Mapper.Instance;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            ShowRoster();

            // Textures.
            Appearance appearance = null;
            Skin       skin       = null;
            Suit       suit       = null;
            int        suitIndex  = -1;

            if (selectedKerbal != null)
            {
                appearance = mapper.GetAppearance(selectedKerbal);

                skin = mapper.GetKerbalSkin(selectedKerbal, appearance);
                suit = mapper.GetKerbalSuit(selectedKerbal, appearance);

                suitIndex = availableSuits.IndexOf(suit);
            }
            else if (selectedClass != null)
            {
                if (mapper.ClassSuits.TryGetValue(selectedClass, out suit))
                {
                    suitIndex = availableSuits.IndexOf(suit);
                }
            }

            GUILayout.BeginVertical();

            if (skin != null)
            {
                ShowKerbalSkinTextures(skin);
            }
            if (suit != null)
            {
                ShowKerbalSuitTextures(suit);
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(120));

            if (selectedKerbal != null && appearance != null)
            {
                ShowKerbalSkinButtons(appearance);
            }
            if (selectedKerbal != null || selectedClass != null)
            {
                ShowKerbalSuitButtons(appearance, suitIndex, suit);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            ShowOptions();
            ShowDumpTexture();

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
        }

        private void ShowRoster()
        {
            var mapper = Mapper.Instance;

            GUILayout.BeginVertical(GUILayout.Width(200));

            rosterScroll = GUILayout.BeginScrollView(rosterScroll);
            GUILayout.BeginVertical();

            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
                {
                    continue;
                }

                GUI.contentColor = kerbal.rosterStatus switch
                {
                    ProtoCrewMember.RosterStatus.Assigned => Color.cyan,
                    ProtoCrewMember.RosterStatus.Missing  => Color.yellow,
                    _                                     => Color.white
                };

                if (!GUILayout.Button(kerbal.name))
                {
                    continue;
                }

                selectedKerbal = kerbal;
                selectedClass  = null;
                availableSkins = mapper.GetAvailableSkins(kerbal, true);
                availableSuits = mapper.GetAvailableSuits(kerbal, true);
            }

            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned)
            {
                if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead)
                {
                    continue;
                }

                GUI.contentColor = Color.cyan;

                if (!GUILayout.Button(kerbal.name))
                {
                    continue;
                }

                selectedKerbal = kerbal;
                selectedClass  = null;
                availableSkins = mapper.GetAvailableSkins(kerbal, true);
                availableSuits = mapper.GetAvailableSuits(kerbal, true);
            }

            GUI.contentColor = Color.white;
            GUI.color        = ClassColour;

            // Class suits.
            foreach (string clazz in classes)
            {
                if (GUILayout.Button(clazz))
                {
                    selectedKerbal = null;
                    selectedClass  = clazz;
                    availableSkins = null;
                    availableSuits = mapper.Suits.Where(s => s.Gender == null).ToList();
                }
            }

            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (GUILayout.Button("Reset to Defaults"))
            {
                mapper.ResetKerbals();
            }

            GUILayout.EndVertical();
        }

        private static void ShowKerbalSkinTextures(Skin skin)
        {
            GUILayout.Box(skin.Head, GUILayout.Width(200), GUILayout.Height(200));
            GUILayout.Label(skin.Name);
            GUILayout.Space(10);
        }

        private void ShowKerbalSkinButtons(Appearance appearance)
        {
            var mapper = Mapper.Instance;

            GUILayout.BeginHorizontal();
            GUI.enabled = availableSkins.Count != 0;

            if (GUILayout.Button("<"))
            {
                int skinIndex = availableSkins.IndexOf(mapper.GetKerbalSkin(selectedKerbal, appearance));

                skinIndex = skinIndex == -1
                    ? availableSkins.Count - 1
                    : (skinIndex + availableSkins.Count - 1) % availableSkins.Count;
                appearance.Skin = availableSkins[skinIndex];
            }

            if (GUILayout.Button(">"))
            {
                int skinIndex = availableSkins.IndexOf(mapper.GetKerbalSkin(selectedKerbal, appearance));

                skinIndex       = skinIndex == -1 ? 0 : (skinIndex + 1) % availableSkins.Count;
                appearance.Skin = availableSkins[skinIndex];
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            Skin defaultSkin = mapper.GetDefault(selectedKerbal.gender);
            GUI.color = appearance.Skin == defaultSkin ? SelectedColour : Color.white;
            if (GUILayout.Button("Default"))
            {
                appearance.Skin = defaultSkin;
            }

            GUI.color = appearance.Skin == null ? SelectedColour : Color.white;
            if (GUILayout.Button("Unset/Generic"))
            {
                appearance.Skin = null;
            }

            GUI.color = Color.white;

            selectedKerbal.veteran = GUILayout.Toggle(selectedKerbal.veteran, "Veteran");

            GUILayout.Space(110);
        }

        private void ShowKerbalSuitTextures(Suit suit)
        {
            var mapper = Mapper.Instance;

            Texture2D ivaSuitTex;
            Texture2D evaSuitTex;

            if (selectedKerbal != null)
            {
                Suit defaultSuit = mapper.GetDefault(selectedKerbal.suit);
                ivaSuitTex = suit.GetSuit(false, selectedKerbal) ?? defaultSuit.GetSuit(false, selectedKerbal);
                evaSuitTex = suit.GetSuit(true, selectedKerbal) ?? defaultSuit.GetSuit(true, selectedKerbal);
            }
            else
            {
                Suit defaultSuit = mapper.GetDefault(suit.Kind);
                ivaSuitTex = suit.IvaSuit[0] ?? defaultSuit.IvaSuit[0];
                evaSuitTex = suit.EvaSuit[0] ?? defaultSuit.EvaSuit[0];
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box(ivaSuitTex, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.Space(10);
            GUILayout.Box(evaSuitTex, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.EndHorizontal();

            GUILayout.Label(suit.Name);
            GUILayout.Space(10);
        }

        private void ShowKerbalSuitButtons(Appearance appearance, int suitIndex, Suit suit)
        {
            var mapper = Mapper.Instance;

            GUILayout.BeginHorizontal();
            GUI.enabled = availableSuits.Count != 0;

            if (GUILayout.Button("<"))
            {
                suitIndex = suitIndex == -1
                    ? availableSuits.Count - 1
                    : (suitIndex + availableSuits.Count - 1) % availableSuits.Count;

                if (selectedKerbal != null)
                {
                    appearance.Suit = availableSuits[suitIndex];
                }
                else if (selectedClass != null)
                {
                    mapper.ClassSuits[selectedClass] = availableSuits[suitIndex];
                }
            }

            if (GUILayout.Button(">"))
            {
                suitIndex = suitIndex == -1 ? 0 : (suitIndex + 1) % availableSuits.Count;

                if (selectedKerbal != null)
                {
                    appearance.Suit = availableSuits[suitIndex];
                }
                else if (selectedClass != null)
                {
                    mapper.ClassSuits[selectedClass] = availableSuits[suitIndex];
                }
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            bool hasKerbalGenericSuit = selectedKerbal != null && appearance.Suit == null;

            Suit defaultSuit = selectedKerbal == null ? mapper.DefaultSuit : mapper.GetDefault(selectedKerbal.suit);
            GUI.color = suit == defaultSuit && !hasKerbalGenericSuit ? SelectedColour : Color.white;
            if (GUILayout.Button("Default"))
            {
                if (selectedClass != null)
                {
                    mapper.ClassSuits[selectedClass] = defaultSuit;
                }
                else
                {
                    appearance.Suit = defaultSuit;
                }
            }

            GUI.color = suit == null || hasKerbalGenericSuit ? SelectedColour : Color.white;
            if (GUILayout.Button("Unset/Generic"))
            {
                if (selectedClass != null)
                {
                    mapper.ClassSuits[selectedClass] = null;
                }
                else
                {
                    appearance.Suit = null;
                }
            }

            GUI.color = Color.white;

            // Handle radio button clicks and change to default suit if suit kind changed.
            if (selectedKerbal != null)
            {
                bool isVintage = selectedKerbal.suit == KerbalSuit.Vintage;
                bool isFuture  = selectedKerbal.suit == KerbalSuit.Future;

                isVintage = GUILayout.Toggle(isVintage, "Vintage");
                isFuture  = GUILayout.Toggle(isFuture, "Future");

                KerbalSuit? newSuit = (selectedKerbal.suit, isVintage, isFuture) switch
                {
                    (KerbalSuit.Default, true, _)      => KerbalSuit.Vintage,
                    (KerbalSuit.Default, _, true)      => KerbalSuit.Future,
                    (KerbalSuit.Vintage, false, false) => KerbalSuit.Default,
                    (KerbalSuit.Vintage, _, true)      => KerbalSuit.Future,
                    (KerbalSuit.Future, false, false)  => KerbalSuit.Default,
                    (KerbalSuit.Future, true, _)       => KerbalSuit.Vintage,
                    _                                  => null
                };

                if (newSuit.HasValue)
                {
                    selectedKerbal.suit = newSuit.Value;
                    availableSuits      = mapper.GetAvailableSuits(selectedKerbal, true);
                    appearance.Suit     = mapper.GetDefault(selectedKerbal.suit);
                }
            }
        }

        private static void ShowOptions()
        {
            var mapper      = Mapper.Instance;
            var reflections = Reflections.Instance;

            if (reflections == null)
            {
                log.Print("reflections is null!");
            }
            else
            {
                bool enableReflections = reflections.ReflectionType == Reflections.Type.Real;
                enableReflections          = GUILayout.Toggle(enableReflections, "Enable real-time reflections");
                reflections.ReflectionType = enableReflections ? Reflections.Type.Real : Reflections.Type.None;
            }

            if (mapper == null)
            {
                log.Print("mapper is null!");
            }
            else
            {
                bool personaliseSuit = mapper.PersonaliseSuit;
                personaliseSuit        = GUILayout.Toggle(personaliseSuit, "Personalise suits");
                mapper.PersonaliseSuit = personaliseSuit;

                if (mapper.IsLegacyKSP)
                {
                    bool hideBackpack = mapper.HideBackpack;
                    hideBackpack        = GUILayout.Toggle(hideBackpack, "Hide cargo and parachute backpacks");
                    mapper.HideBackpack = hideBackpack;
                }
            }
        }

        private void ShowDumpTexture()
        {
            GUILayout.BeginHorizontal();

            dumpTextureName = GUILayout.TextField(dumpTextureName, GUILayout.Height(28));
            if (GUILayout.Button("Dump", GUILayout.Width(80)))
            {
                Texture2D texture = Resources.FindObjectsOfTypeAll<Texture2D>()
                    .FirstOrDefault(t => t.name == dumpTextureName);
                if (texture != null)
                {
                    Util.DumpToPng(texture);
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Set ProtoCrewMembers to have the right suit kinds. On some occasions, e.g. when not changing suit kind
        /// through TR GUI or on new game initialisation there can appear mismatches between suit texture assignments
        /// and Kerbal models. We fix those here.
        /// </summary>
        private static void UpdateKerbalSuitKinds()
        {
            var mapper = Mapper.Instance;

            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
            {
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
                {
                    continue;
                }

                Appearance appearance = mapper.GetAppearance(kerbal);
                Suit       suit       = mapper.GetKerbalSuit(kerbal, appearance);

                if (suit != null && suit.Kind != kerbal.suit)
                {
                    kerbal.suit = suit.Kind;
                }
            }

            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Unowned)
            {
                if (kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead)
                {
                    continue;
                }

                Appearance appearance = mapper.GetAppearance(kerbal);
                Suit       suit       = mapper.GetKerbalSuit(kerbal, appearance);

                if (suit != null && suit.Kind != kerbal.suit)
                {
                    kerbal.suit = suit.Kind;
                }
            }
        }
    }
}
