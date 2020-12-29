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
using UnityEngine;
using Gender = ProtoCrewMember.Gender;
using KerbalSuit = ProtoCrewMember.KerbalSuit;

namespace TextureReplacer
{
    internal class Mapper
    {
        // Instance.
        public static Mapper Instance { get; private set; }

        // Texture map for deduplication of identical textures.
        public readonly Dictionary<string, string> TextureMap = new Dictionary<string, string>();

        // Class-specific suits.
        public readonly Dictionary<string, Suit> ClassSuits = new Dictionary<string, Suit>();

        private const string SkinsPrefix = "TextureReplacer/Skins/";
        private const string SuitsPrefix = "TextureReplacer/Suits/";

        private static readonly Log log = new Log(nameof(Mapper));

        // All Kerbal textures, including excluded by configuration.
        private readonly List<Skin> skins = new List<Skin>();
        private readonly List<Suit> suits = new List<Suit>();

        // Default textures (from `Default/`).
        private readonly Skin[] defaultSkin = {new Skin("DEFAULT.m"), new Skin("DEFAULT.f")};
        private readonly Suit defaultSuit = new Suit("DEFAULT");
        private readonly Suit vintageSuit = new Suit("DEFAULT.V");
        private readonly Suit futureSuit = new Suit("DEFAULT.F");

        // Personalised Kerbal textures.
        private readonly Dictionary<string, Appearance> gameKerbals = new Dictionary<string, Appearance>();

        // Global class suits.
        private readonly Dictionary<string, Suit> globalClassSuits = new Dictionary<string, Suit>();

        // Backed-up personalised textures from main configuration files. These are used to initialise kerbals if a
        // saved game doesn't contain `TRScenario`.
        private readonly ConfigNode customKerbalsNode = new ConfigNode();

        public bool IsLegacyKSP { get; private set; }
        public bool PersonaliseSuit { get; set; }
        public bool HideBackpack { get; set; }

        public static void Recreate()
        {
            Instance = new Mapper
            {
                IsLegacyKSP = Versioning.version_major == 1 && Versioning.version_minor <= 10
            };
        }

        public void ReadConfig(ConfigNode rootNode)
        {
            ConfigNode textureMap = rootNode.GetNode("TextureMap");
            if (textureMap == null)
            {
                return;
            }

            foreach (ConfigNode.Value pair in textureMap.values)
            {
                TextureMap[pair.name] = pair.value;
            }
        }

        /// <summary>
        /// Fill config for custom Kerbal skins and suits.
        /// </summary>
        public void Load()
        {
            FillSkinsAndSuits();
            FillDefaultKerbals();
            ReadKerbalsConfigs();

            // Re-read scenario if database is reloaded during the space centre scene to avoid losing all per-game
            // settings.
            if (HighLogic.CurrentGame == null)
            {
                return;
            }

            ConfigNode scenarioNode = HighLogic.CurrentGame.config.GetNodes("SCENARIO")
                .FirstOrDefault(n => n.GetValue("name") == "TRScenario");

            if (scenarioNode != null)
            {
                OnLoadScenario(scenarioNode);
            }
        }

        public void OnLoadScenario(ConfigNode node)
        {
            bool personaliseSuit = true;
            Util.Parse(node.GetValue("personaliseSuit"), ref personaliseSuit);
            PersonaliseSuit = personaliseSuit;

            bool hideBackpack = false;
            Util.Parse(node.GetValue("hideBackpack"), ref hideBackpack);
            HideBackpack = hideBackpack;

            gameKerbals.Clear();
            ClassSuits.Clear();
            LoadKerbalsMap(node.GetNode("Kerbals"));
            LoadClassSuitMap(node.GetNode("ClassSuits"), ClassSuits);
        }

        public void OnSaveScenario(ConfigNode node)
        {
            node.ClearNodes();
            node.AddValue("personaliseSuit", PersonaliseSuit);
            node.AddValue("hideBackpack", HideBackpack);

            SaveKerbalsMap(node.AddNode("Kerbals"));
            SaveClassSuitMap(node.AddNode("ClassSuits"));
        }

        public void ResetKerbals()
        {
            gameKerbals.Clear();
            ClassSuits.Clear();
            LoadKerbalsMap(null);
            LoadClassSuitMap(null, ClassSuits);
        }

        /// <summary>
        /// Get appearance of a Kerbal. If the skin or the suit is generic it is set to null. If an appearance does not
        /// yet exist for that Kerbal it is created.
        /// </summary>
        public Appearance GetAppearance(ProtoCrewMember kerbal)
        {
            if (gameKerbals.TryGetValue(kerbal.name, out Appearance appearance))
            {
                return appearance;
            }

            string nodeValue = customKerbalsNode.GetValue(kerbal.name);
            if (nodeValue != null)
            {
                return GetAppearanceFromNode(kerbal, nodeValue);
            }

            appearance = new Appearance(kerbal);
            gameKerbals.Add(kerbal.name, appearance);
            return appearance;
        }

