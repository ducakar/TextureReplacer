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

// headMesh01
// eyeballLeft, eyeballRight
// pupilLeft, pupilRight
// upTeeth01, upTeeth02, tongue
// headset_band01 (?)
//
[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class VegaTextureReplacer : MonoBehaviour
{
  private class KerbalSkin
  {
    public Texture2D head;
    public Texture2D suit;
    public Texture2D suitNRM;
    public Texture2D helmet;
    public Texture2D helmetNRM;
    public Texture2D evaSuit;
    public Texture2D evaSuitNRM;
    public Texture2D evaHelmet;
    public Texture2D evaJetpack;
    public Texture2D evaJetpackNRM;

    public bool setTexture(string originalName, Texture2D texture)
    {
      switch (originalName)
      {
        case "kerbalHead":
          head = texture;
          return true;
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
        case "EVAtexture":
          evaSuit = texture;
          return true;
        case "EVAtextureNRM":
          evaSuitNRM = texture;
          return true;
        case "EVAhelmet":
          evaHelmet = texture;
          return true;
        case "EVAjetpack":
          evaJetpack = texture;
          return true;
        case "EVAjetpackNRM":
          evaJetpackNRM = texture;
          return true;
        default:
          log("Unknown kerbal texture name {0} [{1}]", originalName, texture.name);
          return false;
      }
    }
  };

  private static readonly string DIR_PREFIX = "TextureReplacer/";
  private static readonly string DIR_CUSTOM_KERBALS = DIR_PREFIX + "CustomKerbals/";
  private static readonly string DIR_GENERIC_KERBALS = DIR_PREFIX + "GenericKerbals/";
  private Dictionary<string, Texture2D> mappedTextures = new Dictionary<string, Texture2D>();
  private Dictionary<string, KerbalSkin> customSkins = new Dictionary<string, KerbalSkin>();
  private List<KerbalSkin> genericHeadSkins = new List<KerbalSkin>();
  private List<KerbalSkin> genericSuitSkins = new List<KerbalSkin>();
  private GameScenes lastScene = GameScenes.LOADING;
  private int updateCounter = 0;
  private int lastMaterialCount = 0;
  private bool doReplaceIVASkins = false;
  private bool doReplaceEVASkins = false;
  private int lastTextureCount = 0;
  private int memorySpared = 0;
  private bool hasCompressor = false;
  private bool isInitialised = false;

  private static void log(string s, params object[] args)
  {
    Debug.Log("[TextureReplacer] " + String.Format(s, args));
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
      if (texture.mipmapCount == 1 && (texture.width | texture.height) != 1
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
      else if (!hasCompressor && format != TextureFormat.DXT1 && format != TextureFormat.DXT5)
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
   * Texture replacement step.
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
  private void replaceKerbalSkin(Component component, string name, bool isEva)
  {
    KerbalSkin headSkin = null;
    KerbalSkin suitSkin = null;

    if (customSkins.ContainsKey(name))
    {
      headSkin = customSkins[name];
      suitSkin = headSkin;
    }
    else if (genericHeadSkins.Count != 0 || genericSuitSkins.Count != 0)
    {
      int hash = name.GetHashCode();

      if (genericHeadSkins.Count != 0)
        headSkin = genericHeadSkins[((hash * 1021) & 0x7fffffff) % genericHeadSkins.Count];

      if (genericSuitSkins.Count != 0)
        suitSkin = genericSuitSkins[(hash * 2053 & 0x7fffffff) % genericSuitSkins.Count];
    }
    else
    {
      return;
    }

    foreach (SkinnedMeshRenderer smr in component.GetComponentsInChildren<SkinnedMeshRenderer>())
    {
      Material material = smr.material;
      if (material.mainTexture == null)
        continue;

      Texture2D newTexture = null;
      Texture2D newNormalMap = null;
      bool isSuit = false;

      if (headSkin != null && smr.name == "headMesh01")
      {
        newTexture = headSkin.head;
      }
      else if (suitSkin != null)
      {
        if (smr.name == "body01")
        {
          newTexture = isEva ? suitSkin.evaSuit : suitSkin.suit;
          newNormalMap = isEva ? suitSkin.evaSuitNRM : suitSkin.suitNRM;
          isSuit = true;
        }
        else if (smr.name == "helmet")
        {
          newTexture = isEva ? suitSkin.evaHelmet : suitSkin.helmet;
          newNormalMap = suitSkin.helmetNRM;
        }
        else if (material.name.StartsWith("jetpack"))
        {
          newTexture = suitSkin.evaJetpack;
          newNormalMap = suitSkin.evaJetpackNRM;
        }
      }
      else if (smr.name == "body01")
      {
        isSuit = true;
      }

      // This is required to fix IVA suits after KSP resetting them to the stock ones all the time.
      // If there is the default replacement for IVA suit texture and the current Kerbal skin
      // contains no IVA suit, we must set it to the default replacement, otherwise the stock one
      // will be used.
      if (isSuit && newTexture == null && mappedTextures.ContainsKey(material.mainTexture.name))
        newTexture = mappedTextures[material.mainTexture.name];

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
    if (doReplaceIVASkins)
    {
      foreach (Kerbal kerbal in Kerbal.FindObjectsOfType(typeof(Kerbal)))
        replaceKerbalSkin(kerbal, kerbal.name, false);
    }

    if (doReplaceEVASkins)
    {
      foreach (KerbalEVA eva in KerbalEVA.FindObjectsOfType(typeof(KerbalEVA)))
      {
        if (eva.vessel != null)
          replaceKerbalSkin(eva, eva.vessel.vesselName, true);
      }
    }
  }

  /**
   * Initialisation for textures replacement.
   */
  private void initialiseReplacer()
  {
    Dictionary<string, int> genericDirs = new Dictionary<string, int>();
    List<KerbalSkin> genericSkins = new List<KerbalSkin>();
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

        if (!customSkins.ContainsKey(kerbalName))
          customSkins.Add(kerbalName, new KerbalSkin());

        KerbalSkin skin = customSkins[kerbalName];

        if (skin.setTexture(originalName, texture))
          log("Mapping {0}'s {1} -> {2}", kerbalName, originalName, texture.name);
      }
      else if (texture.name.StartsWith(DIR_GENERIC_KERBALS))
      {
        int lastSlash = texture.name.LastIndexOf('/');
        int dirNameLength = lastSlash - DIR_GENERIC_KERBALS.Length;
        string originalName = texture.name.Substring(lastSlash + 1);

        KerbalSkin skin;
        int index = genericSkins.Count;

        if (originalName.StartsWith("kerbalHead"))
        {
          skin = new KerbalSkin();
          skin.head = texture;
          genericSkins.Add(skin);

          log("Mapping generic[{0}] kerbalHead -> {1}", index, texture.name);
        }
        else if (dirNameLength > 0)
        {
          string dirName = texture.name.Substring(DIR_GENERIC_KERBALS.Length, dirNameLength);

          if (genericDirs.ContainsKey(dirName))
          {
            index = genericDirs[dirName];
            skin = genericSkins[index];
          }
          else
          {
            genericDirs.Add(dirName, index);
            skin = new KerbalSkin();
            genericSkins.Add(skin);
          }

          if (skin.setTexture(originalName, texture))
            log("Mapping generic[{0}] {1} -> {2}", index, originalName, texture.name);
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
          log("Mapping {0} -> {1}", originalName, texture.name);
          mappedTextures.Add(originalName, texture);
        }
      }

      lastTextureName = texture.name;
    }

    foreach (KerbalSkin skin in genericSkins)
    {
      if (skin.head != null)
        genericHeadSkins.Add(skin);

      if (skin.suit != null || skin.helmet != null || skin.evaSuit != null || skin.evaHelmet != null
          || skin.evaJetpack != null)
        genericSuitSkins.Add(skin);
    }

    // Replace textures (and apply trilinear filter). This doesn't reach some textures like skybox
    // and kerbalMainGrey. Those will be replaced later.
    replaceTextures((Material[]) Resources.FindObjectsOfTypeAll(typeof(Material)));
  }

  protected void Start()
  {
    DontDestroyOnLoad(this);

    // Prevent conficts with TextureCompressor. If it is found among loaded plugins, texture
    // compression step will be skipped since TextureCompressor should handle it (better).
    foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
    {
      if (go.name == "TextureCompressor")
      {
        log("Detected TextureCompressor, disabling texture compression");
        hasCompressor = true;
        break;
      }
    }

    // Update IVA textures on vessel switch.
    GameEvents.onVesselChange.Add(delegate (Vessel v) {
      if (!v.isEVA)
        doReplaceIVASkins = true;
    });

    // Update IVA textures when a new Kerbal enters. This should be unneccessary, but we do it
    // just in case that some plugin (e.g. Crew Manifest) moves Kerbals across the vessel. Even
    // when it is unneccessary it doesn't hurt performance since vessel switch occurs within the
    // same frame, so both events trigger only one texture replacement pass.
    GameEvents.onCrewBoardVessel.Add(delegate {
      doReplaceIVASkins = true;
    });

    // Update IVA textures on docking.
    GameEvents.onVesselWasModified.Add(delegate (Vessel v) {
      if (v.vesselName != null)
        doReplaceIVASkins = true;
    });

    // Update EVA textures when a Kerbal exits a capsule.
    GameEvents.onCrewOnEva.Add(delegate {
      doReplaceEVASkins = true;
    });

    // Update EVA textures when a Kerbal comes into 2.4 km range.
    GameEvents.onVesselLoaded.Add(delegate (Vessel v) {
      if (v.isEVA)
        doReplaceEVASkins = true;
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

      if (HighLogic.LoadedSceneIsFlight && (doReplaceIVASkins || doReplaceEVASkins))
      {
        replaceKerbalSkins();

        doReplaceIVASkins = false;
        doReplaceEVASkins = false;
      }
    }
  }
}
