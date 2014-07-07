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
using UnityEngine;

namespace TextureReplacer
{
  internal class Personaliser
  {
    private enum SuitAssignment
    {
      RANDOM,
      CONSECUTIVE
    }

    private class Head
    {
      public string name;
      public bool isFemale;
      public bool isEyeless;
      public Texture2D head;
      public Texture2D headNRM;
    }

    private class Suit
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
          {
            suit = texture;
            return true;
          }
          case "kerbalMainNRM":
          {
            suitNRM = texture;
            return true;
          }
          case "kerbalHelmetGrey":
          {
            helmet = texture;
            return true;
          }
          case "kerbalHelmetNRM":
          {
            helmetNRM = texture;
            return true;
          }
          case "kerbalVisor":
          {
            visor = texture;
            return true;
          }
          case "EVAtexture":
          {
            evaSuit = texture;
            return true;
          }
          case "EVAtextureNRM":
          {
            evaSuitNRM = texture;
            return true;
          }
          case "EVAhelmet":
          {
            evaHelmet = texture;
            return true;
          }
          case "EVAvisor":
          {
            evaVisor = texture;
            return true;
          }
          case "EVAjetpack":
          {
            evaJetpack = texture;
            return true;
          }
          case "EVAjetpackNRM":
          {
            evaJetpackNRM = texture;
            return true;
          }
          default:
          {
            return false;
          }
        }
      }
    }

    private static readonly string DIR_DEFAULT = Util.DIR + "Default/";
    private static readonly string DIR_HEADS = Util.DIR + "Heads/";
    private static readonly string DIR_SUITS = Util.DIR + "Suits/";
    // Delay for IVA replacement (in seconds).
    private static readonly float IVA_TIMER_DELAY = 0.2f;
    // Kerbal textures.
    private Suit defaultSuit = new Suit() { name = "DEFAULT" };
    private List<Head> heads = new List<Head>();
    private List<Suit> suits = new List<Suit>();
    private List<Suit> femaleSuits = new List<Suit>();
    // Personalised Kerbal textures.
    private Dictionary<string, Head> customHeads = new Dictionary<string, Head>();
    private Dictionary<string, Suit> customSuits = new Dictionary<string, Suit>();
    // Cabin-specific suits.
    private Dictionary<string, Suit> cabinSuits = new Dictionary<string, Suit>();
    // Atmospheric IVA suit parameters.
    private bool isAtmSuitEnabled = true;
    private double atmSuitPressure = 0.5;
    // Whether assignment of suits should be consecutive.
    private SuitAssignment suitAssignment = SuitAssignment.RANDOM;
    // List of vessels for which Kerbal EVA has to be updated (either vessel is an EVA or has an EVA
    // on an external seat).
    private List<Vessel> kerbalVessels = new List<Vessel>();
    // Update counter for IVA replacement. It has to scheduled with a little lag to avoid race
    // conditions with stock IVA texture replacement that sets orange suits to Jeb, Bill and Bob and
    // grey suits to other Kerbals.
    private float ivaReplaceTimer = -1.0f;
    // Helmet removal.
    private Mesh helmetMesh = null;
    private Mesh visorMesh = null;
    private bool isHelmetRemovalEnabled = true;
    // An alternative, more expensive, IVA replacement method must be used for sfr pods.
    private bool isSfrDetected = false;
    // Instance.
    public static Personaliser instance = null;

    /**
     * Replace Kerbal textures.
     *
     * This is a helper method for `replaceKerbalSkins()`. It sets personalised or random textures
     * for an IVA or an EVA Kerbal.
     */
    private void replaceKerbalSkin(Component component, ProtoCrewMember kerbal, Part inPart,
                                   bool isAtmSuit)
    {
      Head head = null;
      Suit suit = null;

      if (!customHeads.TryGetValue(kerbal.name, out head) && heads.Count != 0)
      {
        // Hash is multiplied with a large prime to increase randomisation, since hashes returned by
        // `GetHashCode()` are close together if strings only differ in the last (few) char(s).
        int index = ((kerbal.name.GetHashCode() * 4099) & 0x7fffffff) % heads.Count;
        head = heads[index];
      }

      bool isEva = inPart == null;
      bool isFemale = head == null ? false : head.isFemale;
      List<Suit> genderSuits = isFemale ? femaleSuits : suits;

      if ((isEva || !cabinSuits.TryGetValue(inPart.partInfo.name, out suit))
          && !customSuits.TryGetValue(kerbal.name, out suit) && genderSuits.Count != 0)
      {
        // Here we must use a different prime to increase randomisation so that the same head is
        // not always combined with the same suit.
        int number = suitAssignment == SuitAssignment.RANDOM ?
                       (kerbal.name.GetHashCode() * 2053) & 0x7fffffff :
                       HighLogic.CurrentGame.CrewRoster.IndexOf(kerbal);

        suit = genderSuits[number % genderSuits.Count];
      }

      foreach (Renderer renderer in component.GetComponentsInChildren<Renderer>())
      {
        SkinnedMeshRenderer smr = renderer as SkinnedMeshRenderer;

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
            {
              if (head != null && head.isEyeless)
                smr.sharedMesh = null;

              break;
            }
            case "headMesh01":
            case "upTeeth01":
            case "upTeeth02":
            case "tongue":
            {
              if (head != null)
              {
                newTexture = head.head;
                newNormalMap = head.headNRM;
              }
              break;
            }
            case "body01":
            {
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
            }
            case "helmet":
            {
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
            }
            case "visor":
            {
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
            }
            default: // Jetpack.
            {
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
          }

          if (newTexture != null && newTexture != material.mainTexture)
            material.mainTexture = newTexture;

          if (newNormalMap != null && newNormalMap != material.GetTexture("_BumpMap"))
            material.SetTexture("_BumpMap", newNormalMap);
        }
      }
    }

    /**
     * Set custom and random Kerbals' textures.
     */
    private void replaceKerbalSkins()
    {
      // IVA textures must be replaced with a little lag, otherwise we risk race conditions with KSP
      // handler that resets IVA suits to the stock ones. The race condition issue always occurs
      // when boarding an external seat.
      if (ivaReplaceTimer == 0.0f)
      {
        Vessel vessel = FlightGlobals.ActiveVessel;
        Kerbal[] kerbals = isSfrDetected ? (Kerbal[]) Kerbal.FindObjectsOfType(typeof(Kerbal)) :
                           InternalSpace.Instance == null ? null :
                           InternalSpace.Instance.GetComponentsInChildren<Kerbal>();

        if (vessel != null && kerbals != null)
        {
          bool hideHelmets = isHelmetRemovalEnabled
                             && vessel.situation != Vessel.Situations.FLYING
                             && vessel.situation != Vessel.Situations.SUB_ORBITAL
                             && vessel.situation != Vessel.Situations.PRELAUNCH;

          foreach (Kerbal kerbal in kerbals)
            replaceKerbalSkin(kerbal, kerbal.protoCrewMember, kerbal.InPart, hideHelmets);
        }

        ivaReplaceTimer = -1.0f;
      }

      if (kerbalVessels.Count != 0)
      {
        foreach (Vessel vessel in kerbalVessels)
        {
          if (vessel == null || !vessel.loaded || vessel.vesselName == null)
            continue;

          double atmPressure = FlightGlobals.getStaticPressure();
          // Workaround for a KSP bug that reports pressure the same as pressure on altitude 0
          // whenever a Kerbal leaves an external seat. But we don't need to personalise textures
          // for Kerbals that leave a seat anyway.
          if (atmPressure == FlightGlobals.currentMainBody.atmosphereMultiplier)
            continue;

          bool isAtmSuit = isAtmSuitEnabled
                           && atmPressure >= atmSuitPressure
                           && FlightGlobals.currentMainBody.atmosphereContainsOxygen;

          KerbalEVA eva = vessel.GetComponent<KerbalEVA>();
          if (eva != null)
          {
            // Vessel is a Kerbal.
            List<ProtoCrewMember> crew = vessel.rootPart.protoModuleCrew;
            if (crew.Count != 0)
              replaceKerbalSkin(eva, crew[0], null, isAtmSuit);
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
                replaceKerbalSkin(seat.Occupant, crew[0], null, isAtmSuit);
            }
          }
        }

        kerbalVessels.Clear();
        // Prevent list capacity from growing too much.
        if (kerbalVessels.Capacity > 16)
          kerbalVessels.TrimExcess();
      }
    }

    /**
     * Update IVA textures on vessel switch or docking.
     */
    private void scheduleIVAUpdate(Vessel vessel)
    {
      if (!vessel.isEVA && vessel.vesselName != null)
        ivaReplaceTimer = IVA_TIMER_DELAY;
    }

    /**
     * Update EVA textures when a new Kerbal is created or when one comes into 2.3 km range.
     */
    private void scheduleEVAUpdate(Vessel vessel)
    {
      kerbalVessels.Add(vessel);
    }

    /**
     * Enable/disable helmets in the current IVA space depending on situation.
     */
    private void updateHelmets(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> eventData)
    {
      Vessel vessel = FlightGlobals.ActiveVessel;

      if (vessel == eventData.host && vessel != null)
      {
        Kerbal[] kerbals = InternalSpace.Instance == null ? null :
                           InternalSpace.Instance.GetComponentsInChildren<Kerbal>();

        bool hideHelmets = vessel.situation != Vessel.Situations.FLYING
                           && vessel.situation != Vessel.Situations.SUB_ORBITAL
                           && vessel.situation != Vessel.Situations.PRELAUNCH;

        foreach (Kerbal kerbal in kerbals)
        {
          if (kerbal.showHelmet)
          {
            // Kerbal.ShowHelmet(false) irreversibly removes a helmet while Kerbal.ShowHelmet(true)
            // has no effect at all. We need the following workaround.
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

    /**
     * Fill config for custom Kerbal heads and suits.
     */
    private void readKerbalsConfigs()
    {
      List<string> excludedHeads = new List<string>();
      List<string> excludedSuits = new List<string>();
      List<string> femaleHeads = new List<string>();
      List<string> femaleSuits = new List<string>();
      List<string> eyelessHeads = new List<string>();

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
                  head = heads.FirstOrDefault(h => h.name == headName);
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
                  suit = suits.FirstOrDefault(s => s.name == suitName);
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
          string sExcludedHeads = genericNode.GetValue("excludedHeads");
          if (sExcludedHeads != null)
            excludedHeads.AddRange(Util.splitConfigValue(sExcludedHeads));

          string sExcludedSuits = genericNode.GetValue("excludedSuits");
          if (sExcludedSuits != null)
            excludedSuits.AddRange(Util.splitConfigValue(sExcludedSuits));

          string sFemaleHeads = genericNode.GetValue("femaleHeads");
          if (sFemaleHeads != null)
            femaleHeads.AddRange(Util.splitConfigValue(sFemaleHeads));

          string sFemaleSuits = genericNode.GetValue("femaleSuits");
          if (sFemaleSuits != null)
            femaleSuits.AddRange(Util.splitConfigValue(sFemaleSuits));

          string sEyelessHeads = genericNode.GetValue("eyelessHeads");
          if (sEyelessHeads != null)
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

      // Create female suits list.
      this.femaleSuits.AddRange(suits.Where(s => femaleSuits.Contains(s.name)));
      this.suits.RemoveAll(s => femaleSuits.Contains(s.name));
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public void readConfig(ConfigNode rootNode)
    {
      string sIsAtmSuitEnabled = rootNode.GetValue("isAtmSuitEnabled");
      if (sIsAtmSuitEnabled != null)
        Boolean.TryParse(sIsAtmSuitEnabled, out isAtmSuitEnabled);

      string sAtmSuitPressure = rootNode.GetValue("atmSuitPressure");
      if (sAtmSuitPressure != null)
        Double.TryParse(sAtmSuitPressure, out atmSuitPressure);

      string sIsHelmetRemovalEnabled = rootNode.GetValue("isHelmetRemovalEnabled");
      if (sIsHelmetRemovalEnabled != null)
        Boolean.TryParse(sIsHelmetRemovalEnabled, out isHelmetRemovalEnabled);
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      Dictionary<string, int> suitDirs = new Dictionary<string, int>();
      string lastTextureName = "";

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(Util.DIR))
          continue;

        // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
        // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides
        // the preceding texture.
        if (texture.name == lastTextureName)
        {
          Util.log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
        }
        // Add a head texture.
        else if (texture.name.StartsWith(DIR_HEADS))
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
          else
          {
            Head head = new Head() { name = headName, head = texture };
            heads.Add(head);
            Util.log("Mapped head \"{0}\" -> {1}", head.name, texture.name);
          }
        }
        // Add a suit texture.
        else if (texture.name.StartsWith(DIR_SUITS))
        {
          texture.wrapMode = TextureWrapMode.Clamp;

          int lastSlash = texture.name.LastIndexOf('/');
          int index = suits.Count;
          int dirNameLength = lastSlash - DIR_SUITS.Length;
          string originalName = texture.name.Substring(lastSlash + 1);

          if (dirNameLength < 1)
          {
            Util.log("Suit texture should be inside a subdirectory: {0}", texture.name);
          }
          else
          {
            string dirName = texture.name.Substring(DIR_SUITS.Length, dirNameLength);

            Suit suit = null;
            if (suitDirs.ContainsKey(dirName))
            {
              index = suitDirs[dirName];
              suit = suits[index];
            }
            else
            {
              suitDirs.Add(dirName, index);

              index = suits.Count;
              suit = new Suit() { name = dirName };
              suits.Add(suit);
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

      // Check if the srf mod is present.
      isSfrDetected = AssemblyLoader.loadedAssemblies.Any(a => a.name.StartsWith("sfrPartModules")) || 
                      AssemblyLoader.loadedAssemblies.Any(a => a.name.StartsWith("RasterPropMonitor")) ;
      if (isSfrDetected)
        Util.log("Detected sfr mod or RasterPropMonitor, enabling alternative Kerbal IVA texture replacement");

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
      kerbalVessels.Clear();

      if (HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onVesselChange.Add(scheduleIVAUpdate);
        GameEvents.onVesselWasModified.Add(scheduleIVAUpdate);
        GameEvents.onVesselCreate.Add(scheduleEVAUpdate);
        GameEvents.onVesselLoaded.Add(scheduleEVAUpdate);

        if (isHelmetRemovalEnabled)
          GameEvents.onVesselSituationChange.Add(updateHelmets);
      }
      else
      {
        GameEvents.onVesselChange.Remove(scheduleIVAUpdate);
        GameEvents.onVesselWasModified.Remove(scheduleIVAUpdate);
        GameEvents.onVesselCreate.Remove(scheduleEVAUpdate);
        GameEvents.onVesselLoaded.Remove(scheduleEVAUpdate);

        if (isHelmetRemovalEnabled)
          GameEvents.onVesselSituationChange.Remove(updateHelmets);
      }
    }

    public void updateScene()
    {
      // IVA/EVA texture replacement pass. It is scheduled via event callbacks.
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (ivaReplaceTimer == 0.0f || kerbalVessels.Count != 0)
          replaceKerbalSkins();

        if (ivaReplaceTimer > 0)
          ivaReplaceTimer = Math.Max(0.0f, ivaReplaceTimer - Time.deltaTime);
      }
    }
  }
}
