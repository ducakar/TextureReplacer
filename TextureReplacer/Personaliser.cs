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
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TextureReplacer
{
  class Personaliser
  {
    public enum SuitAssignment
    {
      RANDOM,
      CONSECUTIVE,
      EXPERIENCE
    }

    public class KerbalData
    {
      public bool isFemale;
      public bool isVeteran;
      public Head head;
      public Suit suit;
      public Suit cabinSuit;
    }

    public class Head
    {
      public string name;
      public bool isFemale;
      public bool isEyeless;
      public Texture2D head;
      public Texture2D headNRM;
    }

    public class Suit
    {
      public string name;
      public bool isFemale;
      public Texture2D suitVeteran;
      public Texture2D suit;
      public Texture2D suitNRM;
      public Texture2D helmet;
      public Texture2D helmetNRM;
      public Texture2D visor;
      public Texture2D evaSuit;
      public Texture2D evaSuitNRM;
      public Texture2D evaHelmet;
      public Texture2D evaVisor;
      public Texture2D evaJetpack;
      public Texture2D evaJetpackNRM;

      public bool setTexture(string originalName, Texture2D texture)
      {
        switch (originalName)
        {
          case "kerbalMain":
            suitVeteran = suitVeteran ?? texture;
            return false;
          case "kerbalMainGrey":
            suit = suit ?? texture;
            return true;
          case "kerbalMainNRM":
            suitNRM = suitNRM ?? texture;
            return true;
          case "kerbalHelmetGrey":
            helmet = helmet ?? texture;
            return true;
          case "kerbalHelmetNRM":
            helmetNRM = helmetNRM ?? texture;
            return true;
          case "kerbalVisor":
            visor = visor ?? texture;
            return true;
          case "EVAtexture":
            evaSuit = evaSuit ?? texture;
            return true;
          case "EVAtextureNRM":
            evaSuitNRM = evaSuitNRM ?? texture;
            return true;
          case "EVAhelmet":
            evaHelmet = evaHelmet ?? texture;
            return true;
          case "EVAvisor":
            evaVisor = evaVisor ?? texture;
            return true;
          case "EVAjetpack":
            evaJetpack = evaJetpack ?? texture;
            return true;
          case "EVAjetpackNRM":
            evaJetpackNRM = evaJetpackNRM ?? texture;
            return true;
          default:
            return false;
        }
      }
    }

    static readonly string DIR_DEFAULT = Util.DIR + "Default/";
    static readonly string DIR_HEADS = Util.DIR + "Heads/";
    static readonly string DIR_SUITS = Util.DIR + "Suits/";
    // Default textures (from `Default/`).
    public readonly Head defaultHead = new Head { name = "DEFAULT" };
    public readonly Suit defaultSuit = new Suit { name = "DEFAULT" };
    // All Kerbal textures, including excluded by configuration.
    public readonly List<Head> heads = new List<Head>();
    public readonly List<Suit> suits = new List<Suit>();
    // Male textures (minus excluded).
    readonly List<Head> kerbalHeads = new List<Head>();
    readonly List<Suit> kerbalSuits = new List<Suit>();
    // Female textures (minus excluded).
    readonly List<Head> kerminHeads = new List<Head>();
    readonly List<Suit> kerminSuits = new List<Suit>();
    // Female name REs.
    readonly List<Regex> kerminNames = new List<Regex>();
    // Personalised Kerbal textures.
    readonly Dictionary<string, KerbalData> gameKerbals = new Dictionary<string, KerbalData>();
    // Backed-up personalised textures from main configuration files. These are used to initialise
    // kerbals if a saved game doesn't contain `TRScenario`.
    readonly Dictionary<string, KerbalData> customKerbals = new Dictionary<string, KerbalData>();
    // Cabin-specific suits.
    readonly Dictionary<string, Suit> cabinSuits = new Dictionary<string, Suit>();
    // Perk-specific suits.
    public readonly Dictionary<string, Suit> perkSuits = new Dictionary<string, Suit>();
    public readonly Dictionary<string, Suit> defaultPerkSuits = new Dictionary<string, Suit>();
    // Helmet removal.
    Mesh helmetMesh = null;
    Mesh visorMesh = null;
    public bool isHelmetRemovalEnabled = true;
    // Atmospheric IVA suit parameters.
    public bool isAtmSuitEnabled = true;
    double atmSuitPressure = 0.5;
    readonly HashSet<string> atmSuitBodies = new HashSet<string>();
    // Whether assignment of suits should be consecutive.
    public SuitAssignment suitAssignment = SuitAssignment.RANDOM;
    // For transparent pods, previous vessel IVA has to be updated too on vessel switching since
    // textures are reset to stock on switch.
    Vessel previousVessel = null;
    // List of vessels where IVA textures have to be updated. In stock game it suffices to only
    // perform this on the current vessel but not so if one uses transparent pods (JSITransparentPod
    // or sfr modules).
    readonly List<Vessel> ivaVessels = new List<Vessel>();
    // Instance.
    public static Personaliser instance = null;

    static bool isRegularVessel(Vessel vessel)
    {
      bool value = vessel != null && vessel.parts.Count != 0
                   && vessel.rootPart.GetComponent<KerbalEVA>() == null;
      return value;
    }

    static bool isSituationSafe(Vessel vessel)
    {
      bool value = vessel.situation != Vessel.Situations.FLYING
                   && vessel.situation != Vessel.Situations.SUB_ORBITAL;
      return value;
    }

    public bool isAtmBreathable()
    {
      bool value = FlightGlobals.getStaticPressure() >= atmSuitPressure
                   && atmSuitBodies.Contains(FlightGlobals.currentMainBody.bodyName);
      return value;
    }

    public KerbalData getKerbalData(string name)
    {
      KerbalData kerbalData;

      if (!gameKerbals.TryGetValue(name, out kerbalData))
      {
        int spaceIndex = name.IndexOf(' ');
        string firstName = spaceIndex > 0 ? name.Substring(0, spaceIndex) : name;
        bool isFemale = kerminNames.Any(r => r.IsMatch(firstName));
        bool isVeteran = name == "Jebediah Kerman" || name == "Bill Kerman" || name == "Bob Kerman";

        kerbalData = new KerbalData { isFemale = isFemale, isVeteran = isVeteran };
        gameKerbals.Add(name, kerbalData);
      }
      return kerbalData;
    }

    Suit getPerkSuit(ProtoCrewMember kerbal)
    {
      Suit suit = null;

      if (suitAssignment == SuitAssignment.EXPERIENCE)
        perkSuits.TryGetValue(kerbal.experienceTrait.TypeName, out suit);

      return suit;
    }

    /**
     * Replace textures on a Kerbal model.
     */
    void personaliseKerbal(Component component, ProtoCrewMember kerbal, Part cabin, bool needsSuit)
    {
      KerbalData kerbalData = getKerbalData(kerbal.name);
      bool isEva = cabin == null;
      bool isFemale = kerbalData.isFemale;
      Head head = kerbalData.head;
      Suit suit = kerbalData.suit;

      if (head == null)
      {
        List<Head> genderHeads = isFemale && kerminHeads.Count != 0 ? kerminHeads : kerbalHeads;

        if (genderHeads.Count != 0)
        {
          // Hash is multiplied with a large prime to increase randomisation, since hashes returned
          // by `GetHashCode()` are close together if strings only differ in the last (few) char(s).
          int number = (kerbal.name.GetHashCode() * 4099) & 0x7fffffff;
          head = genderHeads[number % genderHeads.Count];
        }
      }

      if (isEva || !cabinSuits.TryGetValue(cabin.partInfo.name, out kerbalData.cabinSuit))
      {
        suit = kerbalData.suit ?? getPerkSuit(kerbal);

        if (suit == null)
        {
          List<Suit> genderSuits = isFemale && kerminSuits.Count != 0 ? kerminSuits : kerbalSuits;

          if (genderSuits.Count != 0)
          {
            // Here we must use a different prime to increase randomisation so that the same head is
            // not always combined with the same suit.
            int number =
              suitAssignment == SuitAssignment.RANDOM ?
              ((kerbal.name.GetHashCode() + kerbal.name.Length) * 2053) & 0x7fffffff :
              HighLogic.CurrentGame.CrewRoster.IndexOf(kerbal);

            suit = genderSuits[number % genderSuits.Count];
          }
        }
      }

      head = head == defaultHead ? null : head;
      suit = (isEva && needsSuit) || kerbalData.cabinSuit == null ? suit : kerbalData.cabinSuit;
      suit = suit == defaultSuit ? null : suit;

      // We must include hidden meshes, since flares are hidden when light is turned off.
      // All other meshes are always visible, so no performance hit here.
      foreach (Renderer renderer in component.GetComponentsInChildren<Renderer>(true))
      {
        var smr = renderer as SkinnedMeshRenderer;

        // Thruster jets, flag decals and headlight flares.
        if (smr == null)
        {
          if (renderer.name != "screenMessage")
            renderer.enabled = needsSuit;
        }
        else
        {
          Material material = renderer.material;
          Texture2D newTexture = null;
          Texture2D newNormalMap = null;

          switch (smr.name)
          {
            case "eyeballLeft":
            case "eyeballRight":
            case "pupilLeft":
            case "pupilRight":
              if (head != null && head.isEyeless)
                smr.sharedMesh = null;

              break;

            case "headMesh01":
            case "upTeeth01":
            case "upTeeth02":
            case "tongue":
              if (head != null)
              {
                newTexture = head.head;
                newNormalMap = head.headNRM;

                smr.material.shader = newNormalMap != null ? Util.BUMPED_DIFFUSE_SHADER :
                                                             Util.DIFFUSE_SHADER;
              }
              break;

            case "body01":
              bool isEvaSuit = isEva && needsSuit;

              if (suit != null)
              {
                newTexture = isEvaSuit ? suit.evaSuit : suit.suit;
                newNormalMap = isEvaSuit ? suit.evaSuitNRM : suit.suitNRM;
              }

              // This required for two reasons: to fix IVA suits after KSP resetting them to the
              // stock ones all the time and to fix the switch from non-default to default texture
              // during EVA suit toggle.
              if (newTexture == null)
                newTexture = isEvaSuit ? defaultSuit.evaSuit :
                             kerbalData.isVeteran ? defaultSuit.suitVeteran : defaultSuit.suit;

              if (newNormalMap == null)
                newNormalMap = isEvaSuit ? defaultSuit.evaSuitNRM : defaultSuit.suitNRM;

              // Update textures in Kerbal IVA object since KSP resets them to these values a few
              // frames after portraits begin to render.
              if (!isEva)
              {
                Kerbal kerbalIVA = (Kerbal) component;

                kerbalIVA.textureStandard = newTexture;
                kerbalIVA.textureVeteran = newTexture;
              }
              break;

            case "helmet":
              if (isEva)
                smr.enabled = needsSuit;
              else
                smr.sharedMesh = needsSuit ? helmetMesh : null;

              if (needsSuit && suit != null)
              {
                newTexture = isEva ? suit.evaHelmet : suit.helmet;
                newNormalMap = suit.helmetNRM;
              }
              break;

            case "visor":
              if (isEva)
                smr.enabled = needsSuit;
              else
                smr.sharedMesh = needsSuit ? visorMesh : null;

              if (needsSuit && suit != null)
              {
                newTexture = isEva ? suit.evaVisor : suit.visor;

                if (newTexture != null)
                  material.color = Color.white;
              }
              break;

            default: // Jetpack.
              smr.enabled = needsSuit;

              if (needsSuit && suit != null)
              {
                newTexture = suit.evaJetpack;
                newNormalMap = suit.evaJetpackNRM;
              }
              break;
          }

          if (newTexture != null)
            material.mainTexture = newTexture;

          if (newNormalMap != null)
            material.SetTexture(Util.BUMPMAP_PROPERTY, newNormalMap);
        }
      }
    }

    /**
     * Personalise Kerbals in internal spaces.
     */
    void personaliseIVAs()
    {
      // IVA textures must be replaced with a little lag, otherwise we risk race conditions with KSP
      // handler that resets IVA suits to the stock ones. The race condition issue always occurs
      // when boarding an external seat.
      foreach (Vessel vessel in ivaVessels)
      {
        if (vessel != null && vessel.loaded && vessel.vesselName != null)
        {
          foreach (Part part in vessel.parts)
          {
            if (part.internalModel != null)
            {
              Kerbal[] kerbals = part.internalModel.GetComponentsInChildren<Kerbal>();
              if (kerbals.Length != 0)
              {
                bool needsSuit = !isHelmetRemovalEnabled || !isSituationSafe(vessel);

                foreach (Kerbal kerbal in kerbals)
                  personaliseKerbal(kerbal, kerbal.protoCrewMember, kerbal.InPart, needsSuit);
              }
            }
          }
        }
      }

      ivaVessels.Clear();
      // Prevent capacity from growing too much.
      if (ivaVessels.Capacity > 16)
        ivaVessels.TrimExcess();
    }

    /**
     * Update IVA textures on vessel switch or docking. For transparent pods to work correctly,
     * this also has to be performed for the old vessel.
     */
    void scheduleSwitchUpdate(Vessel vessel)
    {
      if (previousVessel != vessel && isRegularVessel(previousVessel))
        ivaVessels.AddUnique(previousVessel);

      if (isRegularVessel(vessel))
        ivaVessels.AddUnique(vessel);

      previousVessel = vessel;
    }

    /**
     * Update IVA textures on crew transfer.
     */
    void scheduleTransferUpdate(GameEvents.HostedFromToAction<ProtoCrewMember, Part> action)
    {
      scheduleSwitchUpdate(action.to.vessel);
    }

    /**
     * Update IVA textures when a new vessel is created or when it comes into 2.3 km range (for
     * transparent pods).
     */
    void scheduleSpawnUpdate(Vessel vessel)
    {
      if (isRegularVessel(vessel))
        ivaVessels.AddUnique(vessel);
    }

    /**
     * Enable/disable helmets in the current IVA space depending on situation.
     */
    void updateHelmets(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> action)
    {
      Vessel vessel = action.host;
      if (!isHelmetRemovalEnabled || vessel == null)
        return;

      foreach (Part part in vessel.parts)
      {
        if (part.internalModel != null)
        {
          Kerbal[] kerbals = part.internalModel.GetComponentsInChildren<Kerbal>();
          if (kerbals.Length != 0)
          {
            bool hideHelmets = isSituationSafe(vessel);

            foreach (Kerbal kerbal in kerbals)
            {
              if (kerbal.showHelmet)
              {
                // `Kerbal.ShowHelmet(false)` irreversibly removes a helmet while
                // `Kerbal.ShowHelmet(true)` has no effect at all. We need the following workaround.
                foreach (SkinnedMeshRenderer smr
                         in kerbal.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                  if (smr.name == "helmet")
                    smr.sharedMesh = hideHelmets ? null : helmetMesh;
                  else if (smr.name == "visor")
                    smr.sharedMesh = hideHelmets ? null : visorMesh;
                }
              }
            }
          }
        }
      }
    }

    /**
     * Load per-game custom kerbals mapping.
     */
    void loadKerbals(ConfigNode node)
    {
      if (node == null)
      {
        foreach (var entry in customKerbals)
          gameKerbals.Add(entry.Key, entry.Value);
      }
      else
      {
        foreach (ConfigNode.Value entry in node.values)
        {
          string[] tokens = Util.splitConfigValue(entry.value);
          string name = entry.name;
          string headName = tokens.Length >= 1 ? tokens[0] : null;
          string suitName = tokens.Length >= 2 ? tokens[1] : null;

          KerbalData kerbalData = getKerbalData(name);

          if (headName != null && headName != "GENERIC")
          {
            kerbalData.head = headName == "DEFAULT" ? defaultHead :
                              heads.FirstOrDefault(h => h.name == headName);
          }

          if (suitName != null && suitName != "GENERIC")
          {
            kerbalData.suit = suitName == "DEFAULT" ? defaultSuit :
                              suits.FirstOrDefault(s => s.name == suitName);
          }
        }
      }
    }

    /**
     * Save per-game custom Kerbals mapping.
     */
    void saveKerbals(ConfigNode node)
    {
      foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
      {
        if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
          continue;

        KerbalData kerbalData = getKerbalData(kerbal.name);

        string headName = kerbalData.head == null ? "GENERIC" : kerbalData.head.name;
        string suitName = kerbalData.suit == null ? "GENERIC" : kerbalData.suit.name;

        node.AddValue(kerbal.name, headName + " " + suitName);
      }
    }

    /**
     * Load suit mapping.
     */
    void loadSuitMap(ConfigNode node, IDictionary<string, Suit> map,
                     IDictionary<string, Suit> defaultMap = null)
    {
      if (node == null)
      {
        if (defaultSuit != null)
        {
          foreach (var entry in defaultMap)
            map.Add(entry.Key, entry.Value);
        }
      }
      else
      {
        foreach (ConfigNode.Value entry in node.values)
        {
          string suitName = entry.value;

          map.Remove(entry.name);

          if (suitName != null && suitName != "GENERIC")
          {
            map[entry.name] = suitName == "DEFAULT" ? defaultSuit :
                              suits.FirstOrDefault(s => s.name == suitName);
          }
        }
      }
    }

    /**
     * Save suit mapping.
     */
    static void saveSuitMap(Dictionary<string, Suit> map, ConfigNode node)
    {
      foreach (var entry in map)
      {
        string suitName = entry.Value == null ? "GENERIC" : entry.Value.name;

        node.AddValue(entry.Key, suitName);
      }
    }

    /**
     * Fill config for custom Kerbal heads and suits.
     */
    void readKerbalsConfigs()
    {
      var excludedHeads = new List<string>();
      var excludedSuits = new List<string>();
      var femaleHeads = new List<string>();
      var femaleSuits = new List<string>();
      var eyelessHeads = new List<string>();

      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer"))
      {
        ConfigNode customNode = file.config.GetNode("CustomKerbals");
        if (customNode != null)
          loadKerbals(customNode);

        ConfigNode genericNode = file.config.GetNode("GenericKerbals");
        if (genericNode != null)
        {
          Util.addLists(genericNode.GetValues("excludedHeads"), excludedHeads);
          Util.addLists(genericNode.GetValues("excludedSuits"), excludedSuits);
          Util.addLists(genericNode.GetValues("femaleHeads"), femaleHeads);
          Util.addLists(genericNode.GetValues("femaleSuits"), femaleSuits);
          Util.addRELists(genericNode.GetValues("femaleNames"), kerminNames);
          Util.addLists(genericNode.GetValues("eyelessHeads"), eyelessHeads);
          Util.parse(genericNode.GetValue("suitAssignment"), ref suitAssignment);
        }

        ConfigNode perkNode = file.config.GetNode("PerkSuits");
        if (perkNode != null)
          loadSuitMap(perkNode, perkSuits);

        ConfigNode cabinNode = file.config.GetNode("CabinSuits");
        if (cabinNode != null)
          loadSuitMap(cabinNode, cabinSuits);
      }

      // Tag female and eye-less heads.
      foreach (Head head in heads)
      {
        head.isFemale = femaleHeads.Contains(head.name);
        head.isEyeless = eyelessHeads.Contains(head.name);
      }
      // Tag female suits.
      foreach (Suit suit in suits)
        suit.isFemale = femaleSuits.Contains(suit.name);

      foreach (var entry in gameKerbals)
        customKerbals.Add(entry.Key, entry.Value);

      foreach (var entry in perkSuits)
        defaultPerkSuits.Add(entry.Key, entry.Value);

      // Create lists of male heads and suits.
      kerbalHeads.AddRange(heads.Where(h => !h.isFemale && !excludedHeads.Contains(h.name)));
      kerbalSuits.AddRange(suits.Where(s => !s.isFemale && !excludedSuits.Contains(s.name)));

      // Create lists of female heads and suits.
      kerminHeads.AddRange(heads.Where(h => h.isFemale && !excludedHeads.Contains(h.name)));
      kerminSuits.AddRange(suits.Where(s => s.isFemale && !excludedSuits.Contains(s.name)));

      // Trim lists.
      heads.TrimExcess();
      suits.TrimExcess();
      kerbalHeads.TrimExcess();
      kerbalSuits.TrimExcess();
      kerminHeads.TrimExcess();
      kerminSuits.TrimExcess();
      kerminNames.TrimExcess();
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      Util.parse(rootNode.GetValue("isHelmetRemovalEnabled"), ref isHelmetRemovalEnabled);
      Util.parse(rootNode.GetValue("isAtmSuitEnabled"), ref isAtmSuitEnabled);
      Util.parse(rootNode.GetValue("atmSuitPressure"), ref atmSuitPressure);
      Util.addLists(rootNode.GetValues("atmSuitBodies"), atmSuitBodies);
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      var suitDirs = new Dictionary<string, int>();
      string lastTextureName = "";

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(Util.DIR, StringComparison.Ordinal))
          continue;

        // Add a head texture.
        if (texture.name.StartsWith(DIR_HEADS, StringComparison.Ordinal))
        {
          texture.wrapMode = TextureWrapMode.Clamp;

          string headName = texture.name.Substring(DIR_HEADS.Length);
          if (headName.EndsWith("NRM", StringComparison.Ordinal))
          {
            string baseName = headName.Substring(0, headName.Length - 3);

            Head head = heads.Find(h => h.name == baseName);
            if (head != null)
              head.headNRM = texture;
          }
          else if (heads.All(h => h.name != headName))
          {
            Head head = new Head { name = headName, head = texture };
            heads.Add(head);
          }
        }
        // Add a suit texture.
        else if (texture.name.StartsWith(DIR_SUITS, StringComparison.Ordinal))
        {
          texture.wrapMode = TextureWrapMode.Clamp;

          int lastSlash = texture.name.LastIndexOf('/');
          int dirNameLength = lastSlash - DIR_SUITS.Length;
          string originalName = texture.name.Substring(lastSlash + 1);

          if (dirNameLength < 1)
          {
            Util.log("Suit texture should be inside a subdirectory: {0}", texture.name);
          }
          else
          {
            string dirName = texture.name.Substring(DIR_SUITS.Length, dirNameLength);

            Suit suit;
            if (suitDirs.ContainsKey(dirName))
            {
              int index = suitDirs[dirName];
              suit = suits[index];
            }
            else
            {
              int index = suits.Count;
              suit = new Suit { name = dirName };
              suits.Add(suit);

              suitDirs.Add(dirName, index);
            }

            if (!suit.setTexture(originalName, texture))
              Util.log("Unknown suit texture name \"{0}\": {1}", originalName, texture.name);
          }
        }
        else if (texture.name.StartsWith(DIR_DEFAULT, StringComparison.Ordinal))
        {
          int lastSlash = texture.name.LastIndexOf('/');
          string originalName = texture.name.Substring(lastSlash + 1);

          if (originalName == "kerbalHead")
          {
            defaultHead.head = texture;
            texture.wrapMode = TextureWrapMode.Clamp;
          }
          else if (originalName == "kerbalHeadNRM")
          {
            defaultHead.headNRM = texture;
            texture.wrapMode = TextureWrapMode.Clamp;
          }
          else if (defaultSuit.setTexture(originalName, texture) || originalName == "kerbalMain")
          {
            texture.wrapMode = TextureWrapMode.Clamp;
          }
        }

        lastTextureName = texture.name;
      }

      readKerbalsConfigs();

      // Save pointer to helmet & visor meshes so helmet removal can restore them.
      foreach (KerbalEVA eva in Resources.FindObjectsOfTypeAll<KerbalEVA>())
      {
        foreach (SkinnedMeshRenderer smr in eva.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
          if (smr.name == "helmet")
            helmetMesh = smr.sharedMesh;
          else if (smr.name == "visor")
            visorMesh = smr.sharedMesh;
        }

        // Install module for KerbalEVA part to enable EVA suit toggle.
        if (eva.GetComponent<TREvaModule>() == null)
          eva.gameObject.AddComponent<TREvaModule>();
      }
    }

    /**
     * Default Kerbal textures are not loaded until main menu shows, so part of initialisation must
     * be performed then.
     */
    void initialiseDefaultKerbal()
    {
      foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
      {
        if (texture.name != null
            && (texture.name.StartsWith("kerbal", StringComparison.Ordinal)
            || texture.name.StartsWith("EVA", StringComparison.Ordinal)))
        {
          if (texture.name == "kerbalHead")
            defaultHead.head = defaultHead.head ?? texture;
          else
            defaultSuit.setTexture(texture.name, texture);
        }
      }

      // Set default suits on proto-IVA Kerbal.
      foreach (Kerbal kerbal in Resources.FindObjectsOfTypeAll<Kerbal>())
      {
        kerbal.textureStandard = defaultSuit.suit;
        kerbal.textureVeteran = defaultSuit.suitVeteran;
      }
    }

    public void resetScene()
    {
      previousVessel = null;
      ivaVessels.Clear();

      if (HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onVesselChange.Add(scheduleSwitchUpdate);
        GameEvents.onVesselWasModified.Add(scheduleSwitchUpdate);
        GameEvents.onCrewTransferred.Add(scheduleTransferUpdate);
        GameEvents.onVesselCreate.Add(scheduleSpawnUpdate);
        GameEvents.onVesselLoaded.Add(scheduleSpawnUpdate);
        GameEvents.onVesselSituationChange.Add(updateHelmets);
      }
      else
      {
        GameEvents.onVesselChange.Remove(scheduleSwitchUpdate);
        GameEvents.onVesselWasModified.Remove(scheduleSwitchUpdate);
        GameEvents.onCrewTransferred.Remove(scheduleTransferUpdate);
        GameEvents.onVesselCreate.Remove(scheduleSpawnUpdate);
        GameEvents.onVesselLoaded.Remove(scheduleSpawnUpdate);
        GameEvents.onVesselSituationChange.Remove(updateHelmets);
      }

      if (HighLogic.LoadedScene == GameScenes.MAINMENU)
        initialiseDefaultKerbal();
    }

    public void updateScene()
    {
      // IVA texture replacement pass. It is scheduled via event callbacks.
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (ivaVessels.Count != 0)
          personaliseIVAs();
      }
    }

    public void loadScenario(ConfigNode node)
    {
      gameKerbals.Clear();
      perkSuits.Clear();

      loadKerbals(node.GetNode("Kerbals") ?? node.GetNode("CustomKerbals"));
      loadSuitMap(node.GetNode("PerkSuits"), perkSuits);

      Util.parse(node.GetValue("isHelmetRemovalEnabled"), ref isHelmetRemovalEnabled);
      Util.parse(node.GetValue("isAtmSuitEnabled"), ref isAtmSuitEnabled);
      Util.parse(node.GetValue("suitAssignment"), ref suitAssignment);
    }

    public void saveScenario(ConfigNode node)
    {
      saveKerbals(node.AddNode("Kerbals"));
      saveSuitMap(perkSuits, node.AddNode("PerkSuits"));

      node.AddValue("isHelmetRemovalEnabled", isHelmetRemovalEnabled);
      node.AddValue("isAtmSuitEnabled", isAtmSuitEnabled);
      node.AddValue("suitAssignment", suitAssignment);
    }

    /**
     * Set external EVA/IVA suit. Fails and return false iff trying to remove EVA suit outside of
     * breathable atmosphere.
     * This function is used by TREvaModule.
     */
    public bool personalise(Part evaPart, bool evaSuit)
    {
      bool success = true;

      List<ProtoCrewMember> crew = evaPart.protoModuleCrew;
      if (crew.Count != 0)
      {
        if (!evaSuit && !isAtmBreathable())
        {
          evaSuit = true;
          success = false;
        }

        personaliseKerbal(evaPart, crew[0], null, evaSuit);
      }
      return success;
    }
  }
}
