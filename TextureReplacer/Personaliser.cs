/*
 * Copyright © 2013-2020 Davorin Učakar
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
    public class Personaliser
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
            foreach (InternalModel model in Resources.FindObjectsOfTypeAll<InternalModel>())
            {
                if (model.GetComponent<TRIvaModelModule>() == null)
                {
                    model.gameObject.AddComponent<TRIvaModelModule>();
                }
            }

            var prefab = Prefab.Instance;

            EnsureEvaModule(prefab.MaleEva);
            EnsureEvaModule(prefab.FemaleEva);
            EnsureEvaModule(prefab.MaleEvaSlim);
            EnsureEvaModule(prefab.FemaleEvaSlim);
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
        public void PersonaliseEva(UnityEngine.Component part, ProtoCrewMember kerbal, bool useEvaSuit)
        {
            PersonaliseKerbal(part, kerbal, true, useEvaSuit);
        }

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
        // Must be non-static or the event won't work.
        private void OnHelmetChanged(UnityEngine.Component eva, bool hasHelmet, bool hasNeckRing)
        {
            var evaModule = eva.GetComponent<TREvaModule>();
            if (evaModule)
            {
                evaModule.OnHelmetChanged(hasHelmet);
            }
        }
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static

        private static void EnsureEvaModule(Component eva)
        {
            if (eva != null && eva.GetComponent<TREvaModule>() == null)
            {
                eva.gameObject.AddComponent<TREvaModule>();
            }
        }

        /// <summary>
        /// Replace textures on a Kerbal model.
        /// </summary>
        private void PersonaliseKerbal(Component component, ProtoCrewMember kerbal, bool isEva, bool useEvaSuit)
        {
            Transform transform = component.transform;

            // Prefabricated Vintage & Future IVA models are missing so they are instantiated anew every time. Hence, we
            // have to apply fixes to them and set fallback default textures.
            bool isPrefabMissing = !isEva && kerbal.suit != KerbalSuit.Default;

            Transform modelTransform = (isEva, kerbal.suit, kerbal.gender) switch
            {
                (false, KerbalSuit.Vintage, _) => transform.Find("kbIVA@idle/model01"),
                (false, KerbalSuit.Future, Gender.Male) => transform.Find("serenityMaleIVA/model01"),
                (false, KerbalSuit.Future, Gender.Female) => transform.Find("serenityFemaleIVA/model01"),
                _ => transform.Find("model01")
            };

            // Sometimes when we switch between suits (e.g. with clothes hanger) suit kind and model get out of sync.
            // Just try all possible nodes in such cases.
#pragma warning disable S3358 // Ternary operators should not be nested
            modelTransform = modelTransform != null
                ? modelTransform
                : (transform.Find("model01") != null)
                    ? transform.Find("model01")
                    : (transform.Find("kbIVA@idle/model01") != null)
                        ? transform.Find("kbIVA@idle/model01")
                        : (transform.Find("serenityMaleIVA/model01") != null)
                            ? transform.Find("serenityMaleIVA/model01")
                            : transform.Find("serenityFemaleIVA/model01");
#pragma warning restore S3358 // Ternary operators should not be nested

            Appearance appearance = mapper.GetAppearance(kerbal);

            Skin skin = mapper.GetKerbalSkin(kerbal, appearance);
            Skin defaultSkin = mapper.GetDefaultSkin(kerbal.gender);

            // We determine body and helmet texture here to avoid code duplication between suit and helmet cases in the
            // following switch. Setting the suit explicitly -- even when default -- is necessary to fix the switch to
            // the default IVA texture
            // when on EVA.
            Suit suit = null;
            Texture2D suitTexture = null;
            Texture2D suitNormalMap = null;

            if (mapper.PersonaliseSuit)
            {
                Suit defaultSuit = mapper.GetDefaultSuit(kerbal.suit);

                suit = mapper.GetKerbalSuit(kerbal, appearance);
                suitTexture = suit.GetSuit(useEvaSuit, kerbal);
                suitTexture = suitTexture.HasValue() ? suitTexture : defaultSuit.GetSuit(useEvaSuit, kerbal);
                suitNormalMap = suit.GetSuitNRM(useEvaSuit);
                suitNormalMap = suitNormalMap.HasValue() ? suitNormalMap : defaultSuit.GetSuitNRM(useEvaSuit);
            }

            foreach (SkinnedMeshRenderer smr in modelTransform.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material material = smr.material;

                Texture2D newTexture = null;
                Texture2D newNormalMap = null;
                Texture2D newEmissive = null;

                switch (smr.name)
                {
                    case "eyeballLeft":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
                        if (skin.Eyeless)
                        {
                            smr.sharedMesh = null;
                            break;
                        }

                        newTexture = skin.EyeballLeft;

                        if (isPrefabMissing)
                        {
                            newTexture = newTexture.HasValue() ? newTexture : defaultSkin.EyeballLeft;
                            material.shader = Replacer.EyeShader;
                        }
                        break;

                    case "eyeballRight":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
                        if (skin.Eyeless)
                        {
                            smr.sharedMesh = null;
                            break;
                        }

                        newTexture = skin.EyeballRight;

                        if (isPrefabMissing)
                        {
                            newTexture = newTexture.HasValue() ? newTexture : defaultSkin.EyeballRight;
                            material.shader = Replacer.EyeShader;
                        }
                        break;

                    case "pupilLeft":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
                        if (skin.Eyeless)
                        {
                            smr.sharedMesh = null;
                            break;
                        }

                        newTexture = skin.PupilLeft;

                        if (isPrefabMissing)
                        {
                            newTexture = newTexture.HasValue() ? newTexture : defaultSkin.PupilLeft;
                            material.shader = Replacer.EyeShader;
                        }

                        if (newTexture.HasValue())
                        {
                            material.color = Color.white;
                        }
                        break;

                    case "pupilRight":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
                        if (skin.Eyeless)
                        {
                            smr.sharedMesh = null;
                            break;
                        }

                        newTexture = skin.PupilRight;

                        if (isPrefabMissing)
                        {
                            newTexture = newTexture.HasValue() ? newTexture : defaultSkin.PupilRight;
                            material.shader = Replacer.EyeShader;
                        }

                        if (newTexture.HasValue())
                        {
                            material.color = Color.white;
                        }
                        break;

                    case "headMesh01":
                    case "headMesh02":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
                        newTexture = skin.Head;
                        newNormalMap = skin.HeadNRM;

                        if (isPrefabMissing)
                        {
                            newTexture = newTexture.HasValue() ? newTexture : defaultSkin.Head;
                            newNormalMap = newNormalMap.HasValue() ? newNormalMap : defaultSkin.HeadNRM;
                            material.shader = Replacer.HeadShader;
                        }

                        if (newNormalMap.HasValue())
                        {
                            material.shader = Replacer.BumpedHeadShader;
                        }
                        break;

                    case "tongue":
                    case "upTeeth01":
                    case "upTeeth02":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
                    case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
                        if (isPrefabMissing)
                        {
                            smr.material = Replacer.Instance.TeethMaterial;
                        }
                        break;

                    case "body01":
                    case "mesh_female_kerbalAstronaut01_body01":
                        if (suit == null)
                        {
                            break;
                        }

                        newTexture = suitTexture;
                        newNormalMap = suitNormalMap;
                        if (isEva)
                        {
                            newEmissive = suit.EvaSuitEmissive;
                        }
                        else if (!kerbal.hasNeckRingOn && kerbal.suit == KerbalSuit.Default)
                        {
                            // Standard IVA suit does not have a separate neck ring mesh that could be removed. Replace
                            // it with EVA model to make the neck ring disappear.
                            smr.sharedMesh = Replacer.Instance.StandardEvaBodyMesh;
                        }

                        // Update textures in Kerbal IVA object since KSP resets them to these values a few frames
                        // later.
                        if (component is Kerbal kerbalIva)
                        {
                            kerbalIva.textureStandard = newTexture;
                            kerbalIva.textureVeteran = newTexture;
                        }
                        break;

                    case "neckRing":
                        if (suit == null)
                        {
                            break;
                        }

                        newTexture = suitTexture;
                        newNormalMap = suitNormalMap;
                        if (isEva)
                        {
                            newEmissive = suit.EvaSuitEmissive;
                        }
                        else if (!kerbal.hasNeckRingOn)
                        {
                            smr.gameObject.DestroyGameObject();
                        }
                        break;

                    case "helmet":
                    case "mesh_female_kerbalAstronaut01_helmet":
                        if (suit == null)
                        {
                            break;
                        }

                        newTexture = suitTexture;
                        newNormalMap = suitNormalMap;
                        if (isEva)
                        {
                            newEmissive = suit.EvaSuitEmissive;
                        }
                        break;

                    case "visor":
                    case "mesh_female_kerbalAstronaut01_visor":
                        if (suit == null)
                        {
                            break;
                        }

                        // Visor texture has to be replaced every time.
                        newTexture = suit.GetVisor(useEvaSuit);
                        if (newTexture.HasValue())
                        {
                            material.color = Color.white;
                        }
                        break;

                    default:
                        // Jetpack.
                        if (!isEva)
                        {
                            break;
                        }

                        if (mapper.IsLegacyKSP)
                        {
                            smr.enabled = useEvaSuit;
                        }

                        if (suit == null)
                        {
                            break;
                        }

                        if (!mapper.IsLegacyKSP || useEvaSuit)
                        {
                            newTexture = suit.Jetpack;
                            newNormalMap = suit.JetpackNRM;
                            newEmissive = suit.JetpackEmissive;
                        }
                        break;
                }

                if (newTexture.HasValue())
                {
                    material.mainTexture = newTexture;
                }
                if (newNormalMap.HasValue())
                {
                    material.SetTexture(Util.BumpMapProperty, newNormalMap);
                }
                if (newEmissive.HasValue())
                {
                    material.SetTexture(Util.EmissiveProperty, newEmissive);
                }
            }

            if (!isEva)
            {
                return;
            }

            // Backpacks and parachute are positioned on another node in model hierarchy.
            Transform cargoPackTransform = (transform.Find("model/EVABackpack/kerbalCargoContainerPack/base") != null)
                ? transform.Find("model/EVABackpack/kerbalCargoContainerPack/base")
                : transform.Find("model/kerbalCargoContainerPack/base");
            Transform parachutePackTransform = transform.Find("model/EVAparachute/base");

            var cargoPack = cargoPackTransform.GetComponent<Renderer>();
            var parachutePack = parachutePackTransform.GetComponent<Renderer>();

            if (mapper.IsLegacyKSP)
            {
                Transform flagTransform = transform.Find("model/kbEVA_flagDecals");
                var flag = flagTransform.GetComponent<Renderer>();

                bool showJetpack = useEvaSuit;
                bool showBackpack = showJetpack && !mapper.HideBackpack;

                flag.enabled = showJetpack;
                cargoPack.enabled = showBackpack;
                parachutePack.enabled = showBackpack;
            }

            if (suit == null)
            {
                return;
            }

            if (suit.CargoPack.HasValue() || suit.CargoPackNRM.HasValue() || suit.CargoPackEmissive.HasValue())
            {
                Material cargoPackMaterial = cargoPack.material;

                if (suit.CargoPack.HasValue())
                {
                    cargoPackMaterial.mainTexture = suit.CargoPack;
                }
                if (suit.CargoPackNRM.HasValue())
                {
                    cargoPackMaterial.SetTexture(Util.BumpMapProperty, suit.CargoPackNRM);
                }
                if (suit.CargoPackEmissive.HasValue())
                {
                    cargoPackMaterial.SetTexture(Util.EmissiveProperty, suit.CargoPackEmissive);
                }
            }

            if (suit.ParachutePack.HasValue() || suit.ParachutePackNRM.HasValue())
            {
                Material parachutePackMaterial = parachutePack.material;

                if (suit.ParachutePack.HasValue())
                {
                    parachutePackMaterial.mainTexture = suit.ParachutePack;
                }
                if (suit.ParachutePackNRM.HasValue())
                {
                    parachutePackMaterial.SetTexture(Util.BumpMapProperty, suit.ParachutePackNRM);
                }
            }

            if (suit.ParachuteCanopy.HasValue() || suit.ParachuteCanopyNRM.HasValue())
            {
                Transform parachuteCanopyTransform = transform.Find("model/EVAparachute/canopyrot/canopy");
                Renderer parachuteCanopy = parachuteCanopyTransform.GetComponent<Renderer>();
                Material parachuteCanopyMaterial = parachuteCanopy.material;

                if (suit.ParachuteCanopy.HasValue())
                {
                    parachuteCanopyMaterial.mainTexture = suit.ParachuteCanopy;
                }
                if (suit.ParachuteCanopyNRM.HasValue())
                {
                    parachuteCanopyMaterial.SetTexture(Util.BumpMapProperty, suit.ParachuteCanopyNRM);
                }
            }
        }
    }
}