        public Skin GetDefaultSkin(Gender gender)
        {
            return defaultSkin[(int) gender];
        }

        public Suit GetDefaultSuit(KerbalSuit? kind)
        {
            return kind switch
            {
                KerbalSuit.Vintage => vintageSuit,
                KerbalSuit.Future  => futureSuit,
                _                  => defaultSuit
            };
        }

        /// <summary>
        /// Get the actual Kerbal skin. Read from appearance if not null (i.e. not generic) otherwise determined
        /// randomly.
        /// </summary>
        public Skin GetKerbalSkin(ProtoCrewMember kerbal, Appearance appearance)
        {
            if (appearance.Skin != null)
            {
                return appearance.Skin;
            }

            IList<Skin> availableSkins = GetAvailableSkins(kerbal, false);
            if (availableSkins.Count == 0)
            {
                return defaultSkin[(int) kerbal.gender];
            }

            // Hash is multiplied with a large prime to increase randomisation, since hashes returned by `GetHashCode()`
            // are close together if strings only differ in the last (few) char(s).
            int number = (appearance.Hash * 4099) & 0x7fffffff;
            return availableSkins[number % availableSkins.Count];
        }

        /// <summary>
        /// Get the actual Kerbal suit. Read from appearance if not null (i.e. not generic) otherwise determined either
        /// from class suits or assigned randomly (based on configuration).
        /// </summary>
        public Suit GetKerbalSuit(ProtoCrewMember kerbal, Appearance appearance)
        {
            Suit suit = appearance.Suit ?? GetClassSuit(kerbal);
            if (suit != null)
            {
                kerbal.suit = suit.Kind;
                return suit;
            }

            IList<Suit> availableSuits = GetAvailableSuits(kerbal, false);
            if (availableSuits.Count == 0)
            {
                return GetDefaultSuit(kerbal.suit);
            }

            // We must use a different prime here to increase randomisation so that the same skin is not always combined
            // with the same suit.
            int number = (appearance.Hash * 2053) & 0x7fffffff;
            return availableSuits[number % availableSuits.Count];
        }

        private Suit GetClassSuit(ProtoCrewMember kerbal)
        {
            ClassSuits.TryGetValue(kerbal.experienceTrait.Config.Name, out Suit suit);
            return suit;
        }

        public IList<Skin> GetAvailableSkins(ProtoCrewMember kerbal, bool allowExcluded)
        {
            return (allowExcluded
                ? skins.Where(s => s.Gender == kerbal.gender)
                : skins.Where(s => !s.Excluded && s.Gender == kerbal.gender)).ToList();
        }

        public IList<Suit> GetAvailableSuits(ProtoCrewMember kerbal, bool allowExcluded)
        {
            return (allowExcluded
                    ? suits.Where(s => s.Kind == kerbal.suit && (s.Gender == null || s.Gender == kerbal.gender))
                    : suits.Where(s =>
                                      !s.Excluded && s.Kind == kerbal.suit &&
                                      (s.Gender == null || s.Gender == kerbal.gender)))
                .ToList();
        }

        public List<Suit> GetGenderlessSuits()
        {
            return suits.Where(s => s.Gender == null).ToList();
        }

        /// <summary>
        /// Load per-game custom kerbals mapping.
        /// </summary>
        private void LoadKerbalsMap(ConfigNode node)
        {
            node ??= customKerbalsNode;

            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned))
            {
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead &&
                    kerbal.type != ProtoCrewMember.KerbalType.Unowned)
                {
                    continue;
                }

                string value = node.GetValue(kerbal.name);
                if (value == null)
                {
                    continue;
                }

