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
  public class Personaliser
  {
    private enum SuitAssignment
    {
      RANDOM,
      CONSECUTIVE
    }

    private class KerbalSuit
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

    private static readonly string DIR_DEFAULT = TextureReplacer.DIR + "Default/";
    private static readonly string DIR_HEADS = TextureReplacer.DIR + "Heads/";
    private static readonly string DIR_SUITS = TextureReplacer.DIR + "Suits/";
    // Kerbal textures.
    private KerbalSuit defaultSuit = new KerbalSuit() { name = "DEFAULT" };
    private List<Texture2D> heads = new List<Texture2D>();
    private List<KerbalSuit> suits = new List<KerbalSuit>();
    // Personalised Kerbal textures.
    private Dictionary<string, Texture2D> customHeads = new Dictionary<string, Texture2D>();
    private Dictionary<string, KerbalSuit> customSuits = new Dictionary<string, KerbalSuit>();
    // Atmospheric IVA suit parameters.
    private bool isAtmSuitEnabled = true;
    private double atmSuitPressure = 0.5;
    // Whether assignment of suits should be consecutive.
    private SuitAssignment suitAssignment = SuitAssignment.RANDOM;
    // List of vessels for which Kerbal EVA has to be updated (either vessel is an EVA or has an EVA
    // on an external seat).
    private List<Vessel> kerbalVessels = new List<Vessel>();
    // Update counter for IVA replacement. It has to scheduled with a few frame lag to avoid race
    // conditions with stock IVA texture replacement that sets orange suits to Jeb, Bill and Bob and
    // grey suits to other Kerbals.
    private int ivaReplaceCounter = -1;
    // An alternative, more expensive, IVA replacement method must be used for sfr pods.
    private bool isSfrDetected = false;
    // Instance.
    public static Personaliser instance = null;

    /**
     * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
     */
    private static void log(string s, params object[] args)
    {
      Debug.Log("[TR.Personaliser] " + String.Format(s, args));
    }

    /**
     * Replace Kerbal textures.
     *
     * This is a helper method for `replaceKerbalSkins()`. It sets personalised or random textures
     * for an IVA or an EVA Kerbal.
     */
    private void replaceKerbalSkin(Component component, ProtoCrewMember kerbal, bool isEva,
                                   bool isAtmSuit)
    {
      Texture2D headTexture = null;
      KerbalSuit suitSkin = null;

      customHeads.TryGetValue(kerbal.name, out headTexture);
      customSuits.TryGetValue(kerbal.name, out suitSkin);

      if (headTexture == null && heads.Count != 0)
      {
        // Hash is multiplied with a large prime to increase randomisation, since hashes returned by
        // `GetHashCode()` are close together if strings only differ in the last (few) char(s).
        int index = ((kerbal.name.GetHashCode() * 1021) & 0x7fffffff) % heads.Count;
        headTexture = heads[index];
      }

      if (suitSkin == null && suits.Count != 0)
      {
        if (suitAssignment == SuitAssignment.RANDOM)
        {
          // Here we must use a different prime to increase randomisation so that the same head is
          // not always combined with the same suit.
          int index = ((kerbal.name.GetHashCode() * 2053) & 0x7fffffff) % suits.Count;
          suitSkin = suits[index];
        }
        else
        {
          // Assign the suit based on consecutive number of a Kerbal.
          int index = HighLogic.CurrentGame.CrewRoster.IndexOf(kerbal) % suits.Count;
          suitSkin = suits[index];
        }
      }

      foreach (SkinnedMeshRenderer smr in component.GetComponentsInChildren<SkinnedMeshRenderer>())
      {
        Material material = smr.material;
        Texture2D newTexture = null;
        Texture2D newNormalMap = null;

        switch (smr.name)
        {
          case "eyeballLeft":
          case "eyeballRight":
          case "pupilLeft":
          case "pupilRight":
          {
            break;
          }
          case "headMesh01":
          case "upTeeth01":
          case "upTeeth02":
          case "tongue":
          {
            if (headTexture != null)
              newTexture = headTexture;

            break;
          }
          case "body01":
          {
            bool isEvaSuit = isEva && !isAtmSuit;

            if (suitSkin != null)
            {
              newTexture = isEvaSuit ? suitSkin.evaSuit : suitSkin.suit;
              newNormalMap = isEvaSuit ? suitSkin.evaSuitNRM : suitSkin.suitNRM;
            }

            // This required to fix IVA suits after KSP resetting them to the stock ones all the
            // time. If there is the default replacement for IVA suit texture and the current Kerbal
            // skin contains no IVA suit, we must set it to the default replacement, otherwise the
            // stock one will be used.
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
            if (isAtmSuit)
            {
              smr.sharedMesh = null;
            }
            else if (suitSkin != null)
            {
              newTexture = isEva ? suitSkin.evaHelmet : suitSkin.helmet;
              newNormalMap = suitSkin.helmetNRM;
            }
            break;
          }
          case "visor":
          {
            if (isAtmSuit)
            {
              smr.sharedMesh = null;
            }
            else
            {
              // Visor texture must be set every time, because the replacement on proto-IVA Kerbal
              // doesn't seem to work.
              KerbalSuit skin = suitSkin ?? defaultSuit;
              newTexture = isEva ? skin.evaVisor : skin.visor;

              if (newTexture != null)
                smr.material.color = Color.white;
            }
            break;
          }
          default: // Jetpack.
          {
            if (isAtmSuit)
            {
              smr.sharedMesh = null;
            }
            else if (suitSkin != null)
            {
              newTexture = suitSkin.evaJetpack;
              newNormalMap = suitSkin.evaJetpackNRM;
            }
            break;
          }
        }

        if (newTexture != null && newTexture != smr.material.mainTexture)
          smr.material.mainTexture = newTexture;

        if (newNormalMap != null && newNormalMap != smr.material.GetTexture("_BumpMap"))
          smr.material.SetTexture("_BumpMap", newNormalMap);
      }
    }

    /**
     * Set custom and random Kerbals' textures.
     */
    private void replaceKerbalSkins()
    {
      // IVA textures must be replaced with a little (2 frame) lag, otherwise we risk race
      // conditions with KSP handler that resets IVA suits to the stock ones. The race condition
      // issue always occurs when boarding an external seat.
      if (ivaReplaceCounter == 0)
      {
        Kerbal[] kerbals = isSfrDetected ? (Kerbal[]) Kerbal.FindObjectsOfType(typeof(Kerbal)) :
                           InternalSpace.Instance == null ? null :
                           InternalSpace.Instance.GetComponentsInChildren<Kerbal>();

        if (kerbals != null)
        {
          foreach (Kerbal kerbal in kerbals)
            replaceKerbalSkin(kerbal, kerbal.protoCrewMember, false, false);
        }

        ivaReplaceCounter = -1;
      }

      if (kerbalVessels.Count != 0)
      {
        foreach (Vessel vessel in kerbalVessels)
        {
          if (vessel == null || !vessel.loaded || vessel.vesselName == null)
            continue;

          double atmPressure = FlightGlobals.getStaticPressure();
          // Workaround for a KSP bug that reports `FlightGlobals.getStaticPressure() == 1.0` and
          // `vessel.staticPressure == 0.0` whenever a Kerbal leaves an external seat. But we don't
          // need to personalise textures for Kerbals that leave a seat anyway.
          if (atmPressure == 1.0 && vessel.staticPressure == 0.0)
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
              replaceKerbalSkin(eva, crew[0], true, isAtmSuit);
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
                replaceKerbalSkin(seat.Occupant, crew[0], true, isAtmSuit);
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
     * Fill config for custom Kerbal heads and suits.
     */
    private void readConfig()
    {
      ConfigNode rootNode = ConfigNode.Load(TextureReplacer.PATH + "/Kerbals.cfg");
      if (rootNode == null)
        return;

      rootNode = rootNode.GetNode("TextureReplacer");
      if (rootNode == null)
        return;

      ConfigNode customNode = rootNode.GetNode("CustomKerbals");
      if (customNode != null)
      {
        foreach (ConfigNode.Value entry in customNode.values)
        {
          string[] tokens = TextureReplacer.splitConfigValue(entry.value);

          if (tokens.Length >= 1)
          {
            string headName = tokens[0];

            Texture2D headTex = heads.FirstOrDefault(h => h.name.EndsWith(headName));
            if (headTex != null && !customHeads.ContainsKey(entry.name))
            {
              customHeads.Add(entry.name, headTex);
              log("Mapped {0}'s head -> {1}", entry.name, headTex.name);
            }
          }

          if (tokens.Length >= 2)
          {
            string suitName = tokens[1];

            KerbalSuit suitSkin = suitName == "DEFAULT" ? defaultSuit :
                                  suits.FirstOrDefault(s => s.name.EndsWith(suitName));
            if (suitSkin != null && !customSuits.ContainsKey(entry.name))
            {
              customSuits.Add(entry.name, suitSkin);
              log("Mapped {0}'s suit -> {1}", entry.name, suitSkin.name);
            }
          }
        }
      }

      ConfigNode genericNode = rootNode.GetNode("GenericKerbals");
      if (genericNode != null)
      {
        string sExcludedHeads = genericNode.GetValue("excludedHeads");
        if (sExcludedHeads != null)
        {
          string[] excludedHeads = TextureReplacer.splitConfigValue(sExcludedHeads);
          foreach (string headName in excludedHeads)
            heads.RemoveAll(h => h.name.EndsWith(headName));
        }

        string sExcludedSuits = genericNode.GetValue("excludedSuits");
        if (sExcludedSuits != null)
        {
          string[] excludedSuits = TextureReplacer.splitConfigValue(sExcludedSuits);
          foreach (string suitName in excludedSuits)
            suits.RemoveAll(s => s.name.EndsWith(suitName));
        }

        string sSuitAssignment = genericNode.GetValue("suitAssignment");
        if (sSuitAssignment != null)
        {
          if (sSuitAssignment == "random")
            suitAssignment = SuitAssignment.RANDOM;
          else if (sSuitAssignment == "consecutive")
            suitAssignment = SuitAssignment.CONSECUTIVE;
          else
            log("Invalid value for suitAssignment: {0}", sSuitAssignment);
        }
      }
    }

    /**
     * Read configuration and perform pre-load initialisation.
     */
    public Personaliser(ConfigNode rootNode)
    {
      string sIsAtmSuitEnabled = rootNode.GetValue("isAtmSuitEnabled");
      if (sIsAtmSuitEnabled != null)
        Boolean.TryParse(sIsAtmSuitEnabled, out isAtmSuitEnabled);

      string sAtmSuitPressure = rootNode.GetValue("atmSuitPressure");
      if (sAtmSuitPressure != null)
        Double.TryParse(sAtmSuitPressure, out atmSuitPressure);

      // Check if the srf mod is present.
      isSfrDetected = AssemblyLoader.loadedAssemblies.Any(a => a.name.StartsWith("sfrPartModules"));
      if (isSfrDetected)
        log("Detected sfr mod, enabling alternative Kerbal IVA texture replacement");

      // Update IVA textures on vessel switch.
      GameEvents.onVesselChange.Add(delegate(Vessel v) {
        if (!v.isEVA)
          ivaReplaceCounter = 2;
      });

      // Update IVA textures when a new Kerbal enters. This should be unnecessary but we do it just
      // in case that some plugin (e.g. Crew Manifest) moves Kerbals across the vessel. Even when it
      // is unnecessary it doesn't hurt performance since vessel switch occurs within the same
      // frame, so both events trigger only one texture replacement pass.
      GameEvents.onCrewBoardVessel.Add(delegate {
        ivaReplaceCounter = 2;
      });

      // Update IVA textures on docking.
      GameEvents.onVesselWasModified.Add(delegate(Vessel v) {
        if (v.vesselName != null)
          ivaReplaceCounter = 2;
      });

      // Update EVA textures when a new Kerbal is created.
      GameEvents.onVesselCreate.Add(delegate(Vessel v) {
        kerbalVessels.Add(v);
      });

      // Update EVA textures when a Kerbal comes into 2.4 km range.
      GameEvents.onVesselLoaded.Add(delegate(Vessel v) {
        kerbalVessels.Add(v);
      });
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      Dictionary<string, int> genericDirs = new Dictionary<string, int>();
      string lastTextureName = "";

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(TextureReplacer.DIR))
          continue;

        int lastSlash = texture.name.LastIndexOf('/');
        string originalName = texture.name.Substring(lastSlash + 1);

        // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
        // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides
        // the preceding texture.
        if (texture.name == lastTextureName)
        {
          log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
        }
        // Add a head texture.
        else if (texture.name.StartsWith(DIR_HEADS))
        {
          texture.wrapMode = TextureWrapMode.Clamp;

          heads.Add(texture);
          log("Mapped head #{0} -> {1}", heads.Count - 1, texture.name);
        }
        // Add a suit texture.
        else if (texture.name.StartsWith(DIR_SUITS))
        {
          int index = suits.Count;
          int dirNameLength = lastSlash - DIR_HEADS.Length;
          string dirName = texture.name.Substring(DIR_SUITS.Length, dirNameLength);

          texture.wrapMode = TextureWrapMode.Clamp;

          KerbalSuit suit = null;
          if (genericDirs.ContainsKey(dirName))
          {
            index = genericDirs[dirName];
            suit = suits[index];
          }
          else
          {
            genericDirs.Add(dirName, index);

            index = suits.Count;
            suit = new KerbalSuit { name = dirName };
            suits.Add(suit);
          }

          if (suit.setTexture(originalName, texture))
            log("Mapped suit #{0}'s {1} -> {2}", suits.Count - 1, originalName, texture.name);
          else
            log("Unknown suit texture name {0}", texture.name);
        }
        else if (texture.name.StartsWith(DIR_DEFAULT))
        {
          if (defaultSuit.setTexture(originalName, texture))
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        lastTextureName = texture.name;
      }

      readConfig();
    }

    public void resetScene()
    {
      ivaReplaceCounter = -1;
    }

    public void updateScene()
    {
      // IVA/EVA texture replacement pass. It is scheduled via event callbacks.
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (ivaReplaceCounter == 0 || kerbalVessels.Count != 0)
          replaceKerbalSkins();

        if (ivaReplaceCounter > 0)
          --ivaReplaceCounter;
      }
    }
  }
}
