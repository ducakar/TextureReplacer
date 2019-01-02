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
using System.Linq;
using UnityEngine;
using Gender = ProtoCrewMember.Gender;
using KerbalSuit = ProtoCrewMember.KerbalSuit;

namespace TextureReplacer
{
  class Personaliser
  {
    const string SkinsPrefix = "TextureReplacer/Skins/";
    const string SuitsPrefix = "TextureReplacer/Suits/";

    static readonly Log log = new Log(nameof(Personaliser));

    // Male/female textures (minus excluded).
    readonly List<Skin>[] kerbalSkins = { new List<Skin>(), new List<Skin>() };
    readonly List<Suit>[] kerbalSuits = { new List<Suit>(), new List<Suit>() };
    // Personalised Kerbal textures.
    readonly Dictionary<string, Appearance> gameKerbals = new Dictionary<string, Appearance>();

    // Backed-up personalised textures from main configuration files. These are used to initialise kerbals if a saved
    // game doesn't contain `TRScenario`.
    readonly ConfigNode customKerbalsNode = new ConfigNode();

    public bool HideParachuteBackpack { get; set; }

    // Instance.
    public static Personaliser Instance { get; private set; }

    // Default textures (from `Default/`).
    public readonly Skin[] DefaultSkin = {
      new Skin { Name = "DEFAULT", IsDefault = true },
      new Skin { Name = "DEFAULT", IsDefault = true }
    };
    public readonly Suit DefaultSuit = new Suit { Name = "DEFAULT", IsDefault = true };
    public readonly Suit VintageSuit = new Suit { Name = "VINTAGE", IsDefault = true };

    // All Kerbal textures, including excluded by configuration.
    public readonly List<Skin> Skins = new List<Skin>();
    public readonly List<Suit> Suits = new List<Suit>();

    // Class-specific suits.
    public readonly Dictionary<string, Suit> ClassSuits = new Dictionary<string, Suit>();
    public readonly Dictionary<string, Suit> DefaultClassSuits = new Dictionary<string, Suit>();

    Suit GetClassSuit(ProtoCrewMember kerbal)
    {
      ClassSuits.TryGetValue(kerbal.experienceTrait.Config.Name, out Suit suit);
      return suit;
    }

    /// <summary>
    /// Get appearance of a Kerbal. If the skin or the suit is generic it is set to null. If an appearance does not
    /// yet exist for that Kerbal it is created.
    /// </summary>
    public Appearance GetAppearance(ProtoCrewMember kerbal)
    {
      if (!gameKerbals.TryGetValue(kerbal.name, out Appearance appearance)) {
        appearance = new Appearance {
          Hash = kerbal.name.GetHashCode()
        };
        gameKerbals.Add(kerbal.name, appearance);
      }
      return appearance;
    }

    /// <summary>
    /// Get the actual Kerbal skin. Read from appearance if not null (i.e. not generic) otherwise determined randomly.
    /// </summary>
    public Skin GetKerbalSkin(ProtoCrewMember kerbal, Appearance appearance)
    {
      if (appearance.Skin != null) {
        return appearance.Skin;
      }

      List<Skin> genderSkins = kerbalSkins[(int)kerbal.gender];
      if (genderSkins.Count == 0) {
        return DefaultSkin[(int)kerbal.gender];
      }

      // Hash is multiplied with a large prime to increase randomisation, since hashes returned by `GetHashCode()` are
      // close together if strings only differ in the last (few) char(s).
      int number = (appearance.Hash * 4099) & 0x7fffffff;
      return genderSkins[number % genderSkins.Count];
    }

    /// <summary>
    /// Get the actual Kerbal suit. Read from appearance if not null (i.e. not generic) otherwise determined either from
    /// class suits or assigned randomly (based on configuration).
    /// </summary>
    public Suit GetKerbalSuit(ProtoCrewMember kerbal, Appearance appearance)
    {
      Suit suit = appearance.Suit ?? GetClassSuit(kerbal);
      if (suit != null) {
        return suit;
      }

      List<Suit> genderSuits = kerbalSuits[(int)kerbal.gender];
      if (genderSuits.Count == 0) {
        return kerbal.suit == KerbalSuit.Vintage ? VintageSuit : DefaultSuit;
      }

      // We must use a different prime here to increase randomisation so that the same skin is not always combined with
      // the same suit.
      int number = (appearance.Hash * 2053) & 0x7fffffff;
      return genderSuits[number % genderSuits.Count];
    }

