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

using System.Linq;
using UnityEngine;

namespace TextureReplacer
{
    internal class Prefab
    {
        public static Prefab Instance { get; private set; }

        public bool HasSlim;
        public bool HasVintage;
        public bool HasFuture;

        public Kerbal MaleIva;
        public Kerbal FemaleIva;
        public Part MaleEva;
        public Part FemaleEva;

        public Kerbal MaleIvaSlim;
        public Kerbal FemaleIvaSlim;
        public Part MaleEvaSlim;
        public Part FemaleEvaSlim;

        public GameObject MaleIvaVintage;
        public GameObject FemaleIvaVintage;
        public Part MaleEvaVintage;
        public Part FemaleEvaVintage;

        public GameObject MaleIvaFuture;
        public GameObject FemaleIvaFuture;
        public Part MaleEvaFuture;
        public Part FemaleEvaFuture;

        private static readonly Log log = new Log(nameof(Prefab));

        private bool logKerbalHierarchy;

        public static void Recreate()
        {
            Instance = new Prefab();
        }

        /// <summary>
        /// Read configuration and perform pre-load initialisation.
        /// </summary>
        public void ReadConfig(ConfigNode rootNode)
        {
            Util.Parse(rootNode.GetValue("logKerbalHierarchy"), ref logKerbalHierarchy);
        }

        /// <summary>
        /// Load Kerbal prefabs.
        /// </summary>
        public void Load()
        {
            // Shaders between male and female models are inconsistent, female models are missing normal maps and
            // specular lighting. So, we copy shaders from male materials to respective female materials.
            Kerbal[] kerbals = Resources.FindObjectsOfTypeAll<Kerbal>();

            MaleIva   = kerbals.First(k => k.transform.name == "kerbalMale");
            FemaleIva = kerbals.First(k => k.transform.name == "kerbalFemale");
            MaleEva   = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;
            FemaleEva = PartLoader.getPartInfoByName("kerbalEVAfemale").partPrefab;

            MaleIvaSlim   = kerbals.FirstOrDefault(k => k.transform.name == "slimSuitIVAMale");
            FemaleIvaSlim = kerbals.FirstOrDefault(k => k.transform.name == "slimSuitIVAFemale");
            MaleEvaSlim   = PartLoader.getPartInfoByName("kerbalEVASlimSuit")?.partPrefab;
            FemaleEvaSlim = PartLoader.getPartInfoByName("kerbalEVASlimSuitFemale")?.partPrefab;

            HasSlim = MaleIvaSlim != null && FemaleIvaSlim != null && MaleEvaSlim != null && FemaleEvaSlim != null;

            // Vintage Kerbals don't have prefab models loaded. We need to load them from assets.
            AssetBundle missionsBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(b => b.name == "makinghistory_assets");

            if (missionsBundle != null)
            {
                const string maleIvaPrefab   = "assets/expansions/missions/kerbals/iva/kerbalmalevintage.prefab";
                const string femaleIvaPrefab = "assets/expansions/missions/kerbals/iva/kerbalfemalevintage.prefab";

                MaleIvaVintage   = missionsBundle.LoadAsset(maleIvaPrefab) as GameObject;
                FemaleIvaVintage = missionsBundle.LoadAsset(femaleIvaPrefab) as GameObject;
                MaleEvaVintage   = PartLoader.getPartInfoByName("kerbalEVAVintage").partPrefab;
                FemaleEvaVintage = PartLoader.getPartInfoByName("kerbalEVAfemaleVintage").partPrefab;

                HasVintage = true;
            }

            // Future Kerbals don't have prefab models loaded. We need to load them from assets.
            AssetBundle serenityBundle = AssetBundle.GetAllLoadedAssetBundles()
                .FirstOrDefault(b => b.name == "serenity_assets");

            if (serenityBundle != null)
            {
                const string maleIvaPrefab   = "assets/expansions/serenity/kerbals/iva/kerbalmalefuture.prefab";
                const string femaleIvaPrefab = "assets/expansions/serenity/kerbals/iva/kerbalfemalefuture.prefab";

                MaleIvaFuture   = serenityBundle.LoadAsset(maleIvaPrefab) as GameObject;
                FemaleIvaFuture = serenityBundle.LoadAsset(femaleIvaPrefab) as GameObject;
                MaleEvaFuture   = PartLoader.getPartInfoByName("kerbalEVAFuture").partPrefab;
                FemaleEvaFuture = PartLoader.getPartInfoByName("kerbalEVAfemaleFuture").partPrefab;

                HasFuture = true;
            }

            if (logKerbalHierarchy)
            {
                LogHierarchies();
            }
        }

