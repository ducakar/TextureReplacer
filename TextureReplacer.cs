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
using UnityEngine;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class TextureReplacer : MonoBehaviour
{
  private class KerbalSkin
  {
    public Texture2D suit;
    public Texture2D suitNRM;
    public Texture2D helmet;
    public Texture2D helmetNRM;
    public Color? visor;
    public Texture2D evaSuit;
    public Texture2D evaSuitNRM;
    public Texture2D evaHelmet;
    public Color? evaVisor;
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
          visor = texture.GetPixel(0, 0);
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
          evaVisor = texture.GetPixel(0, 0);
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
          log("Unknown kerbal texture name {0} [{1}]", originalName, texture.name);
          return false;
        }
      }
    }
  };

  private static readonly string DIR_PREFIX = "TextureReplacer/";
  private static readonly string DIR_CUSTOM_KERBALS = DIR_PREFIX + "CustomKerbals/";
  private static readonly string DIR_GENERIC_KERBALS = DIR_PREFIX + "GenericKerbals/";
  private Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
  private KerbalSkin defaultSkin = new KerbalSkin();
  private Dictionary<string, Texture2D> customHeads = new Dictionary<string, Texture2D>();
  private Dictionary<string, KerbalSkin> customSuits = new Dictionary<string, KerbalSkin>();
  private List<Texture2D> genericHeads = new List<Texture2D>();
  private List<KerbalSkin> genericSuits = new List<KerbalSkin>();
  private double atmSuitPressure = Double.PositiveInfinity;
  private bool isAtmSuitEnabled = false;
  private List<Vessel> kerbalVessels = new List<Vessel>();
  private GameScenes lastScene = GameScenes.LOADING;
  private int updateCounter = 0;
  private int lastMaterialCount = 0;
  private int ivaReplaceCounter = -1;
  private int lastTextureCount = 0;
  private int memorySpared = 0;
  private bool isCompressionEnabled = true;
  private bool isMipmapGenEnabled = true;
  private bool isInitialised = false;

  private static void log(string s, params object[] args)
  {
    Debug.Log("[TextureReplacer] " + String.Format(s, args));
  }

  private void readConfig()
  {
    string configPath = KSP.IO.IOUtils.GetFilePathFor(GetType(), "Config.cfg");
    ConfigNode config = ConfigNode.Load(configPath);
    if (config == null)
      return;

    config = config.GetNode("TextureReplacer");
    if (config == null)
      return;

    string sIsCompressionEnabled = config.GetValue("isCompresionEnabled");
    if (sIsCompressionEnabled != null)
      Boolean.TryParse(sIsCompressionEnabled, out isCompressionEnabled);

    string sIsMipmapGenEnabled = config.GetValue("isMipmapGenEnabled");
    if (sIsMipmapGenEnabled != null)
      Boolean.TryParse(sIsMipmapGenEnabled, out isMipmapGenEnabled);

    string sAtmSuitPressure = config.GetValue("atmSuitPressure");
    if (sAtmSuitPressure != null)
      Double.TryParse(sAtmSuitPressure, out atmSuitPressure);

    isAtmSuitEnabled = !Double.IsInfinity(atmSuitPressure);
  }

  /**
   * Estimate image size.
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

      // Generate mipmaps if neccessary. Images that may be UI icons should be excluded to prevent
      // blurrines when using less-than-full texture quality.
      if (isMipmapGenEnabled && texture.mipmapCount == 1 && (texture.width | texture.height) != 1
          && (texture.name.StartsWith(DIR_PREFIX)
          || texture.name.IndexOf("/FX/", StringComparison.OrdinalIgnoreCase) >= 0
          || texture.name.IndexOf("/Parts/", StringComparison.OrdinalIgnoreCase) >= 0
          || texture.name.IndexOf("/Spaces/", StringComparison.OrdinalIgnoreCase) >= 0))
      {
        int oldSize = textureSize(texture);
        bool isTransparent = false;
        Color32[] pixels32 = null;

        // PNGs and JPEGs are always loaded as transparent, so we check if they actually contain any
        // tranpsarent pixels. If not, they are converted to DXT1.
        if (format == TextureFormat.RGBA32 || format == TextureFormat.DXT5)
        {
          pixels32 = texture.GetPixels32();

          foreach (Color32 pixel in pixels32)
          {
            if (pixel.a != 255)
            {
              isTransparent = true;
              break;
            }
          }
        }

        if (isTransparent)
        {
          texture.Resize(texture.width, texture.height, TextureFormat.RGBA32, true);
          texture.SetPixels32(pixels32);
        }
        else
        {
          Color[] pixels24 = texture.GetPixels();

          texture.Resize(texture.width, texture.height, TextureFormat.RGB24, true);
          texture.SetPixels(pixels24);
        }

        texture.Apply(true, false);
        texture.Compress(true);

        int newSize = textureSize(texture);
        memorySpared += oldSize - newSize;

        log("Generated mipmaps for {0} [{1}x{2} {3} -> {4}]",
            texture.name, texture.width, texture.height, format, texture.format);
      }
      // Compress if neccessary.
      else if (isCompressionEnabled && format != TextureFormat.DXT1 && format != TextureFormat.DXT5)
      {
        int oldSize = textureSize(texture);

        texture.Compress(true);

        int newSize = textureSize(texture);
        memorySpared += oldSize - newSize;

        log("Compressed {0} [{1}x{2} {3} -> {4}]",
            texture.name, texture.width, texture.height, format, texture.format);
      }
    }

    lastTextureCount = texInfos.Count;
  }

  /**
   * Initialisation for textures replacement.
   */
  private void initialiseReplacer()
  {
    Dictionary<string, int> genericDirs = new Dictionary<string, int>();
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
      else if (texture.name.StartsWith(DIR_CUSTOM_KERBALS))
      {
        int lastSlash = texture.name.LastIndexOf('/');
        int kerbalNameLength = lastSlash - DIR_CUSTOM_KERBALS.Length;
        string originalName = texture.name.Substring(lastSlash + 1);
        string kerbalName = texture.name.Substring(DIR_CUSTOM_KERBALS.Length, kerbalNameLength);

        if (originalName == "kerbalHead")
        {
          customHeads.Add(kerbalName, texture);

          log("Mapped {0}'s {1} -> {2}", kerbalName, originalName, texture.name);
        }
        else
        {
          KerbalSkin skin;

          if (customSuits.ContainsKey(kerbalName))
          {
            skin = customSuits[kerbalName];
          }
          else
          {
            skin = new KerbalSkin();
            customSuits.Add(kerbalName, skin);
          }

          if (skin.setTexture(originalName, texture))
            log("Mapped {0}'s {1} -> {2}", kerbalName, originalName, texture.name);
        }
      }
      else if (texture.name.StartsWith(DIR_GENERIC_KERBALS))
      {
        int lastSlash = texture.name.LastIndexOf('/');
        int dirNameLength = lastSlash - DIR_GENERIC_KERBALS.Length;
        string originalName = texture.name.Substring(lastSlash + 1);

        if (originalName.StartsWith("kerbalHead"))
        {
          genericHeads.Add(texture);

          log("Mapped generic head #{0} kerbalHead -> {1}", genericHeads.Count, texture.name);
        }
        else if (dirNameLength > 0)
        {
          string dirName = texture.name.Substring(DIR_GENERIC_KERBALS.Length, dirNameLength);
          int index = genericSuits.Count;
          KerbalSkin skin;

          if (genericDirs.ContainsKey(dirName))
          {
            index = genericDirs[dirName];
            skin = genericSuits[index];
          }
          else
          {
            genericDirs.Add(dirName, index);
            skin = new KerbalSkin();
            genericSuits.Add(skin);
          }

          if (skin.setTexture(originalName, texture))
            log("Mapped generic suit #{0} {1} -> {2}", index, originalName, texture.name);
        }
      }
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
            smr.sharedMaterial.color = mappedTextures["EVAvisor"].GetPixel(0, 0);
        }
        else
        {
          if (hasIvaVisor)
            smr.sharedMaterial.color = mappedTextures["kerbalVisor"].GetPixel(0, 0);
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

      if (!mappedTextures.ContainsKey(texture.name))
      {
        // Set trilinear filter. Trilinear filter is also set in initialisation but it only iterates
        // through textures in `GameData/`.
        if (texture.filterMode == FilterMode.Bilinear)
          texture.filterMode = FilterMode.Trilinear;

        continue;
      }

      Texture2D newTexture = mappedTextures[texture.name];
      if (newTexture != texture)
      {
        material.mainTexture = newTexture;
        Resources.UnloadAsset(texture);
      }

      Texture normalMap = material.GetTexture("_BumpMap");
      if (normalMap == null || !mappedTextures.ContainsKey(normalMap.name))
        continue;

      Texture2D newNormalMap = mappedTextures[normalMap.name];
      if (newNormalMap != normalMap)
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
  private void replaceKerbalSkin(Component component, string name, Vessel vessel)
  {
    Texture2D headTexture = null;
    KerbalSkin suitSkin = null;

    if (customHeads.ContainsKey(name))
    {
      headTexture = customHeads[name];
    }
    else if (genericHeads.Count != 0)
    {
      int hash = name.GetHashCode();
      headTexture = genericHeads[((hash * 1021) & 0x7fffffff) % genericHeads.Count];
    }

    if (customSuits.ContainsKey(name))
    {
      suitSkin = customSuits[name];
    }
    else if (genericSuits.Count != 0)
    {
      int hash = name.GetHashCode();
      suitSkin = genericSuits[(hash * 2053 & 0x7fffffff) % genericSuits.Count];
    }

    bool isEva = vessel != null;
    bool isAtmSuit = isAtmSuitEnabled && isEva && vessel.atmDensity > atmSuitPressure &&
                     vessel.mainBody.atmosphereContainsOxygen;

    foreach (SkinnedMeshRenderer smr in component.GetComponentsInChildren<SkinnedMeshRenderer>())
    {
      Material material = smr.material;
      Texture2D newTexture = null;
      Texture2D newNormalMap = null;

      switch (smr.name)
      {
        case "headMesh01":
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
          if (!isEvaSuit && newTexture == null)
          {
            if (!isEva && material.mainTexture.name == "kerbalMain")
            {
              if (mappedTextures.ContainsKey(material.mainTexture.name))
                newTexture = mappedTextures[material.mainTexture.name];
            }
            else
            {
              newTexture = defaultSkin.suit;
            }
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
            KerbalSkin skin = suitSkin ?? defaultSkin;
            Color? colour = isEva ? skin.evaVisor : skin.visor;

            if (colour.HasValue)
              smr.material.color = colour.Value;
          }
          break;
        }
        case "jetpack_base01":
        case "tank1":
        case "tank2":
        {
          if (suitSkin != null)
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
      foreach (Kerbal kerbal in InternalSpace.Instance.GetComponentsInChildren<Kerbal>())
        replaceKerbalSkin(kerbal, kerbal.name, null);

      ivaReplaceCounter = -1;
    }

    if (kerbalVessels.Count != 0)
    {
      foreach (Vessel vessel in kerbalVessels)
      {
        if (vessel == null || vessel.vesselName == null)
          continue;

        KerbalEVA eva = vessel.GetComponent<KerbalEVA>();
        if (eva == null)
          continue;

        replaceKerbalSkin(eva, vessel.vesselName, vessel);
      }

      kerbalVessels.Clear();
      // Prevent list capacity from growing too much.
      if (kerbalVessels.Capacity > 16)
        kerbalVessels.TrimExcess();
    }
  }

  protected void Start()
  {
    DontDestroyOnLoad(this);

    readConfig();

    // Prevent conficts with TextureCompressor. If it is found among loaded plugins, texture
    // compression step will be skipped since TextureCompressor should handle it (better).
    foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
    {
      if (go.name == "TextureCompressor")
      {
        log("Detected TextureCompressor, disabling texture compression");
        isCompressionEnabled = false;
        break;
      }
    }

    // Update IVA textures on vessel switch.
    GameEvents.onVesselChange.Add(delegate(Vessel v) {
      if (!v.isEVA)
        ivaReplaceCounter = 2;
      else if (isAtmSuitEnabled)
        kerbalVessels.Add(v);
    });

    // Update IVA textures when a new Kerbal enters. This should be unneccessary, but we do it
    // just in case that some plugin (e.g. Crew Manifest) moves Kerbals across the vessel. Even
    // when it is unneccessary it doesn't hurt performance since vessel switch occurs within the
    // same frame, so both events trigger only one texture replacement pass.
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
      if (v.isEVA)
        kerbalVessels.Add(v);
    });
  }

  protected void LateUpdate()
  {
    if (!isInitialised)
    {
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
      if (HighLogic.LoadedScene != lastScene)
      {
        lastScene = HighLogic.LoadedScene;
        lastMaterialCount = 0;
        ivaReplaceCounter = -1;
        updateCounter = HighLogic.LoadedScene == GameScenes.MAINMENU ? 64 : 16;
      }

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

      if (HighLogic.LoadedSceneIsFlight)
      {
        if (ivaReplaceCounter == 0 || kerbalVessels.Count != 0)
          replaceKerbalSkins();
        else if (ivaReplaceCounter > 0)
          --ivaReplaceCounter;
      }
    }
  }
}