    /// <summary>
    /// Replace textures on a Kerbal model.
    /// </summary>
    void PersonaliseKerbal(Component component, ProtoCrewMember kerbal, bool isEva, bool useEvaSuit)
    {
      Appearance appearance = GetAppearance(kerbal);
      bool isVintage = kerbal.suit == KerbalSuit.Vintage;

      Skin skin = GetKerbalSkin(kerbal, appearance);
      Suit suit = GetKerbalSuit(kerbal, appearance);
      Suit defaultSuit = isVintage ? VintageSuit : DefaultSuit;

      Transform model = isEva || !isVintage ? component.transform.Find("model01")
        : component.transform.Find("kbIVA@idle/model01");
      Transform flag = isEva ? component.transform.Find("model/kbEVA_flagDecals") : null;
      Transform parachute = isEva ? component.transform.Find("model/EVAparachute/base") : null;

      // We determine body and helmet texture here to avoid code duplication between suit and helmet cases in the
      // following switch.
      // Setting the suit explicitly -- even when default -- is necessary for two reasons: to fix IVA suits after KSP
      // resetting them to the stock ones all the time and to fix the switch to default texture on start of EVA walk or
      // EVA suit toggle.
      Texture2D suitTexture = suit.GetSuit(useEvaSuit, kerbal) ?? defaultSuit.GetSuit(useEvaSuit, kerbal);
      Texture2D suitNormalMap = suit.GetSuitNRM(useEvaSuit) ?? defaultSuit.GetSuitNRM(useEvaSuit);

      if (isEva) {
        flag.GetComponent<Renderer>().enabled = useEvaSuit;
        parachute.GetComponent<Renderer>().enabled = useEvaSuit && !HideParachuteBackpack;
      }

      // We must include hidden meshes, since flares are hidden when light is turned off.
      // All other meshes are always visible, so no performance hit here.
      foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) {
        var smr = renderer as SkinnedMeshRenderer;

        // Headlight flares and thruster jets.
        if (smr == null) {
          renderer.enabled = useEvaSuit;
          continue;
        }

        Texture2D newTexture = null;
        Texture2D newNormalMap = null;

        switch (smr.name) {
          case "eyeballLeft":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
            } else {
              newTexture = skin.EyeballLeft;
              // Vintage IVA is missing a proto-model so it has to be replaced always.
              if (!isEva && isVintage) {
                smr.material.shader = Replacer.EyeShader;
                newTexture = newTexture ?? DefaultSkin[(int)kerbal.gender].EyeballLeft;
              }
            }
            break;

          case "eyeballRight":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
            } else {
              newTexture = skin.EyeballRight;
              // Vintage IVA is missing a proto-model so it has to be replaced always.
              if (!isEva && isVintage) {
                smr.material.shader = Replacer.EyeShader;
                newTexture = newTexture ?? DefaultSkin[(int)kerbal.gender].EyeballRight;
              }
            }
            break;

          case "pupilLeft":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
            } else {
              newTexture = skin.PupilLeft;
              // Vintage IVA is missing a proto-model so it has to be replaced always.
              if (!isEva && isVintage) {
                smr.material.shader = Replacer.EyeShader;
                newTexture = newTexture ?? DefaultSkin[(int)kerbal.gender].PupilLeft;
              }
              if (newTexture != null) {
                smr.material.color = Color.white;
              }
            }
            break;