                gameKerbals[kerbal.name] = GetAppearanceFromNode(kerbal, value);
            }
        }

        /// <summary>
        /// Save per-game custom Kerbals mapping.
        /// </summary>
        private void SaveKerbalsMap(ConfigNode node)
        {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned))
            {
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead &&
                    kerbal.type != ProtoCrewMember.KerbalType.Unowned)
                {
                    continue;
                }

                Appearance appearance = GetAppearance(kerbal);

                string skinName = appearance.Skin?.Name ?? "GENERIC";
                string suitName = appearance.Suit?.Name ?? "GENERIC";

                node.AddValue(kerbal.name, skinName + " " + suitName);
            }
        }

        private Appearance GetAppearanceFromNode(ProtoCrewMember kerbal, string value)
        {
            string[] tokens   = Util.SplitConfigValue(value);
            string   skinName = tokens.Length >= 1 ? tokens[0] : null;
            string   suitName = tokens.Length >= 2 ? tokens[1] : null;

            return new Appearance(kerbal)
            {
                Skin = skinName switch
                {
                    null        => null,
                    "GENERIC"   => null,
                    "DEFAULT.m" => GetDefaultSkin(kerbal.gender),
                    "DEFAULT.f" => GetDefaultSkin(kerbal.gender),
                    _           => skins.Find(h => h.Name == skinName)
                },
                Suit = suitName switch
                {
                    null        => null,
                    "GENERIC"   => null,
                    "DEFAULT"   => GetDefaultSuit(kerbal.suit),
                    "DEFAULT.V" => GetDefaultSuit(kerbal.suit),
                    "DEFAULT.F" => GetDefaultSuit(kerbal.suit),
                    _           => suits.Find(s => s.Name == suitName)
                }
            };
        }

        /// <summary>
        /// Load suit mapping.
        /// </summary>
        private void LoadClassSuitMap(ConfigNode node, IDictionary<string, Suit> map)
        {
            if (node == null)
            {
                foreach ((string key, Suit value) in globalClassSuits)
                {
                    map[key] = value;
                }
            }
            else
            {
                foreach (ConfigNode.Value entry in node.values)
                {
                    map.Remove(entry.name);

                    string suitName = entry.value;
                    switch (suitName)
                    {
                        case null:
                        case "GENERIC":
                        {
                            continue;
                        }
                        case "DEFAULT":
                        {
                            map[entry.name] = defaultSuit;
                            break;
                        }
                        case "DEFAULT.V":
                        {
                            map[entry.name] = vintageSuit;
                            break;
                        }
                        case "DEFAULT.F":
                        {
                            map[entry.name] = futureSuit;
                            break;
                        }
                        default:
                        {
                            Suit suit = suits.Find(s => s.Name == suitName);
                            if (suit != null)
                            {
                                map[entry.name] = suit;
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save suit mapping.
        /// </summary>
        private void SaveClassSuitMap(ConfigNode node)
        {
            foreach ((string key, Suit value) in ClassSuits)
            {
                if (value != null)
                {
                    node.AddValue(key, value.Name);
                }
            }
        }

        private void FillSkinsAndSuits()
        {
            foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
            {
                Texture2D texture = texInfo.texture;
                if (texture != null)
                {
                    FillSkinAndSuitTexture(texture.name, texture);
                }
            }

            foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                foreach ((string key, string value) in TextureMap)
                {
                    if (value == texture.name)
                    {
                        FillSkinAndSuitTexture(Replacer.DefaultPrefix + key, texture);
                    }
                }
            }
        }

        private void FillSkinAndSuitTexture(string textureName, Texture2D texture)
        {
            if (TryGetNameUnderPrefix(textureName, SkinsPrefix, out string name, out string textureBaseName))
            {
                Skin skin = skins.FirstOrDefault(s => s.Name == name);
                if (skin == null)
                {
                    skin = new Skin(name);
                    skins.Add(skin);
                }

                if (skin.SetTexture(textureBaseName, texture))
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
                else
                {
                    log.Print("Unknown skin texture name \"{0}\": {1}", textureBaseName, textureName);
                }
            }
            else if (TryGetNameUnderPrefix(textureName, SuitsPrefix, out name, out textureBaseName))
            {
                Suit suit = suits.FirstOrDefault(s => s.Name == name);
                if (suit == null)
                {
                    suit = new Suit(name);
                    suits.Add(suit);
                }

                if (suit.SetTexture(textureBaseName, texture))
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
                else
                {
                    log.Print("Unknown suit texture name \"{0}\": {1}", textureBaseName, textureName);
                }
            }
            else if (TryGetNameUnderPrefix(textureName, Replacer.DefaultPrefix, out textureBaseName))
            {
                switch (textureBaseName)
                {
                    case "eyeballLeft":
                    case "eyeballRight":
                    case "pupilLeft":
                    case "pupilRight":
                    {
                        defaultSkin[0].SetTexture(textureBaseName, texture);
                        defaultSkin[1].SetTexture(textureBaseName, texture);
                        texture.wrapMode = TextureWrapMode.Clamp;
                        break;
                    }
                    case "kerbalHead":
                    case "kerbalHeadNRM":
                    {
                        defaultSkin[0].SetTexture(textureBaseName, texture);
                        texture.wrapMode = TextureWrapMode.Clamp;
                        break;
                    }
                    case "kerbalGirl_06_BaseColor":
                    case "kerbalGirl_06_BaseColorNRM":
                    {
                        defaultSkin[1].SetTexture(textureBaseName, texture);
                        texture.wrapMode = TextureWrapMode.Clamp;
                        break;
                    }
                    default:
                    {
                        if (defaultSuit.SetTexture(textureBaseName, texture))
                        {
                            texture.wrapMode = TextureWrapMode.Clamp;
                        }
                        break;
                    }
                }
            }
        }

        private void FillDefaultKerbals()
        {
            var prefab = Prefab.Instance;

            Prefab.ExtractSkin(prefab.MaleEva.transform, defaultSkin[0]);
            Prefab.ExtractSkin(prefab.FemaleEva.transform, defaultSkin[1]);

            Prefab.ExtractSuit(prefab.MaleIva.transform, defaultSuit);
            Prefab.ExtractSuit(prefab.MaleEva.transform, defaultSuit);

            if (prefab.MaleIvaVintage != null && prefab.MaleEvaVintage != null)
            {
                Prefab.ExtractSuit(prefab.MaleIvaVintage.transform, vintageSuit);
                Prefab.ExtractSuit(prefab.MaleEvaVintage.transform, vintageSuit);
            }

            if (prefab.MaleIvaFuture != null && prefab.MaleEvaFuture != null)
            {
                Prefab.ExtractSuit(prefab.MaleIvaFuture.transform, futureSuit);
                Prefab.ExtractSuit(prefab.MaleEvaFuture.transform, futureSuit);
            }

            // These textures cannot be found on "prefab" models, we have to add them manually.
            foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                switch (texture.name)
                {
                    case "paleBlueSuite_diffuse":
                    {
                        defaultSuit.SetTexture(texture.name, texture);
                        break;
                    }
                    case "me_suit_difuse_low_polyBrown":
                    {
                        vintageSuit.SetTexture(texture.name, texture);
                        break;
                    }
                    case "futureSuit_diffuse_whiteOrange":
                    {
                        futureSuit.SetTexture(texture.name, texture);
                        break;
                    }
                }
            }

            // Visor needs to be replaced every time, not only on the prefab models, so the visor from the default suit must
            // be set on all suits without a custom visor.
            vintageSuit.IvaVisor ??= defaultSuit.IvaVisor;
            vintageSuit.EvaVisor ??= defaultSuit.EvaVisor;

            futureSuit.IvaVisor ??= defaultSuit.IvaVisor;
            futureSuit.EvaVisor ??= defaultSuit.EvaVisor;

            foreach (Suit suit in suits)
            {
                suit.IvaVisor ??= defaultSuit.IvaVisor;
                suit.EvaVisor ??= defaultSuit.EvaVisor;
            }
        }

        /// <summary>
        /// Fill config for custom Kerbal skins and suits.
        /// </summary>
        private void ReadKerbalsConfigs()
        {
            foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer"))
            {
                ConfigNode customNode = file.config.GetNode("CustomKerbals");
                if (customNode != null)
                {
                    // Merge into `customKerbalsNode`.
                    foreach (ConfigNode.Value entry in customNode.values)
                    {
                        customKerbalsNode.RemoveValue(entry.name);
                        customKerbalsNode.AddValue(entry.name, entry.value);
                    }
                }

                ConfigNode classNode = file.config.GetNode("ClassSuits");
                if (classNode != null)
                {
                    LoadClassSuitMap(classNode, globalClassSuits);
                }
            }

            // Trim lists.
            skins.TrimExcess();
            suits.TrimExcess();
        }

        /// <summary>
        /// Extract part of path consisting of directories under a prefix. Return false if prefix is not found. This
        /// function is used to determine whether a given path contains a skin or suit texture and extracts its name.
        /// E.g. for `SomeMod/TextureReplacer/Skins/MySkins/Skin01/KerbalHead` it extracts skin name `MySkins/Skin01`
        /// and texture name `KerbalHead` for a given prefix `TextureReplacer/Skins`.
        /// </summary>
        private static bool TryGetNameUnderPrefix(string path, string prefix, out string name, out string textureName)
        {
            int prefixIndex = path.IndexOf(prefix, StringComparison.Ordinal);
            if (prefixIndex == -1)
            {
                name        = "";
                textureName = "";
                return false;
            }

            int prefixEnd  = prefixIndex + prefix.Length;
            int nameLength = path.LastIndexOf('/') - prefixEnd;
            if (nameLength < 1)
            {
                name        = "";
                textureName = "";
                return false;
            }

            name        = path.Substring(prefixEnd, nameLength);
            textureName = path.Substring(prefixEnd + nameLength + 1);
            return true;
        }

        private static bool TryGetNameUnderPrefix(string path, string prefix, out string textureName)
        {
            int prefixIndex = path.IndexOf(prefix, StringComparison.Ordinal);
            if (prefixIndex == -1)
            {
                textureName = "";
                return false;
            }

            int prefixEnd = prefixIndex + prefix.Length;
            textureName = path.Substring(prefixEnd);
            return true;
        }
    }
}
