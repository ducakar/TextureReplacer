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

using UnityEngine;
using Gender = ProtoCrewMember.Gender;
using KerbalSuit = ProtoCrewMember.KerbalSuit;

namespace TextureReplacer
{
  internal class Personaliser
  {
    // Instance.
    public static Personaliser Instance { get; private set; }

    private Mapper mapper;

    public static void Recreate()
    {
      Instance = new Personaliser();
    }

    /// <summary>
    /// Post-load initialisation.
    /// </summary>
    public void Load()
    {
      mapper = Mapper.Instance;

      foreach (Kerbal kerbal in Resources.FindObjectsOfTypeAll<Kerbal>()) {
        // After na IVA space is initialised, suits are reset to these values. Replace stock textures with default ones.
        kerbal.textureStandard = mapper.DefaultSuit.IvaSuit[0];
        kerbal.textureVeteran = mapper.DefaultSuit.IvaSuitVeteran;
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
        PartLoader.getPartInfoByName("kerbalEVAfemaleVintage").partPrefab,
        PartLoader.getPartInfoByName("kerbalEVAFuture").partPrefab,
        PartLoader.getPartInfoByName("kerbalEVAfemaleFuture").partPrefab
      };

      foreach (Part eva in evas) {
        if (eva.GetComponent<TREvaModule>() == null) {
          eva.gameObject.AddComponent<TREvaModule>();
        }
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

    // Must be non-static or the event won't work.
    private void OnHelmetChanged(KerbalEVA eva, bool hasHelmet, bool hasNeckRing)
    {
      var evaModule = eva.GetComponent<TREvaModule>();
      if (evaModule) {
        evaModule.OnHelmetChanged(hasHelmet);
      }
    }

    /// <summary>
    /// Replace textures on a Kerbal model.
    /// </summary>
    private void PersonaliseKerbal(Component component, ProtoCrewMember kerbal, bool isEva, bool useEvaSuit)
    {
      Transform transform = component.transform;

      // Prefabricated Vintage & Future IVA models are missing so they are instantiated anew every time. Hence, we have
      // to apply fixes to them and set fallback default textures.
      bool isPrefabMissing = !isEva && kerbal.suit != KerbalSuit.Default;

      Appearance appearance = mapper.GetAppearance(kerbal);

      Skin skin = mapper.GetKerbalSkin(kerbal, appearance);
      Suit suit = mapper.GetKerbalSuit(kerbal, appearance);
      Skin defaultSkin = mapper.GetDefaultSkin(kerbal);
      Suit defaultSuit = mapper.GetDefaultSuit(kerbal);

      Transform modelTransform = (isEva, kerbal.suit, kerbal.gender) switch {
        (false, KerbalSuit.Default, _)            => transform.Find("model01"),
        (false, KerbalSuit.Vintage, _)            => transform.Find("kbIVA@idle/model01"),
        (false, KerbalSuit.Future, Gender.Male)   => transform.Find("serenityMaleIVA/model01"),
        (false, KerbalSuit.Future, Gender.Female) => transform.Find("serenityFemaleIVA/model01"),
        _                                         => transform.Find("model01")
      };

      // Sometimes when we switch between suits (e.g. with clothes hanger) suit kind and model get out of sync.
      // Just try all possible nodes in such cases.
      modelTransform ??= transform.Find("model01") ?? transform.Find("kbIVA@idle/model01") ??
                         transform.Find("serenityMaleIVA/model01") ?? transform.Find("serenityFemaleIVA/model01");

      if (isEva) {
        bool showJetpack = useEvaSuit;
        bool showBackpack = showJetpack && !mapper.HideBackpack;

        Transform flag = transform.Find("model/kbEVA_flagDecals");
        Transform cargo = transform.Find(kerbal.suit == KerbalSuit.Future
          ? "model/kerbalCargoContainerPack/base"
          : "model/EVABackpack/kerbalCargoContainerPack/base");
        Transform parachute = transform.Find("model/EVAparachute/base");

        flag.GetComponent<Renderer>().enabled = showJetpack;
        cargo.GetComponent<Renderer>().enabled = showBackpack;
        parachute.GetComponent<Renderer>().enabled = showBackpack;
      }

      // We determine body and helmet texture here to avoid code duplication between suit and helmet cases in the
      // following switch.
      // Setting the suit explicitly -- even when default -- is necessary for two reasons: to fix IVA suits after KSP
      // resetting them to the stock ones all the time and to fix the switch to default texture on start of EVA walk or
      // EVA suit toggle.
      Texture2D suitTexture = suit.GetSuit(useEvaSuit, kerbal) ?? defaultSuit.GetSuit(useEvaSuit, kerbal);
      Texture2D suitNormalMap = suit.GetSuitNRM(useEvaSuit) ?? defaultSuit.GetSuitNRM(useEvaSuit);

      // We must include hidden meshes, since flares are hidden when light is turned off.
      // All other meshes are always visible, so no performance hit here.
      foreach (Renderer renderer in modelTransform.GetComponentsInChildren<Renderer>(true)) {
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
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft": {
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.EyeballLeft;

            if (isPrefabMissing) {
              smr.material.shader = Replacer.EyeShader;
              newTexture ??= defaultSkin.EyeballLeft;
            }

            break;
          }
          case "eyeballRight":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight": {
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.EyeballRight;

            if (isPrefabMissing) {
              smr.material.shader = Replacer.EyeShader;
              newTexture ??= defaultSkin.EyeballRight;
            }

            break;
          }
          case "pupilLeft":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft": {
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.PupilLeft;

            if (isPrefabMissing) {
              smr.material.shader = Replacer.EyeShader;
              newTexture ??= defaultSkin.PupilLeft;
            }

            if (newTexture != null) {
              smr.material.color = Color.white;
            }

            break;
          }
          case "pupilRight":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight": {
            if (skin.IsEyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.PupilRight;

            if (isPrefabMissing) {
              newTexture ??= defaultSkin.PupilRight;
              smr.material.shader = Replacer.EyeShader;
            }

            if (newTexture != null) {
              smr.material.color = Color.white;
            }

            break;
          }
          case "headMesh01":
          case "headMesh02":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51": {
            newTexture = skin.Head;
            newNormalMap = skin.HeadNRM;

            if (isPrefabMissing) {
              newTexture ??= defaultSkin.Head;
              newNormalMap ??= defaultSkin.HeadNRM;
              smr.material.shader = newNormalMap == null ? Replacer.HeadShader : Replacer.BumpedHeadShader;
            } else if (newNormalMap != null) {
              smr.material.shader = Replacer.BumpedHeadShader;
            }

            break;
          }
          case "tongue":
          case "upTeeth01":
          case "upTeeth02":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01": {
            if (isPrefabMissing) {
              // We use male head texture for teeth, since female texture mapping would map their hair to teeth.
              newTexture = mapper.DefaultSkin[0].Head;
              newNormalMap = mapper.DefaultSkin[0].Head;
              smr.material.shader = newNormalMap == null ? Replacer.HeadShader : Replacer.BumpedHeadShader;
            }