          case "pupilRight":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
            } else {
              newTexture = skin.PupilRight;
              // Vintage IVA is missing a proto-model so it has to be replaced always.
              if (!isEva && isVintage) {
                smr.material.shader = Replacer.EyeShader;
                newTexture = newTexture ?? DefaultSkin[(int)kerbal.gender].PupilRight;
              }
              if (newTexture != null) {
                smr.material.color = Color.white;
              }
            }
            break;

          case "headMesh01":
          case "headMesh02":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
            if (!skin.IsDefault) {
              newTexture = skin.Head;
            }
            break;

          case "tongue":
          case "upTeeth01":
          case "upTeeth02":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
            break;

          case "body01":
          case "mesh_female_kerbalAstronaut01_body01":
            newTexture = suitTexture;
            newNormalMap = suitNormalMap;

            // Update textures in Kerbal IVA object since KSP resets them to these values a few frames later.
            var kerbalIva = component as Kerbal;
            if (kerbalIva != null) {
              kerbalIva.textureStandard = newTexture;
              kerbalIva.textureVeteran = newTexture;
            }
            break;

          case "neckRing":
            newTexture = suitTexture;
            newNormalMap = suitNormalMap;
            break;

          case "helmet":
          case "mesh_female_kerbalAstronaut01_helmet":
            newTexture = suitTexture;
            newNormalMap = suitNormalMap;
            break;

          case "visor":
          case "mesh_female_kerbalAstronaut01_visor":
            // Visor texture has to be replaced every time.
            newTexture = suit.GetVisor(useEvaSuit);
            if (newTexture != null) {
              smr.material.color = Color.white;
            }
            break;

          default: // Jetpack.
            if (isEva) {
              smr.enabled = useEvaSuit;

              if (useEvaSuit) {
                newTexture = suit.EvaJetpack;
                newNormalMap = suit.EvaJetpackNRM;
              }
            }
            break;
        }

        if (newTexture != null) {
          smr.material.mainTexture = newTexture;
        }
        if (newNormalMap != null) {
          smr.material.SetTexture(Util.BumpMapProperty, newNormalMap);
        }
      }
    }

    /// <summary>
    /// Personalise Kerbals in an internal space of a vessel. Used by IvaModule.
    /// </summary>
    public void PersonaliseIva(Kerbal kerbal)
    {
      PersonaliseKerbal(kerbal, kerbal.protoCrewMember, false, false);
    }

    /// <summary>
    /// Set external EVA/IVA suit.
    /// </summary>
    public void PersonaliseEva(Part part, ProtoCrewMember kerbal, bool useEvaSuit)
    {
      PersonaliseKerbal(part, kerbal, true, useEvaSuit);
    }

    /// <summary>
    /// Load per-game custom kerbals mapping.
    /// </summary>
    void LoadKerbalsMap(ConfigNode node)
    {
      node = node ?? customKerbalsNode;

      KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

      foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned)) {
        if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead &&
            kerbal.type != ProtoCrewMember.KerbalType.Unowned) {
          continue;
        }

        Appearance appearance = GetAppearance(kerbal);

        string value = node.GetValue(kerbal.name);
        if (value != null) {
          string[] tokens = Util.SplitConfigValue(value);
          string skinName = tokens.Length >= 1 ? tokens[0] : null;
          string suitName = tokens.Length >= 2 ? tokens[1] : null;

          if (skinName != null && skinName != "GENERIC") {
            appearance.Skin = skinName == "DEFAULT"
              ? DefaultSkin[(int)kerbal.gender]
              : Skins.Find(h => h.Name == skinName);
          }

          if (suitName != null && suitName != "GENERIC") {
            appearance.Suit = suitName == "DEFAULT" ? DefaultSuit
              : suitName == "VINTAGE" ? VintageSuit
              : Suits.Find(s => s.Name == suitName);
          }
        }
      }
    }

    /// <summary>
    /// Save per-game custom Kerbals mapping.
    /// </summary>
    void SaveKerbals(ConfigNode node)
    {
      KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

      foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned)) {
        if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead &&
            kerbal.type != ProtoCrewMember.KerbalType.Unowned) {
          continue;
        }

        Appearance appearance = GetAppearance(kerbal);

        string skinName = appearance.Skin == null ? "GENERIC" : appearance.Skin.Name;
        string suitName = appearance.Suit == null ? "GENERIC" : appearance.Suit.Name;

        node.AddValue(kerbal.name, skinName + " " + suitName);
      }
    }

    /// <summary>
    /// Load suit mapping.
    /// </summary>
    void LoadSuitMap(ConfigNode node, IDictionary<string, Suit> map, IDictionary<string, Suit> defaultMap)
    {
      if (node == null) {
        if (defaultMap != null) {
          foreach (var entry in defaultMap) {
            map[entry.Key] = entry.Value;
          }
        }
      } else {
        foreach (ConfigNode.Value entry in node.values) {
          map.Remove(entry.name);

          string suitName = entry.value;
          if (suitName != null && suitName != "GENERIC") {
            if (suitName == "DEFAULT") {
              map[entry.name] = DefaultSuit;
            } else if (suitName == "VINTAGE") {
              map[entry.name] = VintageSuit;
            } else {
              Suit suit = Suits.Find(s => s.Name == suitName);
              if (suit != null) {
                map[entry.name] = suit;
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Save suit mapping.
    /// </summary>
    static void SaveSuitMap(Dictionary<string, Suit> map, ConfigNode node)
    {
      foreach (var entry in map) {
        string suitName = entry.Value == null ? "GENERIC" : entry.Value.Name;

        node.AddValue(entry.Key, suitName);
      }
    }

    /// <summary>
    /// Fill config for custom Kerbal skins and suits.
    /// </summary>
    void ReadKerbalsConfigs()
    {
      var excludedSkins = new List<string>();
      var excludedSuits = new List<string>();
      var femaleSuits = new List<string>();
      var eyelessSkins = new List<string>();

      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer")) {
        ConfigNode customNode = file.config.GetNode("CustomKerbals");
        if (customNode != null) {
          // Merge into `customKerbalsNode`.
          foreach (ConfigNode.Value entry in customNode.values) {
            customKerbalsNode.RemoveValue(entry.name);
            customKerbalsNode.AddValue(entry.name, entry.value);
          }
        }

        ConfigNode genericNode = file.config.GetNode("GenericKerbals");
        if (genericNode != null) {
          Util.AddLists(genericNode.GetValues("excludedSkins"), excludedSkins);
          Util.AddLists(genericNode.GetValues("excludedSuits"), excludedSuits);
          Util.AddLists(genericNode.GetValues("femaleSuits"), femaleSuits);
          Util.AddLists(genericNode.GetValues("eyelessSkins"), eyelessSkins);
        }

        ConfigNode classNode = file.config.GetNode("ClassSuits");
        if (classNode != null) {
          LoadSuitMap(classNode, DefaultClassSuits, null);
        }
      }

      // Tag eye-less skins.
      foreach (Skin head in Skins) {
        head.IsEyeless = eyelessSkins.Contains(head.Name);
      }
      // Tag female suits.
      foreach (Suit suit in Suits) {
        suit.Gender = femaleSuits.Contains(suit.Name) ? Gender.Female : Gender.Male;
      }

      // Create lists of male skins and suits.
      kerbalSkins[0].AddRange(Skins.Where(h => h.Gender == Gender.Male && !excludedSkins.Contains(h.Name)));
      kerbalSuits[0].AddRange(Suits.Where(s => s.Gender == Gender.Male && !excludedSuits.Contains(s.Name)));

      // Create lists of female skins and suits. Use same suits as for males unless special female suits are set.
      kerbalSkins[1].AddRange(Skins.Where(h => h.Gender == Gender.Female && !excludedSkins.Contains(h.Name)));
      kerbalSuits[1].AddRange(Suits.Where(s => s.Gender == Gender.Female && !excludedSuits.Contains(s.Name)));
      kerbalSuits[1] = kerbalSuits[1].Count == 0 ? kerbalSuits[0] : kerbalSuits[1];

      // Trim lists.
      Skins.TrimExcess();
      Suits.TrimExcess();
      kerbalSkins[0].TrimExcess();
      kerbalSuits[0].TrimExcess();
      kerbalSkins[1].TrimExcess();
      kerbalSuits[1].TrimExcess();
    }

    public static void Recreate()
    {
      Instance = new Personaliser();
    }

    /// <summary>
    /// Read configuration and perform pre-load initialisation.
    /// </summary>
    public void ReadConfig(ConfigNode rootNode)
    {
      bool hideParachuteBackpack = false;
      Util.Parse(rootNode.GetValue("hideParachuteBackpack"), ref hideParachuteBackpack);
      HideParachuteBackpack = hideParachuteBackpack;
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
      var skinDirs = new Dictionary<string, int>();
      var suitDirs = new Dictionary<string, int>();

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture) {
        Texture2D texture = texInfo.texture;
        if (texture == null) {
          continue;
        }

        int skinsPrefixIndex = texture.name.IndexOf(SkinsPrefix, StringComparison.Ordinal);
        int suitsPrefixIndex = texture.name.IndexOf(SuitsPrefix, StringComparison.Ordinal);
        int defaultPrefixIndex = texture.name.IndexOf(Replacer.DefaultPrefix, StringComparison.Ordinal);

        if (skinsPrefixIndex != -1) {
          int prefixLength = skinsPrefixIndex + SkinsPrefix.Length;
          int skinNameLength = texture.name.LastIndexOf('/') - prefixLength;

          if (skinNameLength < 1) {
            log.Print("Skin texture is not inside a subdirectory: {0}", texture.name);
          } else { // Add a skin texture.
            string skinName = texture.name.Substring(prefixLength, skinNameLength);
            string originalName = texture.name.Substring(prefixLength + skinNameLength + 1);

            if (!skinDirs.TryGetValue(skinName, out int index)) {
              index = Skins.Count;
              Skins.Add(new Skin { Name = skinName });
              skinDirs.Add(skinName, index);
            }

            Skin skin = Skins[index];
            if (skin.SetTexture(originalName, texture)) {
              texture.wrapMode = TextureWrapMode.Clamp;
            } else {
              log.Print("Unknown skin texture name \"{0}\": {1}", originalName, texture.name);
            }
          }
        } else if (suitsPrefixIndex != -1) {
          int prefixLength = suitsPrefixIndex + SuitsPrefix.Length;
          int suitNameLength = texture.name.LastIndexOf('/') - prefixLength;

          if (suitNameLength < 1) {
            log.Print("Suit texture is not inside a subdirectory: {0}", texture.name);
          } else { // Add a suit texture.
            string suitName = texture.name.Substring(prefixLength, suitNameLength);
            string originalName = texture.name.Substring(prefixLength + suitNameLength + 1);

            if (!suitDirs.TryGetValue(suitName, out int index)) {
              index = Suits.Count;
              Suits.Add(new Suit { Name = suitName });
              suitDirs.Add(suitName, index);
            }

            Suit suit = Suits[index];
            if (suit.SetTexture(originalName, texture)) {
              texture.wrapMode = TextureWrapMode.Clamp;
            } else {
              log.Print("Unknown suit texture name \"{0}\": {1}", originalName, texture.name);
            }
          }
        } else if (defaultPrefixIndex != -1) {
          int prefixLength = defaultPrefixIndex + Replacer.DefaultPrefix.Length;
          string originalName = texture.name.Substring(prefixLength);

          switch (originalName) {
            case "eyeballLeft":
            case "eyeballRight":
            case "pupilLeft":
            case "pupilRight":
              DefaultSkin[0].SetTexture(originalName, texture);
              DefaultSkin[1].SetTexture(originalName, texture);
              texture.wrapMode = TextureWrapMode.Clamp;
              break;

            case "kerbalHead":
            case "kerbalHeadNRM":
              DefaultSkin[0].SetTexture(originalName, texture);
              texture.wrapMode = TextureWrapMode.Clamp;
              break;

            case "kerbalGirl_06_BaseColor":
            case "kerbalGirl_06_BaseColorNRM":
              DefaultSkin[1].SetTexture(originalName, texture);
              texture.wrapMode = TextureWrapMode.Clamp;
              break;

            default:
              if (DefaultSuit.SetTexture(originalName, texture)) {
                texture.wrapMode = TextureWrapMode.Clamp;
              }
              break;
          }
        }
      }

      // Visor needs to be replaced every time, not only on the proto-Kerbal model, so the visor from the default suit
      // must be set on all suits without a custom visor.
      VintageSuit.IvaVisor = VintageSuit.IvaVisor ?? DefaultSuit.IvaVisor;
      VintageSuit.EvaVisor = VintageSuit.EvaVisor ?? DefaultSuit.EvaVisor;

      foreach (var suit in Suits) {
        suit.IvaVisor = suit.IvaVisor ?? DefaultSuit.IvaVisor;
        suit.EvaVisor = suit.EvaVisor ?? DefaultSuit.EvaVisor;
      }

      ReadKerbalsConfigs();

      // Initialise default Kerbal, which is only loaded when the main menu shows.
      foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>()) {
        if (texture.name != null) {
          if (texture.name == "kerbalHead") {
            DefaultSkin[0].Head = DefaultSkin[0].Head ?? texture;
          } else if (texture.name == "kerbalGirl_06_BaseColor") {
            DefaultSkin[1].Head = DefaultSkin[1].Head ?? texture;
          } else {
            DefaultSuit.SetTexture(texture.name, texture);
          }
        }
      }

      // The previous loop filled the default suit, we still have to get the vintage one. Since IVA textures for vintage
      // suit are only instantiated when the Kerbals are created, we are in trouble here. Let's just use vintage EVA
      // suit for IVA too (both veteran and standard) and be happy with that :)
      Part vintageEva = PartLoader.getPartInfoByName("kerbalEVAVintage").partPrefab;

      foreach (SkinnedMeshRenderer smr in vintageEva.GetComponentsInChildren<SkinnedMeshRenderer>()) {
        if (smr.name == "body01") {
          var suitTexture = smr.material.mainTexture as Texture2D;

          VintageSuit.IvaSuitVeteran = suitTexture;
          VintageSuit.SetIvaSuit(suitTexture, 0, true);
          VintageSuit.SetEvaSuit(suitTexture, 0, true);
        }
      }

      foreach (Kerbal kerbal in Resources.FindObjectsOfTypeAll<Kerbal>()) {
        // After na IVA space is initialised, suits are reset to these values. Replace stock textures with default ones.
        kerbal.textureStandard = DefaultSuit.IvaSuit[0];
        kerbal.textureVeteran = DefaultSuit.IvaSuitVeteran;
      }

      // `TRIvaModelModule` makes sure that internal spaces personalise all Kerbals inside them on instantiation.
      // This will not suffice for Ship Manifest, we will also need to re-add these modules on any crew transfer.
      foreach (InternalModel model in Resources.FindObjectsOfTypeAll<InternalModel>()) {
        if (model.GetComponent<TRIvaModelModule>() == null) {
          model.gameObject.AddComponent<TRIvaModelModule>();
        }
      }

      Part[] evas = {
        PartLoader.getPartInfoByName("kerbalEVA").partPrefab,
        PartLoader.getPartInfoByName("kerbalEVAfemale").partPrefab,
        PartLoader.getPartInfoByName("kerbalEVAVintage").partPrefab,
        PartLoader.getPartInfoByName("kerbalEVAfemaleVintage").partPrefab
      };

      foreach (Part eva in evas) {
        if (eva.GetComponent<TREvaModule>() == null) {
          eva.gameObject.AddComponent<TREvaModule>();
        }
      }

      // Re-read scenario if database is reloaded during the space centre scene to avoid losing all per-game settings.
      if (HighLogic.CurrentGame != null) {
        ConfigNode scenarioNode = HighLogic.CurrentGame.config.GetNodes("SCENARIO")
          .FirstOrDefault(n => n.GetValue("name") == "TRScenario");

        if (scenarioNode != null) {
          OnLoadScenario(scenarioNode);
        }
      }
    }

    void OnHelmetChanged(KerbalEVA eva, bool hasHelmet, bool hasNeckRing)
    {
      var evaModule = eva.GetComponent<TREvaModule>();
      if (evaModule != null) {
        evaModule.OnHelmetChanged(hasHelmet);
      }
    }

    public void OnBeginFlight()
    {
      GameEvents.OnHelmetChanged.Add(OnHelmetChanged);
    }

    public void OnEndFlight()
    {
      GameEvents.OnHelmetChanged.Remove(OnHelmetChanged);
    }

    public void OnLoadScenario(ConfigNode node)
    {
      gameKerbals.Clear();
      ClassSuits.Clear();

      bool hideParachuteBackpack = HideParachuteBackpack;
      Util.Parse(node.GetValue("hideParachuteBackpack"), ref hideParachuteBackpack);
      HideParachuteBackpack = hideParachuteBackpack;

      LoadKerbalsMap(node.GetNode("Kerbals"));
      LoadSuitMap(node.GetNode("ClassSuits"), ClassSuits, DefaultClassSuits);
    }

    public void OnSaveScenario(ConfigNode node)
    {
      node.AddValue("hideParachuteBackpack", HideParachuteBackpack);
      SaveKerbals(node.AddNode("Kerbals"));
      SaveSuitMap(ClassSuits, node.AddNode("ClassSuits"));
    }

    public void ResetKerbals()
    {
      gameKerbals.Clear();
      ClassSuits.Clear();

      LoadKerbalsMap(null);
      LoadSuitMap(null, ClassSuits, DefaultClassSuits);
    }
  }
}
