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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TextureReplacer
{
  class Personaliser
  {
    enum SuitAssignment
    {
      RANDOM,
      CONSECUTIVE
    }

    class Head
    {
      public string name;
      public bool isFemale;
      public bool isEyeless;
      public Texture2D head;
      public Texture2D headNRM;
    }

    class Suit
    {
      public string name;
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
          case "kerbalMainGrey":
            suit = texture;
            return true;
          case "kerbalMainNRM":
            suitNRM = texture;
            return true;
          case "kerbalHelmetGrey":
            helmet = texture;
            return true;
          case "kerbalHelmetNRM":
            helmetNRM = texture;
            return true;
          case "kerbalVisor":
            visor = texture;
            return true;
          case "EVAtexture":
            evaSuit = texture;
            return true;
          case "EVAtextureNRM":
            evaSuitNRM = texture;
            return true;
          case "EVAhelmet":
            evaHelmet = texture;
            return true;
          case "EVAvisor":
            evaVisor = texture;
            return true;
          case "EVAjetpack":
            evaJetpack = texture;
            return true;
          case "EVAjetpackNRM":
            evaJetpackNRM = texture;
            return true;
          default:
            return false;
        }
      }
    }

    static readonly string DIR_DEFAULT = Util.DIR + "Default/";
    static readonly string DIR_HEADS = Util.DIR + "Heads/";
    static readonly string DIR_SUITS = Util.DIR + "Suits/";
    // Delay for IVA replacement (in seconds).
    static readonly float IVA_TIMER_DELAY = 0.2f;
    // Kerbal textures.
    readonly Suit defaultSuit = new Suit { name = "DEFAULT" };
    readonly List<Head> heads = new List<Head>();
    readonly List<Suit> suits = new List<Suit>();
    readonly List<Head> kerminHeads = new List<Head>();
    readonly List<Suit> kerminSuits = new List<Suit>();
    // Female name REs.
    readonly List<Regex> kerminNames = new List<Regex>();
    // Personalised Kerbal textures.
    readonly Dictionary<string, Head> customHeads = new Dictionary<string, Head>();
    readonly Dictionary<string, Suit> customSuits = new Dictionary<string, Suit>();
    // Cabin-specific suits.
    readonly Dictionary<string, Suit> cabinSuits = new Dictionary<string, Suit>();
    // Helmet removal.
    Mesh helmetMesh = null;
    Mesh visorMesh = null;
    bool isHelmetRemovalEnabled = true;
    // Atmospheric IVA suit parameters.
    bool isAtmSuitEnabled = true;
    double atmSuitPressure = 0.5;
    readonly HashSet<string> atmSuitBodies = new HashSet<string>();
    // Whether assignment of suits should be consecutive.
    SuitAssignment suitAssignment = SuitAssignment.RANDOM;
    // Update counter for IVA replacement. It has to scheduled with a little lag to avoid race
    // conditions with stock IVA texture replacement that sets orange suits to Jeb, Bill and Bob and
    // grey suits to other Kerbals.
    float ivaReplaceTimer = -1.0f;
    // For transparent pods, previous vessel IVA has to be updated too on vessel switching since
    // textures are reset to stock on switch.
    Vessel previousVessel = null;
    // List of vessels where IVA textures have to be updated. In stock game it suffices to only
    // perform this on the current vessel but not so if one uses transparent pods (JSITransparentPod
    // or sfr modules).
    readonly List<Vessel> ivaVessels = new List<Vessel>();
    // List of vessels for which Kerbal EVA has to be updated (either vessel is an EVA or has an EVA
    // on an external seat).
    readonly List<Vessel> evaVessels = new List<Vessel>();
    // Instance.
    public static Personaliser instance = null;

    /**
     * Replace textures on a Kerbal model.
     */
    void personaliseKerbal(Component component, ProtoCrewMember kerbal, Part cabin, bool isAtmSuit)
    {
      int spaceIndex = kerbal.name.IndexOf(' ');
      string name = spaceIndex > 0 ? kerbal.name.Substring(0, spaceIndex) : kerbal.name;

      bool isFemale = kerminNames.Any(r => r.IsMatch(name));
      bool isEva = cabin == null;

      List<Head> genderHeads = isFemale && kerminHeads.Count != 0 ? kerminHeads : heads;
      List<Suit> genderSuits = isFemale && kerminSuits.Count != 0 ? kerminSuits : suits;

      Head head;
      Suit suit;

      if (!customHeads.TryGetValue(kerbal.name, out head) && genderHeads.Count != 0)
      {
        // Hash is multiplied with a large prime to increase randomisation, since hashes returned by
        // `GetHashCode()` are close together if strings only differ in the last (few) char(s).
        int index = ((name.GetHashCode() * 4099) & 0x7fffffff) % genderHeads.Count;
        head = genderHeads[index];
      }
      if ((isEva || !cabinSuits.TryGetValue(cabin.partInfo.name, out suit))
          && !customSuits.TryGetValue(kerbal.name, out suit) && genderSuits.Count != 0)
      {
        // Here we must use a different prime to increase randomisation so that the same head is
        // not always combined with the same suit.
        int number = suitAssignment == SuitAssignment.RANDOM ?
                     ((name.GetHashCode() + name.Length) * 2053) & 0x7fffffff :
                     HighLogic.CurrentGame.CrewRoster.IndexOf(kerbal);

        suit = genderSuits[number % genderSuits.Count];
      }

      foreach (Renderer renderer in component.GetComponentsInChildren<Renderer>())
      {
        var smr = renderer as SkinnedMeshRenderer;

        // Thruster jets, flag decals and headlight flares.
        if (smr == null)
        {
          if (isAtmSuit && renderer.name != "screenMessage")
            renderer.enabled = false;
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
              }
              break;

            case "body01":
              bool isEvaSuit = isEva && !isAtmSuit;

              if (suit != null)
              {
                newTexture = isEvaSuit ? suit.evaSuit : suit.suit;
                newNormalMap = isEvaSuit ? suit.evaSuitNRM : suit.suitNRM;
              }

              // This required to fix IVA suits after KSP resetting them to the stock ones all the
              // time. If there is the default replacement for IVA suit texture and the current
              // Kerbal skin contains no IVA suit, we must set it to the default replacement,
              // otherwise the stock one will be used.
              if (!isEvaSuit)
              {
                if (newTexture == null)
                  newTexture = defaultSuit.suit;
                if (newNormalMap == null)
                  newNormalMap = defaultSuit.suitNRM;
              }
              break;

            case "helmet":
              if (isEva && isAtmSuit)
              {
                smr.enabled = false;
              }
              else if (suit != null)
              {
                if (!isEva)
                  smr.sharedMesh = isAtmSuit ? null : helmetMesh;

                newTexture = isEva ? suit.evaHelmet : suit.helmet;
                newNormalMap = suit.helmetNRM;
              }
              break;

            case "visor":
              if (isEva && isAtmSuit)
              {
                smr.enabled = false;
              }
              else
              {
                if (!isEva)
                  smr.sharedMesh = isAtmSuit ? null : visorMesh;

                // Visor texture must be set every time, because the replacement on proto-IVA Kerbal
                // doesn't seem to work.
                Suit skin = suit ?? defaultSuit;
                newTexture = isEva ? skin.evaVisor : skin.visor;

                if (newTexture != null)
                  material.color = Color.white;
              }
              break;

            default: // Jetpack.
              if (isEva && isAtmSuit)
              {
                smr.enabled = false;
              }
              else if (suit != null)
              {
                newTexture = suit.evaJetpack;
                newNormalMap = suit.evaJetpackNRM;
              }
              break;
          }

          if (newTexture != null && newTexture != material.mainTexture)
            material.mainTexture = newTexture;

          if (newNormalMap != null && newNormalMap != material.GetTexture("_BumpMap"))
            material.SetTexture("_BumpMap", newNormalMap);
        }
      }
    }

    /**
     * Personalise Kerbals in internal spaces.
     */
    void personaliseIVA(Vessel vessel)
    {
      foreach (Part part in vessel.parts)
      {
        if (part.internalModel != null)
        {
          Kerbal[] kerbals = part.internalModel.GetComponentsInChildren<Kerbal>();
          if (kerbals.Length != 0)
          {
            bool hideHelmets = isHelmetRemovalEnabled
                               && vessel.situation != Vessel.Situations.FLYING
                               && vessel.situation != Vessel.Situations.SUB_ORBITAL;

            foreach (Kerbal kerbal in kerbals)
              personaliseKerbal(kerbal, kerbal.protoCrewMember, kerbal.InPart, hideHelmets);
          }
        }
      }
    }

    /**
     * Personalise Kerbal EVA model.
     */
    void personaliseEVA(Vessel vessel)
    {
      double atmPressure = FlightGlobals.getStaticPressure();
      // Workaround for a KSP bug that reports pressure the same as pressure on altitude 0 whenever
      // a Kerbal leaves an external seat. But we don't need to personalise textures for Kerbals
      // that leave a seat anyway.
      if (atmPressure == FlightGlobals.currentMainBody.atmosphereMultiplier)
        return;

      bool isAtmSuit = isAtmSuitEnabled
                       && atmPressure >= atmSuitPressure
                       && atmSuitBodies.Contains(FlightGlobals.currentMainBody.bodyName);

      KerbalEVA eva = vessel.GetComponent<KerbalEVA>();
      if (eva != null)
      {
        // Vessel is a Kerbal.
        List<ProtoCrewMember> crew = vessel.rootPart.protoModuleCrew;
        if (crew.Count != 0)
          personaliseKerbal(eva, crew[0], null, isAtmSuit);
      }
      else
      {
        // Vessel is a ship. Update Kerbals on external seats.
        foreach (Part part in vessel.parts)
        {
          KerbalSeat seat = part.GetComponent<KerbalSeat>();
          if (seat == null || seat.Occupant == null)
            continue;

          List<ProtoCrewMember> crew = seat.Occupant.protoModuleCrew;
          if (crew.Count != 0)
            personaliseKerbal(seat.Occupant, crew[0], null, isAtmSuit);
        }
      }
    }

    /**
     * Set custom and random Kerbals' textures.
     */
    void replaceKerbalSkins()
    {
      // IVA textures must be replaced with a little lag, otherwise we risk race conditions with KSP
      // handler that resets IVA suits to the stock ones. The race condition issue always occurs
      // when boarding an external seat.
      if (ivaReplaceTimer == 0.0f)
      {
        foreach (Vessel vessel in ivaVessels)
        {
          if (vessel != null && vessel.loaded && vessel.vesselName != null)
            personaliseIVA(vessel);
        }

        ivaReplaceTimer = -1.0f;

        ivaVessels.Clear();
        // Prevent list capacity from growing too much.
        if (ivaVessels.Capacity > 16)
          ivaVessels.TrimExcess();
      }

      if (evaVessels.Count != 0)
      {
        foreach (Vessel vessel in evaVessels)
        {
          if (vessel != null && vessel.loaded && vessel.vesselName != null)
            personaliseEVA(vessel);
        }

        evaVessels.Clear();
        // Prevent list capacity from growing too much.
        if (evaVessels.Capacity > 16)
          evaVessels.TrimExcess();
      }
    }

    /**
     * Update IVA textures on vessel switch or docking.
     */
    void scheduleSwitchUpdate(Vessel vessel)
    {
      if (previousVessel != null && previousVessel != vessel)
      {
        ivaReplaceTimer = IVA_TIMER_DELAY;
        ivaVessels.Add(previousVessel);
      }
      if (vessel != null)
      {
        ivaReplaceTimer = IVA_TIMER_DELAY;
        ivaVessels.Add(vessel);
      }
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
     * Update EVA textures when a new Kerbal is created or when one comes into 2.3 km range.
     */
    void scheduleSpawnUpdate(Vessel vessel)
    {
      if (vessel != null)
      {
        ivaReplaceTimer = IVA_TIMER_DELAY;
        ivaVessels.Add(vessel);
        evaVessels.Add(vessel);
      }
    }

    /**
     * Enable/disable helmets in the current IVA space depending on situation.
     */
    void updateHelmets(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> eventData)
    {
      Vessel vessel = eventData.host;
      if (vessel == null)
        return;

      foreach (Part part in vessel.parts)
      {
        if (part.internalModel != null)
        {
          Kerbal[] kerbals = part.internalModel.GetComponentsInChildren<Kerbal>();
          if (kerbals.Length != 0)
          {
            bool hideHelmets = isHelmetRemovalEnabled
                               && vessel.situation != Vessel.Situations.FLYING
                               && vessel.situation != Vessel.Situations.SUB_ORBITAL;

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
     * Fill config for custom Kerbal heads and suits.
     */
    void readKerbalsConfigs()
    {
      var excludedHeads = new List<string>();
      var excludedSuits = new List<string>();
      var femaleHeads = new List<string>();
      var femaleSuits = new List<string>();
      var femaleNames = new List<string>();
      var eyelessHeads = new List<string>();

      foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer"))
      {
        ConfigNode customNode = file.config.GetNode("CustomKerbals");
        if (customNode != null)
        {
          foreach (ConfigNode.Value entry in customNode.values)
          {
            string[] tokens = Util.splitConfigValue(entry.value);
            string name = entry.name;
            string headName = tokens.Length >= 1 ? tokens[0] : null;
            string suitName = tokens.Length >= 2 ? tokens[1] : null;

            if (headName != null)
            {
              if (headName == "GENERIC")
              {
                if (customHeads.ContainsKey(name))
                {
                  customHeads.Remove(name);
                  Util.log("Unmapped head for \"{0}\"", name);
                }
              }
              else
              {
                Head head = null;
                if (headName != "DEFAULT")
                {
                  string _headName = headName;
                  head = heads.FirstOrDefault(h => h.name == _headName);
                }
                if (head == null)
                  headName = "DEFAULT";

                customHeads[name] = head;
                Util.log("Mapped head for \"{0}\" -> {1}", name, headName);
              }
            }

            if (suitName != null)
            {
              if (suitName == "GENERIC")
              {
                if (customSuits.ContainsKey(name))
                {
                  customSuits.Remove(name);
                  Util.log("Unmapped suit for \"{0}\"", name);
                }
              }
              else
              {
                Suit suit = null;
                if (suitName != "DEFAULT")
                {
                  string _suitName = suitName;
                  suit = suits.FirstOrDefault(s => s.name == _suitName);
                }
                if (suit == null)
                  suitName = "DEFAULT";

                customSuits[name] = suit;
                Util.log("Mapped suit for \"{0}\" -> {1}", name, suitName);
              }
            }
          }
        }

        ConfigNode genericNode = file.config.GetNode("GenericKerbals");
        if (genericNode != null)
        {
          foreach (string sExcludedHeads in genericNode.GetValues("excludedHeads"))
            excludedHeads.AddRange(Util.splitConfigValue(sExcludedHeads));

          foreach (string sExcludedSuits in genericNode.GetValues("excludedSuits"))
            excludedSuits.AddRange(Util.splitConfigValue(sExcludedSuits));

          foreach (string sFemaleHeads in genericNode.GetValues("femaleHeads"))
            femaleHeads.AddRange(Util.splitConfigValue(sFemaleHeads));

          foreach (string sFemaleSuits in genericNode.GetValues("femaleSuits"))
            femaleSuits.AddRange(Util.splitConfigValue(sFemaleSuits));

          foreach (string sFemaleNames in genericNode.GetValues("femaleNames"))
            femaleNames.AddRange(Util.splitConfigValue(sFemaleNames));

          foreach (string sEyelessHeads in genericNode.GetValues("eyelessHeads"))
            eyelessHeads.AddRange(Util.splitConfigValue(sEyelessHeads));

          string sSuitAssignment = genericNode.GetValue("suitAssignment");
          if (sSuitAssignment != null)
          {
            if (sSuitAssignment == "random")
              suitAssignment = SuitAssignment.RANDOM;
            else if (sSuitAssignment == "consecutive")
              suitAssignment = SuitAssignment.CONSECUTIVE;
            else
              Util.log("Invalid value for suitAssignment: {0}", sSuitAssignment);
          }
        }

        ConfigNode cabinNode = file.config.GetNode("CabinSuits");
        if (cabinNode != null)
        {
          foreach (ConfigNode.Value entry in cabinNode.values)
          {
            string cabinName = entry.name;
            string suitName = entry.value;

            if (suitName.Length != 0)
            {
              Suit suit = suits.FirstOrDefault(s => s.name == suitName) ?? defaultSuit;
              cabinSuits[cabinName] = suit;
              Util.log("Mapped cabin suit for \"{0}\" -> {1}", cabinName, suit.name);
            }
          }
        }
      }

      // Tag female and eye-less heads.
      foreach (Head head in heads)
      {
        head.isFemale = femaleHeads.Contains(head.name);
        head.isEyeless = eyelessHeads.Contains(head.name);
      }

      // Remove excluded heads/suits.
      heads.RemoveAll(h => excludedHeads.Contains(h.name));
      suits.RemoveAll(s => excludedSuits.Contains(s.name));

      // Create lists of female heads and suits.
      kerminHeads.AddRange(heads.Where(h => femaleHeads.Contains(h.name)));
      kerminSuits.AddRange(suits.Where(s => femaleSuits.Contains(s.name)));

      heads.RemoveAll(h => femaleHeads.Contains(h.name));
      suits.RemoveAll(s => femaleSuits.Contains(s.name));

      // Compile regular expressions for female names.
      femaleNames.ForEach(n => kerminNames.Add(new Regex(n)));

      // Trim lists.
      heads.TrimExcess();
      suits.TrimExcess();
      kerminHeads.TrimExcess();
      kerminSuits.TrimExcess();
      kerminNames.TrimExcess();
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      string sIsHelmetRemovalEnabled = rootNode.GetValue("isHelmetRemovalEnabled");
      if (sIsHelmetRemovalEnabled != null)
        Boolean.TryParse(sIsHelmetRemovalEnabled, out isHelmetRemovalEnabled);

      string sIsAtmSuitEnabled = rootNode.GetValue("isAtmSuitEnabled");
      if (sIsAtmSuitEnabled != null)
        Boolean.TryParse(sIsAtmSuitEnabled, out isAtmSuitEnabled);

      string sAtmSuitPressure = rootNode.GetValue("atmSuitPressure");
      if (sAtmSuitPressure != null)
        Double.TryParse(sAtmSuitPressure, out atmSuitPressure);

      foreach (string sAtmSuitBodies in rootNode.GetValues("atmSuitBodies"))
      {
        foreach (string s in Util.splitConfigValue(sAtmSuitBodies))
          atmSuitBodies.Add(s);
      }
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
        if (texture == null || !texture.name.StartsWith(Util.DIR))
          continue;

        // Add a head texture.
        if (texture.name.StartsWith(DIR_HEADS))
        {
          texture.wrapMode = TextureWrapMode.Clamp;

          string headName = texture.name.Substring(DIR_HEADS.Length);
          if (headName.EndsWith("NRM"))
          {
            string baseName = headName.Substring(0, headName.Length - 3);

            Head head = heads.Find(h => h.name == baseName);
            if (head != null)
            {
              head.headNRM = texture;
              Util.log("Mapped head \"{0}\" normal map -> {1}", head.name, texture.name);
            }
          }
          else if (heads.All(h => h.name != headName))
          {
            var head = new Head { name = headName, head = texture };
            heads.Add(head);
            Util.log("Mapped head \"{0}\" -> {1}", head.name, texture.name);
          }
        }
        // Add a suit texture.
        else if (texture.name.StartsWith(DIR_SUITS))
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

            if (suit.setTexture(originalName, texture))
              Util.log("Mapped suit \"{0}\" {1} -> {2}", dirName, originalName, texture.name);
            else
              Util.log("Unknown suit texture name \"{0}\": {1}", originalName, texture.name);
          }
        }
        else if (texture.name.StartsWith(DIR_DEFAULT))
        {
          int lastSlash = texture.name.LastIndexOf('/');
          string originalName = texture.name.Substring(lastSlash + 1);

          if (originalName == "kerbalHead")
            texture.wrapMode = TextureWrapMode.Clamp;

          if (defaultSuit.setTexture(originalName, texture))
          {
            texture.wrapMode = TextureWrapMode.Clamp;
            Util.log("Mapped default suit {0} -> {1}", originalName, texture.name);
          }
        }

        lastTextureName = texture.name;
      }

      readKerbalsConfigs();

      // Save pointer to helmet & visor meshes so helmet removal can restore them.
      foreach (SkinnedMeshRenderer smr
               in Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)))
      {
        if (smr.name == "helmet")
          helmetMesh = smr.sharedMesh;
        else if (smr.name == "visor")
          visorMesh = smr.sharedMesh;
      }
    }

    public void resetScene()
    {
      ivaReplaceTimer = -1.0f;
      previousVessel = null;
      ivaVessels.Clear();
      evaVessels.Clear();

      if (HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onVesselChange.Add(scheduleSwitchUpdate);
        GameEvents.onVesselWasModified.Add(scheduleSwitchUpdate);
        GameEvents.onCrewTransferred.Add(scheduleTransferUpdate);
        GameEvents.onVesselCreate.Add(scheduleSpawnUpdate);
        GameEvents.onVesselLoaded.Add(scheduleSpawnUpdate);

        if (isHelmetRemovalEnabled)
          GameEvents.onVesselSituationChange.Add(updateHelmets);
      }
      else
      {
        GameEvents.onVesselChange.Remove(scheduleSwitchUpdate);
        GameEvents.onVesselWasModified.Remove(scheduleSwitchUpdate);
        GameEvents.onCrewTransferred.Remove(scheduleTransferUpdate);
        GameEvents.onVesselCreate.Remove(scheduleSpawnUpdate);
        GameEvents.onVesselLoaded.Remove(scheduleSpawnUpdate);

        if (isHelmetRemovalEnabled)
          GameEvents.onVesselSituationChange.Remove(updateHelmets);
      }
    }

    public void updateScene()
    {
      // IVA/EVA texture replacement pass. It is scheduled via event callbacks.
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (ivaReplaceTimer == 0.0f || evaVessels.Count != 0)
          replaceKerbalSkins();

        if (ivaReplaceTimer > 0)
          ivaReplaceTimer = Math.Max(0.0f, ivaReplaceTimer - Time.deltaTime);
      }
    }
  }
}