            break;
          }
          case "body01":
          case "mesh_female_kerbalAstronaut01_body01": {
            newTexture = suitTexture;
            newNormalMap = suitNormalMap;

            // Update textures in Kerbal IVA object since KSP resets them to these values a few frames later.
            var kerbalIva = component as Kerbal;
            if (kerbalIva != null) {
              kerbalIva.textureStandard = newTexture;
              kerbalIva.textureVeteran = newTexture;
            }

            break;
          }
          case "neckRing": {
            newTexture = suitTexture;
            newNormalMap = suitNormalMap;
            break;
          }
          case "helmet":
          case "mesh_female_kerbalAstronaut01_helmet": {
            newTexture = suitTexture;
            newNormalMap = suitNormalMap;
            break;
          }
          case "visor":
          case "mesh_female_kerbalAstronaut01_visor": {
            // Visor texture has to be replaced every time.
            newTexture = suit.GetVisor(useEvaSuit);
            if (newTexture != null) {
              smr.material.color = Color.white;
            }

            break;
          }
          default: { // Jetpack.
            if (isEva) {
              smr.enabled = useEvaSuit;
              if (useEvaSuit) {
                newTexture = suit.EvaJetpack;
                newNormalMap = suit.EvaJetpackNRM;
              }
            }

            break;
          }
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
  }
}
