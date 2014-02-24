/*
 * Copyright © 2014 Davorin Učakar
 * Copyright © 2013 Ryan Bray
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

[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class TextureReplacer : MonoBehaviour
{
  private class KerbalSuit
  {
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
  };

  private static readonly string DIR_PREFIX = "TextureReplacer/";
  private static readonly string DIR_CUSTOM_KERBALS = DIR_PREFIX + "CustomKerbals/";
  private static readonly string DIR_GENERIC_KERBALS = DIR_PREFIX + "GenericKerbals/";
  private static readonly string DIR_GENERIC_KERMINS = DIR_PREFIX + "GenericKermins/";
  // General texture replacements.
  private Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
  // Personalised Kerbal textures.
  private Dictionary<string, Texture2D> customHeads = new Dictionary<string, Texture2D>();
  private Dictionary<string, KerbalSuit> customSuits = new Dictionary<string, KerbalSuit>();
  // Generic Kerbal textures (male on the beginning of the list, female on the end).
  private List<Texture2D> genericHeads = new List<Texture2D>();
  private List<KerbalSuit> genericSuits = new List<KerbalSuit>();
  private KerbalSuit defaultSkin = new KerbalSuit();
  // Indices of the first female face and suit.
  private int firstKerminHead = 0;
  private int firstKerminSuit = 0;
  // Atmospheric IVA suit parameters.
  private double atmSuitPressure = 0.5;
  private bool isAtmSuitEnabled = true;
  // List of vessels for which Kerbal EVA has to be updated (either vessel is an EVA or has an EVA
  // on an external seat).
  private List<Vessel> kerbalVessels = new List<Vessel>();
  // Update counter for IVA replacement. It has to scheduled with a few frame lag to avoid race
  // conditions with stock IVA texture replacement that sets orange suits to Jeb, Bill and Bob and
  // grey suits to other Kerbals.
  private int ivaReplaceCounter = -1;
  // Generic texture replacement parameters.
  private GameScenes lastScene = GameScenes.LOADING;
  private int lastMaterialCount = 0;
  // General replacement has to be performed for more than one frame when a scene switch occurs
  // since textures and models may also be loaded with a few frame lag. `updateCounter` specifies
  // for how many frames it should run.
  private int updateCounter = 0;
  // Texture compression and mipmap generation parameters.
  private int lastTextureCount = 0;
  private int memorySpared = 0;
  // List of substrings for paths where mipmap generating is enabled.
  private string[] mipmapDirSubstrings = null;
  // Features.
  private bool isSfrDetected = false;
  private bool isCompressionEnabled = true;
  private bool isMipmapGenEnabled = true;
  private bool isInitialised = false;

  /**
   * Print a log entry for TextureReplacer. `String.Format()`-style formatting is supported.
   */
  private static void log(string s, params object[] args)
  {
    Debug.Log("[TextureReplacer] " + String.Format(s, args));
  }

  /**
   * True iff `i` is a power of two.
   */
  private static bool isPow2(int i)
  {
    return i > 0 && (i & (i - 1)) == 0;
  }

  /**
   * Read configuration file, check which features are enabled.
   */
  private void readConfig()
  {
    string configPath = KSP.IO.IOUtils.GetFilePathFor(GetType(), "Config.cfg");
    ConfigNode config = ConfigNode.Load(configPath);
    if (config == null)
      return;

    config = config.GetNode("TextureReplacer");
    if (config == null)
      return;

    string sIsCompressionEnabled = config.GetValue("isCompressionEnabled");
    if (sIsCompressionEnabled != null)
      Boolean.TryParse(sIsCompressionEnabled, out isCompressionEnabled);

    string sIsMipmapGenEnabled = config.GetValue("isMipmapGenEnabled");
    if (sIsMipmapGenEnabled != null)
      Boolean.TryParse(sIsMipmapGenEnabled, out isMipmapGenEnabled);

    string sIsAtmSuitEnabled = config.GetValue("isAtmSuitEnabled");
    if (sIsAtmSuitEnabled != null)
      Boolean.TryParse(sIsAtmSuitEnabled, out isAtmSuitEnabled);

    string sAtmSuitPressure = config.GetValue("atmSuitPressure");
    if (sAtmSuitPressure != null)
      Double.TryParse(sAtmSuitPressure, out atmSuitPressure);

    string sMipmapDirSubstrings = config.GetValue("mipmapDirSubstrings");
    if (sMipmapDirSubstrings != null)
    {
      mipmapDirSubstrings = sMipmapDirSubstrings.Split(new char[] { ' ', ',' },
                                                       StringSplitOptions.RemoveEmptyEntries);
    }
  }

  /**
   * Estimate texture size in RAM.
   *
   * This is only a rough estimate. It doesn't bother with details like the padding bytes or exact
   * mipmap size calculation.
   */
  private static int textureSize(Texture2D texture)
  {
    int nPixels = texture.width * texture.height;
    int size = texture.format == TextureFormat.DXT1 ? nPixels * 4 / 6 :
               texture.format == TextureFormat.DXT5 ? nPixels * 4 / 4 :
               texture.format == TextureFormat.Alpha8 ? nPixels * 1 :
               texture.format == TextureFormat.RGB24 ? nPixels * 3 : nPixels * 4;

    // Is this correct? Does Unity even store mipmaps in RAM?
    if (texture.mipmapCount != 1)
      size += size / 3;

    return size;
  }

  /**
   * Texture compression & mipmap generation pass.
   *
   * This is run on each game update until game database is loaded.
   */
  private void processTextures()
  {
    List<GameDatabase.TextureInfo> texInfos = GameDatabase.Instance.databaseTexture;

    for (int i = lastTextureCount; i < texInfos.Count; ++i)
    {
      Texture2D texture = texInfos[i].texture;
      TextureFormat format = texture.format;

      // Set trilinear filter.
      texture.filterMode = FilterMode.Trilinear;

      // `texture.GetPixel() throws an exception if the texture is not readable and hence it cannot
      // be compressed nor mipmaps generated.
      try
      {
        texture.GetPixel(0, 0);
      }
      catch (UnityException)
      {
        continue;
      }

      // Generate mipmaps if necessary. Images that may be UI icons should be excluded to prevent
      // blurriness when using less-than-full texture quality.
      if (isMipmapGenEnabled && texture.mipmapCount == 1 && (texture.width | texture.height) != 1
          && mipmapDirSubstrings != null
          && mipmapDirSubstrings.Any(s => texture.name.IndexOf(s) >= 0))
      {
        int oldSize = textureSize(texture);
        bool isTransparent = false;
        Color32[] pixels32 = texture.GetPixels32();

        // PNGs and JPEGs are always loaded as transparent, so we check if they actually contain any
        // transparent pixels. If not, they are converted to DXT1.
        if (texture.format == TextureFormat.RGBA32 || texture.format == TextureFormat.DXT5)
          isTransparent = pixels32.Any(p => p.a != 255);

        // Rebuild texture. This time with mipmaps.
        TextureFormat newFormat = isTransparent ? TextureFormat.RGBA32 : TextureFormat.RGB24;

        texture.Resize(texture.width, texture.height, newFormat, true);
        texture.SetPixels32(pixels32);
        texture.Apply(true, false);

        int newSize = textureSize(texture);
        memorySpared += oldSize - newSize;

        log("Generated mipmaps for {0} [{1}x{2} {3} -> {4}]",
            texture.name, texture.width, texture.height, format, texture.format);

        format = texture.format;
      }

      // Compress if necessary.
      if (isCompressionEnabled && format != TextureFormat.DXT1 && format != TextureFormat.DXT5)
      {
        if (!isPow2(texture.width) || !isPow2(texture.height))
        {
          log("Failed to compress {0}, dimensions {1}x{2} are not powers of 2",
              texture.name, texture.width, texture.height);
        }
        else
        {
          int oldSize = textureSize(texture);

          texture.Compress(true);

          int newSize = textureSize(texture);
          memorySpared += oldSize - newSize;

          log("Compressed {0} [{1}x{2} {3} -> {4}]",
              texture.name, texture.width, texture.height, format, texture.format);
        }
      }
    }

    lastTextureCount = texInfos.Count;
  }

  /**
   * Initialisation for textures replacement.
   */
  private void initialiseReplacer()
  {
    List<KerbalSuit> kerminSuits = new List<KerbalSuit>();
    Dictionary<string, int>[] genericDirs = new Dictionary<string, int>[2] {
      new Dictionary<string, int>(), new Dictionary<string, int>()
    };
    string lastTextureName = "";

    foreach (GameDatabase.TextureInfo texInfo
             in GameDatabase.Instance.databaseTexture.FindAll(ti => ti.name.StartsWith(DIR_PREFIX)))
    {
      Texture2D texture = texInfo.texture;
      if (texture == null)
        continue;

      // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
      // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides the
      // preceding texture.
      if (texture.name == lastTextureName)
      {
        log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
      }
      // Add a presonalised Kerbal texture.
      else if (texture.name.StartsWith(DIR_CUSTOM_KERBALS))
      {
        int lastSlash = texture.name.LastIndexOf('/');
        int kerbalNameLength = lastSlash - DIR_CUSTOM_KERBALS.Length;
        string originalName = texture.name.Substring(lastSlash + 1);
        string kerbalName = texture.name.Substring(DIR_CUSTOM_KERBALS.Length, kerbalNameLength);

        if (originalName == "kerbalHead")
        {
          if (!customHeads.ContainsKey(kerbalName))
          {
            customHeads.Add(kerbalName, texture);

            log("Mapped {0}'s {1} -> {2}", kerbalName, originalName, texture.name);
          }
        }
        else
        {
          // If the suit entry already exists, add the new texture to it, otherwise create a new
          // suit entry.
          KerbalSuit suit = null;
          customSuits.TryGetValue(kerbalName, out suit);

          if (suit == null)
          {
            suit = new KerbalSuit();
            customSuits.Add(kerbalName, suit);
          }

          if (suit.setTexture(originalName, texture))
            log("Mapped {0}'s {1} -> {2}", kerbalName, originalName, texture.name);
          else
            log("Unknown Kerbal texture {0}", texture.name);
        }
      }
      // Add a generic Kerbal/Kermin texture.
      else if (texture.name.StartsWith(DIR_GENERIC_KERBALS)
               || texture.name.StartsWith(DIR_GENERIC_KERMINS))
      {
        bool isFemale = texture.name.StartsWith(DIR_GENERIC_KERMINS);
        int gender = isFemale ? 1 : 0;
        string baseDir = isFemale ? DIR_GENERIC_KERMINS : DIR_GENERIC_KERBALS;
        int lastSlash = texture.name.LastIndexOf('/');
        int dirNameLength = lastSlash - baseDir.Length;
        string originalName = texture.name.Substring(lastSlash + 1);

        if (originalName.StartsWith("kerbalHead"))
        {
          // Male heads go to the beginning, female to the end of the list.
          int index = isFemale ? genericHeads.Count : firstKerminHead;
          genericHeads.Insert(index, texture);

          if (!isFemale)
            ++firstKerminHead;

          log("Mapped generic {0} head #{1} kerbalHead -> {2}",
              isFemale ? "Kermin" : "Kerbal", isFemale ? index - firstKerminHead : index,
              texture.name);
        }
        else if (dirNameLength > 0)
        {
          KerbalSuit suit = new KerbalSuit();
          string dirName = texture.name.Substring(baseDir.Length, dirNameLength);
          int index;

          // We use a special list for female suits: `kerminSuits`. At the end of initialisation it
          // will be appended to `genericSuits`.
          if (genericDirs[gender].ContainsKey(dirName))
          {
            index = genericDirs[gender][dirName];
            suit = isFemale ? kerminSuits[index] : genericSuits[index];
          }
          else
          {
            index = isFemale ? kerminSuits.Count : genericSuits.Count;
            genericDirs[gender].Add(dirName, index);

            if (isFemale)
              kerminSuits.Add(suit);
            else
              genericSuits.Add(suit);
          }

          if (suit.setTexture(originalName, texture))
            log("Mapped generic {0} suit #{1} {2} -> {3}",
                isFemale ? "Kermin" : "Kerbal", index, originalName, texture.name);
          else
            log("Unknown Kerbal texture {0}", texture.name);
        }
      }
      // Add a general texture replacement.
      else
      {
        int lastSlash = texture.name.LastIndexOf('/');
        string originalName = texture.name.Substring(lastSlash + 1);

        // This in wrapped inside an 'if' clause just in case if corrupted GameDatabase contains
        // non-consecutive duplicated entries for some strange reason.
        if (!mappedTextures.ContainsKey(originalName))
        {
          log("Mapped {0} -> {1}", originalName, texture.name);

          mappedTextures.Add(originalName, texture);
          defaultSkin.setTexture(originalName, texture);
        }
      }

      lastTextureName = texture.name;
    }

    // Append female suits to the generic suits list.
    firstKerminSuit = genericSuits.Count;
    genericSuits.AddRange(kerminSuits);

    // Replace textures (and apply trilinear filter). This doesn't reach some textures like skybox
    // and kerbalMainGrey. Those will be replaced later.
    replaceTextures((Material[]) Resources.FindObjectsOfTypeAll(typeof(Material)));

    bool hasIvaVisor = mappedTextures.ContainsKey("kerbalVisor");
    bool hasEvaVisor = mappedTextures.ContainsKey("EVAvisor");

    // Replace visor colour on proto-IVA and -EVA Kerbal.
    if (hasIvaVisor || hasEvaVisor)
    {
      foreach (SkinnedMeshRenderer smr
               in Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)))
      {
        if (smr.name != "visor")
          continue;

        if (smr.transform.parent.parent.parent.parent == null)
        {
          if (hasEvaVisor)
          {
            smr.sharedMaterial.mainTexture = mappedTextures["EVAvisor"];
            smr.sharedMaterial.color = Color.white;
          }
        }
        else
        {
          if (hasIvaVisor)
          {
            smr.sharedMaterial.mainTexture = mappedTextures["kerbalVisor"];
            smr.sharedMaterial.color = Color.white;
          }
        }
      }
    }
  }

  /**
   * General texture replacement step.
   */
  private void replaceTextures(Material[] materials)
  {
    foreach (Material material in materials)
    {
      Texture texture = material.mainTexture;
      if (texture == null || texture.name.Length == 0 || texture.name.StartsWith("Temp"))
        continue;

      Texture2D newTexture = null;
      mappedTextures.TryGetValue(texture.name, out newTexture);

      if (newTexture == null)
      {
        // Set trilinear filter. Trilinear filter is also set in initialisation but it only iterates
        // through textures in `GameData/`.
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        continue;
      }
      else if (newTexture != texture)
      {
        material.mainTexture = newTexture;
        Resources.UnloadAsset(texture);
      }

      Texture normalMap = material.GetTexture("_BumpMap");
      if (normalMap == null)
        continue;

      Texture2D newNormalMap = null;
      mappedTextures.TryGetValue(normalMap.name, out newNormalMap);

      if (newNormalMap != null && newNormalMap != normalMap)
      {
        material.SetTexture("_BumpMap", newNormalMap);
        Resources.UnloadAsset(normalMap);
      }
    }
  }

  /**
   * Replace Kerbal textures.
   *
   * This is a helper method for `replaceKerbalSkins()`. It sets personalised or random textures for
   * an IVA or an EVA Kerbal.
   */
  private void replaceKerbalSkin(Component component, string name, bool isEva, bool isAtmSuit)
  {
    Texture2D headTexture = null;
    KerbalSuit suitSkin = null;
    int hash = name.GetHashCode();
    bool isFemale = false;

    customHeads.TryGetValue(name, out headTexture);
    customSuits.TryGetValue(name, out suitSkin);

    if (headTexture == null && genericHeads.Count != 0)
    {
      // Hash is multiplied with a large prime to increase randomisation, since hashes returned by
      // `GetHashCode()` are close together if strings only differ in the last (few) char(s).
      int index = ((hash * 1021) & 0x7fffffff) % genericHeads.Count;

      isFemale = index >= firstKerminHead;
      headTexture = genericHeads[index];
    }

    if (suitSkin == null && genericSuits.Count != 0)
    {
      int firstSuit = isFemale ? firstKerminSuit : 0;
      int nSuits = isFemale ? genericSuits.Count - firstKerminSuit : firstKerminSuit;

      if (nSuits != 0)
      {
        // Here we must use a different prime to increase randomisation so that the same head is not
        // always combined with the same suit.
        suitSkin = genericSuits[firstSuit + ((hash * 2053) & 0x7fffffff) % nSuits];
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

          // This required to fix IVA suits after KSP resetting them to the stock ones all the time.
          // If there is the default replacement for IVA suit texture and the current Kerbal skin
          // contains no IVA suit, we must set it to the default replacement, otherwise the stock
          // one will be used.
          if (!isEvaSuit && newTexture == null)
            newTexture = defaultSkin.suit;

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
            KerbalSuit skin = suitSkin ?? defaultSkin;
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
   * Set personalised and random Kerbals' textures.
   */
  private void replaceKerbalSkins()
  {
    // IVA textures must be replaced with a little (2 frame) lag, otherwise we risk race conditions
    // with KSP handler that resets IVA suits to the stock ones. The race condition issue always
    // occurs when boarding an external seat.
    if (ivaReplaceCounter == 0)
    {
      Kerbal[] kerbals = isSfrDetected ? (Kerbal[]) Kerbal.FindObjectsOfType(typeof(Kerbal)) :
                                         InternalSpace.Instance.GetComponentsInChildren<Kerbal>();

      foreach (Kerbal kerbal in kerbals)
        replaceKerbalSkin(kerbal, kerbal.name, false, false);

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
            replaceKerbalSkin(eva, crew[0].name, true, isAtmSuit);
        }
        else
        {
          // Vessel is a ship. Update Kerbals on external seats.
          foreach (Part part in vessel.rootPart.FindChildParts<Part>())
          {
            KerbalSeat seat = part.GetComponent<KerbalSeat>();
            if (seat == null || seat.Occupant == null)
              continue;

            List<ProtoCrewMember> crew = seat.Occupant.protoModuleCrew;
            if (crew.Count != 0)
              replaceKerbalSkin(seat.Occupant, crew[0].name, true, isAtmSuit);
          }
        }
      }

      kerbalVessels.Clear();
      // Prevent list capacity from growing too much.
      if (kerbalVessels.Capacity > 16)
        kerbalVessels.TrimExcess();
    }
  }

  public void Start()
  {
    DontDestroyOnLoad(this);

    readConfig();

    foreach (AssemblyLoader.LoadedAssembly assembly in AssemblyLoader.loadedAssemblies)
    {
      // Prevent conflicts with TextureCompressor. If it is found among loaded plugins, texture
      // compression step will be skipped since TextureCompressor should handle it (better).
      if (assembly.name == "TextureCompressor")
      {
        log("Detected TextureCompressor, disabling texture compression and mipmap generation");
        isCompressionEnabled = false;
        isMipmapGenEnabled = false;
      }
      // Use the brute-force approach for Kerbal IVA texture replacement because the standard
      // approach doesn't work with the sfr pods.
      else if (assembly.name.StartsWith("sfrPartModules"))
      {
        log("Detected sfr mod, enabling alternative Kerbal IVA texture replacement");
        isSfrDetected = true;
      }
    }

    // Update IVA textures on vessel switch.
    GameEvents.onVesselChange.Add(delegate(Vessel v) {
      if (!v.isEVA)
        ivaReplaceCounter = 2;
    });

    // Update IVA textures when a new Kerbal enters. This should be unnecessary but we do it just in
    // case that some plugin (e.g. Crew Manifest) moves Kerbals across the vessel. Even when it is
    // unnecessary it doesn't hurt performance since vessel switch occurs within the same frame, so
    // both events trigger only one texture replacement pass.
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

  public void LateUpdate()
  {
    if (!isInitialised)
    {
      // Compress textures, generate mipmaps, convert DXT5 -> DXT1 if necessary etc.
      processTextures();

      if (GameDatabase.Instance.IsReady())
      {
        if (memorySpared > 0)
        {
          log("Texture compression spared {0:0.0} MiB = {1:0.0} MB",
              memorySpared / 1024.0 / 1024.0, memorySpared / 1000.0 / 1000.0);
        }

        initialiseReplacer();
        isInitialised = true;
      }
    }
    else
    {
      // Schedule general texture replacement pass at the beginning of each scene. Textures are
      // still loaded several frames after scene switch so this pass must be repeated multiple
      // times. Especially problematic is the main menu that resets skybox texture twice, second
      // time being several tens of frames after the load (depending on frame rate).
      if (HighLogic.LoadedScene != lastScene)
      {
        lastScene = HighLogic.LoadedScene;
        lastMaterialCount = 0;
        ivaReplaceCounter = -1;
        updateCounter = HighLogic.LoadedScene == GameScenes.MAINMENU ? 64 : 16;
      }

      // General texture replacement pass.
      if (updateCounter > 0)
      {
        --updateCounter;

        Material[] materials = (Material[]) Resources.FindObjectsOfTypeAll(typeof(Material));
        if (materials.Length != lastMaterialCount)
        {
          replaceTextures(materials);
          lastMaterialCount = materials.Length;
        }
      }

      // IVA/EVA texture replacement pass. It is scheduled via event callback defined in `Start()`.
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