        public static void ExtractSkin(Transform kerbal, Skin skin)
        {
            foreach (SkinnedMeshRenderer smr in kerbal.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var texture = smr.material.mainTexture as Texture2D;
                if (texture != null)
                {
                    skin.SetTexture(texture.name, texture);
                }
            }
        }

        public static void ExtractSuit(Transform kerbal, Suit suit)
        {
            foreach (SkinnedMeshRenderer smr in kerbal.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var texture = smr.material.mainTexture as Texture2D;
                if (texture != null)
                {
                    suit.SetTexture(texture.name, texture);
                }

                if (smr.material.HasProperty(Util.BumpMapProperty))
                {
                    var normalMap = smr.material.GetTexture(Util.BumpMapProperty) as Texture2D;
                    if (normalMap != null)
                    {
                        suit.SetTexture(normalMap.name, normalMap);
                    }
                }
            }
        }

        private void LogHierarchies()
        {
            log.Print("Male IVA Default Hierarchy");
            Util.LogDownHierarchy(MaleIva.transform);
            log.Print("Female IVA Default Hierarchy");
            Util.LogDownHierarchy(FemaleIva.transform);
            log.Print("Male EVA Default Hierarchy");
            Util.LogDownHierarchy(MaleEva.transform);
            log.Print("Female EVA Default Hierarchy");
            Util.LogDownHierarchy(FemaleEva.transform);

            if (MaleIvaSlim != null)
            {
                log.Print("Male IVA Slim Hierarchy");
                Util.LogDownHierarchy(MaleIvaSlim.transform);
            }
            if (FemaleIvaSlim != null)
            {
                log.Print("Female IVA Slim Hierarchy");
                Util.LogDownHierarchy(FemaleIvaSlim.transform);
            }
            if (MaleEvaSlim != null)
            {
                log.Print("Male EVA Slim Hierarchy");
                Util.LogDownHierarchy(MaleEvaSlim.transform);
            }
            if (FemaleEvaSlim != null)
            {
                log.Print("Female EVA Slim Hierarchy");
                Util.LogDownHierarchy(FemaleEvaSlim.transform);
            }

            if (MaleIvaVintage != null)
            {
                log.Print("Male IVA Vintage Hierarchy");
                Util.LogDownHierarchy(MaleIvaVintage.transform);
            }
            if (FemaleIvaVintage != null)
            {
                log.Print("Female IVA Vintage Hierarchy");
                Util.LogDownHierarchy(FemaleIvaVintage.transform);
            }
            if (MaleEvaVintage != null)
            {
                log.Print("Male EVA Vintage Hierarchy");
                Util.LogDownHierarchy(MaleEvaVintage.transform);
            }
            if (FemaleEvaVintage != null)
            {
                log.Print("Female EVA Vintage Hierarchy");
                Util.LogDownHierarchy(FemaleEvaVintage.transform);
            }

            if (MaleIvaFuture != null)
            {
                log.Print("Male IVA Future Hierarchy");
                Util.LogDownHierarchy(MaleIvaFuture.transform);
            }
            if (FemaleIvaFuture != null)
            {
                log.Print("Female IVA Future Hierarchy");
                Util.LogDownHierarchy(FemaleIvaFuture.transform);
            }
            if (MaleEvaFuture != null)
            {
                log.Print("Male EVA Future Hierarchy");
                Util.LogDownHierarchy(MaleEvaFuture.transform);
            }
            if (FemaleEvaFuture != null)
            {
                log.Print("Female EVA Future Hierarchy");
                Util.LogDownHierarchy(FemaleEvaFuture.transform);
            }
        }
    }
}
