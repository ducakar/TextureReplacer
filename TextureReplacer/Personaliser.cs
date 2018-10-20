/*
 * Copyright © 2013-2018 Davorin Učakar
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

namespace TextureReplacer
{
  class Personaliser
  {
    const string DefaultDirectory = Util.Directory + "Default/";
    const string SkinsDirectory = Util.Directory + "Skins/";
    const string SuitsDirectory = Util.Directory + "Suits/";

    static readonly Log log = new Log(nameof(Personaliser));

    // Male/female textures (minus excluded).
    readonly List<Skin>[] kerbalSkins = { new List<Skin>(), new List<Skin>() };
    readonly List<Suit>[] kerbalSuits = { new List<Suit>(), new List<Suit>() };
    // Personalised Kerbal textures.
    readonly Dictionary<string, Appearance> gameKerbals = new Dictionary<string, Appearance>();
    // Backed-up personalised textures from main configuration files. These are used to initialise kerbals if a saved
    // game doesn't contain `TRScenario`.
    ConfigNode customKerbalsNode = new ConfigNode();
    // Helmet removal.
    readonly Mesh[] helmetMesh = { null, null };
    readonly Mesh[] visorMesh = { null, null };
    bool isHelmetRemovalEnabled = true;
    // Atmospheric IVA suit parameters.
    bool isAtmSuitEnabled = true;
    double atmSuitPressure = 50.0;
    readonly HashSet<string> atmSuitBodies = new HashSet<string>();

    // Instance.
    public static Personaliser Instance { get; private set; }

    // Default textures (from `Default/`).
    public Skin[] DefaultSkin = { new Skin { Name = "DEFAULT" }, new Skin { Name = "DEFAULT" } };
    public Suit DefaultSuit = new Suit { Name = "DEFAULT" };
    public Suit VintageSuit = new Suit { Name = "VINTAGE" };

    // All Kerbal textures, including excluded by configuration.
    public List<Skin> Skins = new List<Skin>();
    public List<Suit> Suits = new List<Suit>();

    // Class-specific suits.
    public Dictionary<string, Suit> ClassSuits = new Dictionary<string, Suit>();
    public Dictionary<string, Suit> DefaultClassSuits = new Dictionary<string, Suit>();

    public bool IsHelmetRemovalEnabled {
      get { return isHelmetRemovalEnabled; }
      set { isHelmetRemovalEnabled = value; }
    }

    public bool IsAtmSuitEnabled {
      get { return isAtmSuitEnabled; }
      set { isAtmSuitEnabled = value; }
    }

    /// <summary>
    /// Whether a vessel is in a "safe" situation, so Kerbals don't need helmets (i.e. landed/splashed or in orbit).
    /// </summary>
    static bool IsSituationSafe(Vessel vessel)
    {
      return vessel.situation != Vessel.Situations.FLYING && vessel.situation != Vessel.Situations.SUB_ORBITAL;
    }

    /// <summary>
    /// Whether the atmosphere is breathable.
    /// </summary>
    public bool IsAtmBreathable()
    {
      return !HighLogic.LoadedSceneIsFlight ||
             (FlightGlobals.getStaticPressure() >= atmSuitPressure &&
             atmSuitBodies.Contains(FlightGlobals.currentMainBody.bodyName));
    }

    Suit GetClassSuit(ProtoCrewMember kerbal)
    {
      ClassSuits.TryGetValue(kerbal.experienceTrait.Config.Name, out Suit suit);
      return suit;
    }

    /// <summary>
    /// Get appearance of a Kerbal. If the skin or the suit is generic it is set to null. If an appearance does not
    /// yet exist for that Kerbal it is created (and gender is fixed according to forceLegacyFemales).
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
        return DefaultSuit;
      }

      // We must use a different prime here to increase randomisation so that the same skin is not always combined with
      // the same suit.
      int number = (appearance.Hash * 2053) & 0x7fffffff;
      return genderSuits[number % genderSuits.Count];
    }

    /// <summary>
    /// Replace textures on a Kerbal model.
    /// </summary>
    void PersonaliseKerbal(Component component, ProtoCrewMember kerbal, Part pod, bool needsSuit)
    {
      Appearance appearance = GetAppearance(kerbal);
      bool isEva = pod == null;
      bool isVintage = kerbal.suit == ProtoCrewMember.KerbalSuit.Vintage;

      Skin skin = GetKerbalSkin(kerbal, appearance);
      Suit suit = GetKerbalSuit(kerbal, appearance);

      Transform model = isEva || !isVintage ? component.transform.Find("model01")
        : component.transform.Find("kbIVA@idle/model01");
      Transform flag = isEva ? component.transform.Find("model/kbEVA_flagDecals") : null;
      Transform parachute = isEva ? component.transform.Find("model/EVAparachute/base") : null;

      if (isEva) {
        flag.GetComponent<Renderer>().enabled = needsSuit;
        parachute.GetComponent<Renderer>().enabled = needsSuit;
      }

      // We must include hidden meshes, since flares are hidden when light is turned off.
      // All other meshes are always visible, so no performance hit here.
      foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) {
        var smr = renderer as SkinnedMeshRenderer;

        // Parachute backpack, flag decals, headlight flares and thruster jets.
        if (smr == null) {
          renderer.enabled = needsSuit;
        } else {
          Material material = renderer.material;
          Shader newShader = null;
          Texture2D newTexture = null;
          Texture2D newNormalMap = null;

          switch (smr.name) {
            case "eyeballLeft":
            case "eyeballRight":
            case "pupilLeft":
            case "pupilRight":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
              if (skin.IsEyeless) {
                smr.sharedMesh = null;
              }
              break;

            case "headMesh01":
            case "headMesh02":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
            case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
              newTexture = skin.Head;
              newNormalMap = skin.HeadNRM;

              if (newNormalMap != null) {
                newShader = Replacer.BumpedHeadShader;
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
              bool isEvaSuit = isEva && needsSuit;

              newTexture = isEvaSuit ? suit.GetEvaBody(kerbal) : suit.GetIvaBody(kerbal);
              newNormalMap = isEvaSuit ? suit.EvaBodyNRM : suit.IvaBodyNRM;

              if (newTexture == null) {
                // Setting the suit explicitly is necessary for two reasons: to fix IVA suits after KSP resetting them
                // to the stock ones all the time and to fix the switch from non-default to default texture during EVA
                // suit toggle.
                Suit defaultSuit = isVintage ? VintageSuit : DefaultSuit;

                newTexture = isEvaSuit ? defaultSuit.EvaBody
                  : kerbal.veteran ? defaultSuit.IvaBodyVeteran
                  : defaultSuit.IvaBody;
              }

              if (newNormalMap == null) {
                Suit defaultSuit = isVintage ? VintageSuit : DefaultSuit;

                newNormalMap = isEvaSuit ? defaultSuit.EvaBodyNRM : defaultSuit.IvaBodyNRM;
              }

              // Update textures in Kerbal IVA object since KSP resets them to these values a few frames later.
              if (!isEva) {
                var kerbalIva = (Kerbal)component;

                kerbalIva.textureStandard = newTexture;
                kerbalIva.textureVeteran = newTexture;
              }
              break;

            case "helmet":
            case "mesh_female_kerbalAstronaut01_helmet":
              if (isEva) {
                smr.enabled = needsSuit;
              } else {
                smr.sharedMesh = needsSuit ? helmetMesh[(int)kerbal.gender] : null;
              }
              break;

            case "visor":
            case "mesh_female_kerbalAstronaut01_visor":
              if (isEva) {
                smr.enabled = needsSuit;
              } else {
                smr.sharedMesh = needsSuit ? visorMesh[(int)kerbal.gender] : null;
              }

              // Textures have to be replaced even when hidden since it may become visible later on situation change.
              newTexture = isEva ? suit.EvaVisor : suit.IvaVisor;

              if (newTexture != null) {
                material.color = Color.white;
              }
              break;

            default: // Jetpack.
              if (isEva) {
                smr.enabled = needsSuit;

                if (needsSuit) {
                  newTexture = suit.EvaJetpack;
                  newNormalMap = suit.EvaJetpackNRM;
                }
              }
              break;
          }

          if (newShader != null) {
            material.shader = newShader;
          }
          if (newTexture != null) {
            material.mainTexture = newTexture;
          }
          if (newNormalMap != null) {
            material.SetTexture(Util.BumpMapProperty, newNormalMap);
          }
        }
      }
    }

    /// <summary>
    /// Personalise Kerbals in an internal space of a vessel. Used by IvaModule.
    /// </summary>
    public void PersonaliseIva(Kerbal kerbal)
    {
      bool needsSuit = !isHelmetRemovalEnabled || !IsSituationSafe(kerbal.InVessel);

      PersonaliseKerbal(kerbal, kerbal.protoCrewMember, kerbal.InPart, needsSuit);
    }

    /// <summary>
    /// Set external EVA/IVA suit. Fails and returns false iff trying to remove an EVA suit outside of breathable
    /// atmosphere.This function is used by EvaModule.
    /// </summary>
    public bool PersonaliseEva(Part evaPart, bool useEvaSuit)
    {
      bool isDesiredSuitValid = true;

      if (evaPart.protoModuleCrew.Count != 0) {
        if (!useEvaSuit && !IsAtmBreathable()) {
          useEvaSuit = true;
          isDesiredSuitValid = false;
        }
        PersonaliseKerbal(evaPart, evaPart.protoModuleCrew[0], null, useEvaSuit);
      }
      return isDesiredSuitValid;
    }

    void UpdateIvaHelmets(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> action)
    {
      Vessel vessel = action.host;

      if (!isHelmetRemovalEnabled || vessel == null) {
        return;
      }

      foreach (Part part in vessel.parts.Where(p => p.internalModel != null)) {
        Kerbal[] kerbals = part.internalModel.GetComponentsInChildren<Kerbal>();

        if (kerbals.Length != 0) {
          bool hideHelmets = IsSituationSafe(vessel);

          foreach (Kerbal kerbal in kerbals.Where(k => k.showHelmet)) {
            // `Kerbal.ShowHelmet(false)` irreversibly removes a helmet while
            // `Kerbal.ShowHelmet(true)` has no effect at all. We need the following workaround.
            foreach (SkinnedMeshRenderer smr in kerbal.helmetTransform.GetComponentsInChildren<SkinnedMeshRenderer>()) {
              if (smr.name.EndsWith("helmet", StringComparison.Ordinal)) {
                smr.sharedMesh = hideHelmets ? null : helmetMesh[(int)kerbal.protoCrewMember.gender];
              } else if (smr.name.EndsWith("visor", StringComparison.Ordinal)) {
                smr.sharedMesh = hideHelmets ? null : visorMesh[(int)kerbal.protoCrewMember.gender];
              }
            }
          }
        }
      }
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
      Util.Parse(rootNode.GetValue("isHelmetRemovalEnabled"), ref isHelmetRemovalEnabled);
      Util.Parse(rootNode.GetValue("isAtmSuitEnabled"), ref isAtmSuitEnabled);
      Util.Parse(rootNode.GetValue("atmSuitPressure"), ref atmSuitPressure);
      Util.AddLists(rootNode.GetValues("atmSuitBodies"), atmSuitBodies);
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
        if (texture == null || !texture.name.StartsWith(Util.Directory, StringComparison.Ordinal)) {
          continue;
        }

        // Add a skin texture.
        if (texture.name.StartsWith(SkinsDirectory, StringComparison.Ordinal)) {
          texture.wrapMode = TextureWrapMode.Clamp;

          int lastSlash = texture.name.LastIndexOf('/');
          int dirNameLength = lastSlash - SkinsDirectory.Length;
          string originalName = texture.name.Substring(lastSlash + 1);

          if (dirNameLength < 1) {
            log.Print("Skin texture should be inside a subdirectory: {0}", texture.name);
          } else {
            string dirName = texture.name.Substring(SkinsDirectory.Length, dirNameLength);

            if (!skinDirs.TryGetValue(dirName, out int index)) {
              index = Skins.Count;
              Skins.Add(new Skin { Name = dirName });
              skinDirs.Add(dirName, index);
            }

            Skin skin = Skins[index];
            if (!skin.SetTexture(originalName, texture)) {
              log.Print("Unknown skin texture name \"{0}\": {1}", originalName, texture.name);
            }
          }
        }
        // Add a suit texture.
        else if (texture.name.StartsWith(SuitsDirectory, StringComparison.Ordinal)) {
          texture.wrapMode = TextureWrapMode.Clamp;

          int lastSlash = texture.name.LastIndexOf('/');
          int dirNameLength = lastSlash - SuitsDirectory.Length;
          string originalName = texture.name.Substring(lastSlash + 1);

          if (dirNameLength < 1) {
            log.Print("Suit texture should be inside a subdirectory: {0}", texture.name);
          } else {
            string dirName = texture.name.Substring(SuitsDirectory.Length, dirNameLength);

            if (!suitDirs.TryGetValue(dirName, out int index)) {
              index = Suits.Count;
              Suits.Add(new Suit { Name = dirName });
              suitDirs.Add(dirName, index);
            }

            Suit suit = Suits[index];
            if (!suit.SetTexture(originalName, texture)) {
              log.Print("Unknown suit texture name \"{0}\": {1}", originalName, texture.name);
            }
          }
        } else if (texture.name.StartsWith(DefaultDirectory, StringComparison.Ordinal)) {
          int lastSlash = texture.name.LastIndexOf('/');
          string originalName = texture.name.Substring(lastSlash + 1);

          if (originalName == "kerbalHead") {
            DefaultSkin[0].SetTexture(originalName, texture);
            texture.wrapMode = TextureWrapMode.Clamp;
          } else if (originalName == "kerbalHeadNRM") {
            DefaultSkin[0].SetTexture(originalName, texture);
            texture.wrapMode = TextureWrapMode.Clamp;
          } else if (originalName == "kerbalGirl_06_BaseColor") {
            DefaultSkin[1].SetTexture(originalName, texture);
            texture.wrapMode = TextureWrapMode.Clamp;
          } else if (originalName == "kerbalGirl_06_BaseColorNRM") {
            DefaultSkin[1].SetTexture(originalName, texture);
            texture.wrapMode = TextureWrapMode.Clamp;
          } else if (DefaultSuit.SetTexture(originalName, texture)) {
            texture.wrapMode = TextureWrapMode.Clamp;
          }
        }
      }

      // Visor needs to be replaced every time, not only on the proto-Kerbal model, so the visor from the default suit
      // must be set on all suits without a custom visor.
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
      // suit for IVA too and be happy with that :)
      Part vintageEva = PartLoader.getPartInfoByName("kerbalEVAVintage").partPrefab;

      foreach (SkinnedMeshRenderer smr in vintageEva.GetComponentsInChildren<SkinnedMeshRenderer>()) {
        if (smr.name == "body01") {
          VintageSuit.IvaBody = smr.material.mainTexture as Texture2D;
          VintageSuit.IvaBodyVeteran = smr.material.mainTexture as Texture2D;
          VintageSuit.EvaBody = smr.material.mainTexture as Texture2D;
        }
      }

      foreach (Kerbal kerbal in Resources.FindObjectsOfTypeAll<Kerbal>()) {
        int genderIndex = (int)kerbal.protoCrewMember.gender;

        // Save pointer to helmet & visor meshes so helmet removal can restore them.
        foreach (SkinnedMeshRenderer smr in kerbal.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
          if (smr.name.EndsWith("helmet", StringComparison.Ordinal)) {
            helmetMesh[genderIndex] = smr.sharedMesh;
          } else if (smr.name.EndsWith("visor", StringComparison.Ordinal)) {
            visorMesh[genderIndex] = smr.sharedMesh;
          }
        }

        // After na IVA space is initialised, suits are reset to these values. Replace stock textures with default ones.
        kerbal.textureStandard = DefaultSuit.IvaBody;
        kerbal.textureVeteran = DefaultSuit.IvaBodyVeteran;
      }

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

    public void OnBeginFlight()
    {
      GameEvents.onVesselSituationChange.Add(UpdateIvaHelmets);
    }

    public void OnEndFlight()
    {
      GameEvents.onVesselSituationChange.Remove(UpdateIvaHelmets);
    }

    public void OnLoadScenario(ConfigNode node)
    {
      gameKerbals.Clear();
      ClassSuits.Clear();

      LoadKerbalsMap(node.GetNode("Kerbals"));
      LoadSuitMap(node.GetNode("ClassSuits"), ClassSuits, DefaultClassSuits);

      Util.Parse(node.GetValue("isHelmetRemovalEnabled"), ref isHelmetRemovalEnabled);
      Util.Parse(node.GetValue("isAtmSuitEnabled"), ref isAtmSuitEnabled);
    }

    public void OnSaveScenario(ConfigNode node)
    {
      SaveKerbals(node.AddNode("Kerbals"));
      SaveSuitMap(ClassSuits, node.AddNode("ClassSuits"));

      node.AddValue("isHelmetRemovalEnabled", isHelmetRemovalEnabled);
      node.AddValue("isAtmSuitEnabled", isAtmSuitEnabled);
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
