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

      // `TRIvaModelModule` makes sure that internal spaces personalise all Kerbals inside them on instantiation.
      // This will not suffice for Ship Manifest, we will also need to re-add these modules on any crew transfer.
      foreach (InternalModel model in Resources.FindObjectsOfTypeAll<InternalModel>()) {
        if (model.GetComponent<TRIvaModelModule>() == null) {
          model.gameObject.AddComponent<TRIvaModelModule>();
        }
      }

      var prefab = Prefab.Instance;

      EnsureEvaModule(prefab.MaleEva);
      EnsureEvaModule(prefab.FemaleEva);
      EnsureEvaModule(prefab.MaleEvaVintage);
      EnsureEvaModule(prefab.FemaleEvaVintage);
      EnsureEvaModule(prefab.MaleEvaFuture);
      EnsureEvaModule(prefab.FemaleEvaFuture);
    }

    public void OnBeginFlight()
    {
      GameEvents.OnHelmetChanged.Add(OnHelmetChanged);
    }

    public void OnEndFlight()
    {
      GameEvents.OnHelmetChanged.Remove(OnHelmetChanged);
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

    // Must be non-static or the event won't work.
    private void OnHelmetChanged(KerbalEVA eva, bool hasHelmet, bool hasNeckRing)
    {
      var evaModule = eva.GetComponent<TREvaModule>();
      if (evaModule) {
        evaModule.OnHelmetChanged(hasHelmet);
      }
    }

    private static void EnsureEvaModule(Component eva)
    {
      if (eva.GetComponent<TREvaModule>() == null) {
        eva.gameObject.AddComponent<TREvaModule>();
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
      Skin defaultSkin = mapper.GetDefault(kerbal.gender);
      Suit defaultSuit = mapper.GetDefault(kerbal.suit);

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

      // We determine body and helmet texture here to avoid code duplication between suit and helmet cases in the
      // following switch.
      // Setting the suit explicitly -- even when default -- is necessary for two reasons: to fix IVA suits after KSP
      // resetting them to the stock ones all the time and to fix the switch to default texture on start of EVA walk or
      // EVA suit toggle.
      Texture2D suitTexture = suit.GetSuit(useEvaSuit, kerbal) ?? defaultSuit.GetSuit(useEvaSuit, kerbal);
      Texture2D suitNormalMap = suit.GetSuitNRM(useEvaSuit) ?? defaultSuit.GetSuitNRM(useEvaSuit);

      foreach (Renderer renderer in modelTransform.GetComponentsInChildren<Renderer>()) {
        var smr = renderer as SkinnedMeshRenderer;

        // Headlight flares and thruster jets.
        if (smr == null) {
          continue;
        }

        Material material = smr.material;

        Texture2D newTexture = null;
        Texture2D newNormalMap = null;
        Texture2D newEmissive = null;

        switch (smr.name) {
          case "eyeballLeft":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft": {
            if (skin.Eyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.EyeballLeft;

            if (isPrefabMissing) {
              material.shader = Replacer.EyeShader;
              newTexture ??= defaultSkin.EyeballLeft;
            }
            break;
          }
          case "eyeballRight":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight": {
            if (skin.Eyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.EyeballRight;

            if (isPrefabMissing) {
              material.shader = Replacer.EyeShader;
              newTexture ??= defaultSkin.EyeballRight;
            }
            break;
          }
          case "pupilLeft":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft": {
            if (skin.Eyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.PupilLeft;

            if (isPrefabMissing) {
              material.shader = Replacer.EyeShader;
              newTexture ??= defaultSkin.PupilLeft;
            }
            if (newTexture != null) {
              material.color = Color.white;
            }
            break;
          }
          case "pupilRight":
          case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight": {
            if (skin.Eyeless) {
              smr.sharedMesh = null;
              break;
            }

            newTexture = skin.PupilRight;

            if (isPrefabMissing) {
              newTexture ??= defaultSkin.PupilRight;
              material.shader = Replacer.EyeShader;
            }
            if (newTexture != null) {
              material.color = Color.white;
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
              material.shader = newNormalMap == null ? Replacer.HeadShader : Replacer.BumpedHeadShader;
            } else if (newNormalMap != null) {
              material.shader = Replacer.BumpedHeadShader;
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
              material.shader = newNormalMap == null ? Replacer.HeadShader : Replacer.BumpedHeadShader;
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
            if (isEva) {
              newTexture = suitTexture;
              newNormalMap = suitNormalMap;
            } else {
              smr.gameObject.DestroyGameObjectImmediate();
            }
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
              material.color = Color.white;
            }
            break;
          }
          default: { // Jetpack.
            if (isEva) {
              smr.enabled = useEvaSuit;
              if (useEvaSuit) {
                newTexture = suit.Jetpack;
                newNormalMap = suit.JetpackNRM;
                newEmissive = suit.JetpackEmissive;
              }
            }
            break;
          }
        }

        if (newTexture != null) {
          material.mainTexture = newTexture;
        }
        if (newNormalMap != null) {
          material.SetTexture(Util.BumpMapProperty, newNormalMap);
        }
        if (newEmissive != null) {
          material.SetTexture(Util.EmissiveProperty, newEmissive);
        }
      }

      // Backpacks and parachute are positioned on another node in model hierarchy.
      if (isEva) {
        bool showJetpack = useEvaSuit;
        bool showBackpack = showJetpack && !mapper.HideBackpack;

        Transform flagTransform = transform.Find("model/kbEVA_flagDecals");
        Transform cargoPackTransform = transform.Find(kerbal.suit == KerbalSuit.Future && kerbal.gender == Gender.Male
          ? "model/kerbalCargoContainerPack/base"
          : "model/EVABackpack/kerbalCargoContainerPack/base");
        Transform parachutePackTransform = transform.Find("model/EVAparachute/base");
        Transform parachuteCanopyTransform = transform.Find("model/EVAparachute/canopyrot/canopy");

        var flag = flagTransform.GetComponent<Renderer>();
        var cargoPack = cargoPackTransform.GetComponent<Renderer>();
        var parachutePack = parachutePackTransform.GetComponent<Renderer>();
        var parachuteCanopy = parachuteCanopyTransform.GetComponent<Renderer>();

        flag.enabled = showJetpack;
        cargoPack.enabled = showBackpack;
        parachutePack.enabled = showBackpack;

        if (showBackpack) {
          if (suit.CargoPack != null) {
            cargoPack.material.mainTexture = suit.CargoPack;
          }
          if (suit.CargoPackNRM != null) {
            cargoPack.material.SetTexture(Util.BumpMapProperty, suit.CargoPackNRM);
          }
          if (suit.CargoPackEmissive != null) {
            cargoPack.material.SetTexture(Util.EmissiveProperty, suit.CargoPackEmissive);
          }

          if (suit.ParachutePack != null) {
            parachutePack.material.mainTexture = suit.ParachutePack;
          }
          if (suit.ParachutePackNRM != null) {
            parachutePack.material.SetTexture(Util.BumpMapProperty, suit.ParachutePackNRM);
          }
        }

        if (suit.ParachuteCanopy != null) {
          parachuteCanopy.material.mainTexture = suit.ParachuteCanopy;
        }
        if (suit.ParachuteCanopyNRM != null) {
          parachuteCanopy.material.SetTexture(Util.BumpMapProperty, suit.ParachuteCanopyNRM);
        }
      }
    }
  }
}
