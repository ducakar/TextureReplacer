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

    // Default textures (from `Default/`).
    public readonly Skin[] DefaultSkin = {new Skin("DEFAULT.m"), new Skin("DEFAULT.f")};
    public readonly Suit DefaultSuit = new Suit("DEFAULT");
    public readonly Suit VintageSuit = new Suit("DEFAULT.V");
    public readonly Suit FutureSuit = new Suit("DEFAULT.F");

    // All Kerbal textures, including excluded by configuration.
    public readonly List<Skin> Skins = new List<Skin>();
    public readonly List<Suit> Suits = new List<Suit>();

    // Class-specific suits.
    public readonly Dictionary<string, Suit> ClassSuits = new Dictionary<string, Suit>();

    private const string SkinsPrefix = "TextureReplacer/Skins/";
    private const string SuitsPrefix = "TextureReplacer/Suits/";

    private static readonly Log log = new Log(nameof(Mapper));

    // Personalised Kerbal textures.
    private readonly Dictionary<string, Appearance> gameKerbals = new Dictionary<string, Appearance>();
    // Global class suits.
    private readonly Dictionary<string, Suit> globalClassSuits = new Dictionary<string, Suit>();
    // Backed-up personalised textures from main configuration files. These are used to initialise kerbals if a saved
    // game doesn't contain `TRScenario`.
    private readonly ConfigNode customKerbalsNode = new ConfigNode();

    public bool PersonaliseSuit { get; set; }
    public bool HideBackpack { get; set; }

    public static void Recreate()
    {
      Instance = new Mapper();
    }

    /// <summary>
    /// Fill config for custom Kerbal skins and suits.
    /// </summary>
    public void Load()
    {
      FillSkinsAndSuits();
      FillDefaultKerbals();
      ReadKerbalsConfigs();

      // Re-read scenario if database is reloaded during the space centre scene to avoid losing all per-game settings.
      if (HighLogic.CurrentGame == null) {
        return;
      }

      ConfigNode scenarioNode = HighLogic.CurrentGame.config.GetNodes("SCENARIO")
        .FirstOrDefault(n => n.GetValue("name") == "TRScenario");

      if (scenarioNode != null) {
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
      if (gameKerbals.TryGetValue(kerbal.name, out Appearance appearance)) {
        return appearance;
      }

      string nodeValue = customKerbalsNode.GetValue(kerbal.name);
      if (nodeValue != null) {
        return GetAppearanceFromNode(kerbal, nodeValue);
      }

      appearance = new Appearance(kerbal);
      gameKerbals.Add(kerbal.name, appearance);
      return appearance;
    }

    public Skin GetDefault(Gender gender)
    {
      return DefaultSkin[(int) gender];
    }

    public Suit GetDefault(KerbalSuit kind)
    {
      return kind switch {
        KerbalSuit.Vintage => VintageSuit,
        KerbalSuit.Future  => FutureSuit,
        _                  => DefaultSuit
      };
    }

    /// <summary>
    /// Get the actual Kerbal skin. Read from appearance if not null (i.e. not generic) otherwise determined randomly.
    /// </summary>
    public Skin GetKerbalSkin(ProtoCrewMember kerbal, Appearance appearance)
    {
      if (appearance.Skin != null) {
        return appearance.Skin;
      }

      IList<Skin> availableSkins = GetAvailableSkins(kerbal, false);
      if (availableSkins.Count == 0) {
        return DefaultSkin[(int) kerbal.gender];
      }

      // Hash is multiplied with a large prime to increase randomisation, since hashes returned by `GetHashCode()` are
      // close together if strings only differ in the last (few) char(s).
      int number = (appearance.Hash * 4099) & 0x7fffffff;
      return availableSkins[number % availableSkins.Count];
    }

    /// <summary>
    /// Get the actual Kerbal suit. Read from appearance if not null (i.e. not generic) otherwise determined either from
    /// class suits or assigned randomly (based on configuration).
    /// </summary>
    public Suit GetKerbalSuit(ProtoCrewMember kerbal, Appearance appearance)
    {
      Suit suit = appearance.Suit ?? GetClassSuit(kerbal);
      if (suit != null) {
        kerbal.suit = suit.Kind;
        return suit;
      }

      IList<Suit> availableSuits = GetAvailableSuits(kerbal, false);
      if (availableSuits.Count == 0) {
        return GetDefault(kerbal.suit);
      }

      // We must use a different prime here to increase randomisation so that the same skin is not always combined with
      // the same suit.
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
                ? Skins.Where(s => s.Gender == kerbal.gender)
                : Skins.Where(s => !s.Excluded && s.Gender == kerbal.gender)).ToList();
    }

    public IList<Suit> GetAvailableSuits(ProtoCrewMember kerbal, bool allowExcluded)
    {
      return (allowExcluded
                ? Suits.Where(s => s.Kind == kerbal.suit && (s.Gender == null || s.Gender == kerbal.gender))
                : Suits.Where(s =>
                  !s.Excluded && s.Kind == kerbal.suit && (s.Gender == null || s.Gender == kerbal.gender)))
        .ToList();
    }

    /// <summary>
    /// Load per-game custom kerbals mapping.
    /// </summary>
    private void LoadKerbalsMap(ConfigNode node)
    {
      node ??= customKerbalsNode;

      KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

      foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned)) {
        if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead &&
            kerbal.type != ProtoCrewMember.KerbalType.Unowned) {
          continue;
        }

        string value = node.GetValue(kerbal.name);
        if (value == null) {
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

      foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned)) {
        if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead &&
            kerbal.type != ProtoCrewMember.KerbalType.Unowned) {
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
      string[] tokens = Util.SplitConfigValue(value);
      string skinName = tokens.Length >= 1 ? tokens[0] : null;
      string suitName = tokens.Length >= 2 ? tokens[1] : null;

      return new Appearance(kerbal) {
        Skin = skinName switch {
          null        => null,
          "GENERIC"   => null,
          "DEFAULT.m" => GetDefault(kerbal.gender),
          "DEFAULT.f" => GetDefault(kerbal.gender),
          _           => Skins.Find(h => h.Name == skinName)
        },
        Suit = suitName switch {
          null        => null,
          "GENERIC"   => null,
          "DEFAULT"   => GetDefault(kerbal.suit),
          "DEFAULT.V" => GetDefault(kerbal.suit),
          "DEFAULT.F" => GetDefault(kerbal.suit),
          _           => Suits.Find(s => s.Name == suitName)
        }
      };
    }

    /// <summary>
    /// Load suit mapping.
    /// </summary>
    private void LoadClassSuitMap(ConfigNode node, IDictionary<string, Suit> map)
    {
      if (node == null) {
        foreach ((string key, Suit value) in globalClassSuits) {
          map[key] = value;
        }
      } else {
        foreach (ConfigNode.Value entry in node.values) {
          map.Remove(entry.name);

          string suitName = entry.value;
          switch (suitName) {
            case null:
            case "GENERIC": {
              continue;
            }
            case "DEFAULT": {
              map[entry.name] = DefaultSuit;
              break;
            }
            case "DEFAULT.V": {
              map[entry.name] = VintageSuit;
              break;
            }
            case "DEFAULT.F": {
              map[entry.name] = FutureSuit;
              break;
            }
            default: {
              Suit suit = Suits.Find(s => s.Name == suitName);
              if (suit != null) {
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
      foreach ((string key, Suit value) in ClassSuits) {
        if (value != null) {
          node.AddValue(key, value.Name);
        }
      }
    }

    private void FillDefaultKerbals()
    {
      var prefab = Prefab.Instance;

      Prefab.ExtractSkin(prefab.MaleEva.transform, DefaultSkin[0]);
      Prefab.ExtractSkin(prefab.FemaleEva.transform, DefaultSkin[1]);

      Prefab.ExtractSuit(prefab.MaleIva.transform, DefaultSuit);
      Prefab.ExtractSuit(prefab.MaleEva.transform, DefaultSuit);

      if (prefab.MaleIvaVintage && prefab.MaleEvaVintage) {
        Prefab.ExtractSuit(prefab.MaleIvaVintage.transform, VintageSuit);
        Prefab.ExtractSuit(prefab.MaleEvaVintage.transform, VintageSuit);
      }

      if (prefab.MaleIvaFuture && prefab.MaleEvaFuture) {
        Prefab.ExtractSuit(prefab.MaleIvaFuture.transform, FutureSuit);
        Prefab.ExtractSuit(prefab.MaleEvaFuture.transform, FutureSuit);
      }

      // These textures cannot be found on "prefab" models, we have to add them manually.
      foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>()) {
        switch (texture.name) {
          case "paleBlueSuite_diffuse": {
            DefaultSuit.SetTexture(texture.name, texture);
            break;
          }
          case "me_suit_difuse_low_polyBrown": {
            VintageSuit.SetTexture(texture.name, texture);
            break;
          }
          case "futureSuit_diffuse_whiteOrange": {
            FutureSuit.SetTexture(texture.name, texture);
            break;
          }
        }
      }

      // Visor needs to be replaced every time, not only on the prefab models, so the visor from the default suit must
      // be set on all suits without a custom visor.
      VintageSuit.IvaVisor ??= DefaultSuit.IvaVisor;
      VintageSuit.EvaVisor ??= DefaultSuit.EvaVisor;

      FutureSuit.IvaVisor ??= DefaultSuit.IvaVisor;
      FutureSuit.EvaVisor ??= DefaultSuit.EvaVisor;

      foreach (Suit suit in Suits) {
        suit.IvaVisor ??= DefaultSuit.IvaVisor;
        suit.EvaVisor ??= DefaultSuit.EvaVisor;
      }
    }

    private void FillSkinsAndSuits()
    {
      var skinDirs = new Dictionary<string, int>();
      var suitDirs = new Dictionary<string, int>();

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture) {
        Texture2D texture = texInfo.texture;
        if (texture == null) {
          continue;
        }

        if (TryGetNameUnderPrefix(texture.name, SkinsPrefix, out string name, out string textureBaseName)) {
          if (!skinDirs.TryGetValue(name, out int index)) {
            index = Skins.Count;
            skinDirs.Add(name, index);
            Skins.Add(new Skin(name));
          }

          if (Skins[index].SetTexture(textureBaseName, texture)) {
            texture.wrapMode = TextureWrapMode.Clamp;
          } else {
            log.Print("Unknown skin texture name \"{0}\": {1}", textureBaseName, texture.name);
          }
        } else if (TryGetNameUnderPrefix(texture.name, SuitsPrefix, out name, out textureBaseName)) {
          if (!suitDirs.TryGetValue(name, out int index)) {
            index = Suits.Count;
            suitDirs.Add(name, index);
            Suits.Add(new Suit(name));
          }

          if (Suits[index].SetTexture(textureBaseName, texture)) {
            texture.wrapMode = TextureWrapMode.Clamp;
          } else {
            log.Print("Unknown suit texture name \"{0}\": {1}", textureBaseName, texture.name);
          }
        } else if (TryGetNameUnderPrefix(texture.name, Replacer.DefaultPrefix, out name, out textureBaseName)) {
          switch (textureBaseName) {
            case "eyeballLeft":
            case "eyeballRight":
            case "pupilLeft":
            case "pupilRight": {
              DefaultSkin[0].SetTexture(textureBaseName, texture);
              DefaultSkin[1].SetTexture(textureBaseName, texture);
              texture.wrapMode = TextureWrapMode.Clamp;
              break;
            }
            case "kerbalHead":
            case "kerbalHeadNRM": {
              DefaultSkin[0].SetTexture(textureBaseName, texture);
              texture.wrapMode = TextureWrapMode.Clamp;
              break;
            }
            case "kerbalGirl_06_BaseColor":
            case "kerbalGirl_06_BaseColorNRM": {
              DefaultSkin[1].SetTexture(textureBaseName, texture);
              texture.wrapMode = TextureWrapMode.Clamp;
              break;
            }
            default: {
              if (DefaultSuit.SetTexture(textureBaseName, texture)) {
                texture.wrapMode = TextureWrapMode.Clamp;
              }
              break;
            }
          }
        }
      }
    }

    /// <summary>
    /// Fill config for custom Kerbal skins and suits.
    /// </summary>
    private void ReadKerbalsConfigs()
    {
      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer")) {
        ConfigNode customNode = file.config.GetNode("CustomKerbals");
        if (customNode != null) {
          // Merge into `customKerbalsNode`.
          foreach (ConfigNode.Value entry in customNode.values) {
            customKerbalsNode.RemoveValue(entry.name);
            customKerbalsNode.AddValue(entry.name, entry.value);
          }
        }

        ConfigNode classNode = file.config.GetNode("ClassSuits");
        if (classNode != null) {
          LoadClassSuitMap(classNode, globalClassSuits);
        }
      }

      // Trim lists.
      Skins.TrimExcess();
      Suits.TrimExcess();
    }

    // Extract part of path consisting of directories under a prefix. Return false if prefix is not found.
    // This function is used to determine whether a given path contains a skin or suit texture and extracts its name.
    // E.g. for `SomeMod/TextureReplacer/Skins/MySkins/Skin01/KerbalHead` it extract skin name `MySkins/Skin01` and
    // texture name `KerbalHead` for a given prefix `TextureReplacer/Skins`.
    private static bool TryGetNameUnderPrefix(string path, string prefix, out string name, out string textureName)
    {
      int prefixIndex = path.IndexOf(prefix, StringComparison.Ordinal);
      if (prefixIndex == -1) {
        name = "";
        textureName = "";
        return false;
      }

      int prefixLength = prefixIndex + prefix.Length;
      int nameLength = path.LastIndexOf('/') - prefixLength;
      if (nameLength < 1) {
        name = "";
        textureName = "";
        return false;
      }

      name = path.Substring(prefixLength, nameLength);
      textureName = path.Substring(prefixLength + nameLength + 1);
      return true;
    }
  }
}
