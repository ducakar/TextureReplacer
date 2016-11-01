/*
 * Copyright © 2013-2016 Davorin Učakar, RangeMachine
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

namespace TextureReplacer
{
    public class Personaliser
    {
        public class Head
        {
            public string name;
            public bool isFemale;
            public bool isEyeless;

            public Texture2D head;
            public Texture2D headNRM;
        }

        public class Suit
        {
            public string name;
            public bool isFemale;

            public Texture2D suitVeteran;
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

            private Texture2D[] levelSuits;
            private Texture2D[] levelHelmets;
            private Texture2D[] levelEvaSuits;
            private Texture2D[] levelEvaHelmets;

            public Texture2D getSuit(int level)
            {
                return level != 0 && levelSuits != null ? levelSuits[level - 1] : suit;
            }

            public Texture2D getHelmet(int level)
            {
                return level != 0 && levelHelmets != null ? levelHelmets[level - 1] : helmet;
            }

            public Texture2D getEvaSuit(int level)
            {
                return level != 0 && levelEvaSuits != null ? levelEvaSuits[level - 1] : evaSuit;
            }

            public Texture2D getEvaHelmet(int level)
            {
                return level != 0 && levelEvaHelmets != null ? levelEvaHelmets[level - 1] : evaHelmet;
            }

            public bool setTexture(string originalName, Texture2D texture)
            {
                int level;

                switch (originalName)
                {
                    case "kerbalMain":
                        suitVeteran = suitVeteran ?? texture;
                        return false;

                    case "kerbalMainGrey":
                        suit = suit ?? texture;
                        return true;

                    case "kerbalMainNRM":
                        suitNRM = suitNRM ?? texture;
                        return true;

                    case "kerbalHelmetGrey":
                        helmet = helmet ?? texture;
                        return true;

                    case "kerbalHelmetNRM":
                        helmetNRM = helmetNRM ?? texture;
                        return true;

                    case "kerbalVisor":
                        visor = visor ?? texture;
                        return true;
                    
                    
                    case "EVAtexture":
                        evaSuit = evaSuit ?? texture;
                        return true;

                    case "EVAtextureNRM":
                        evaSuitNRM = evaSuitNRM ?? texture;
                        return true;

                    case "EVAhelmet":
                        evaHelmet = evaHelmet ?? texture;
                        return true;

                    case "EVAvisor":
                        evaVisor = evaVisor ?? texture;
                        return true;

                    case "EVAjetpack":
                        evaJetpack = evaJetpack ?? texture;
                        return true;

                    case "EVAjetpackNRM":
                        evaJetpackNRM = evaJetpackNRM ?? texture;
                        return true;

                    case "kerbalMainGrey1":
                    case "kerbalMainGrey2":
                    case "kerbalMainGrey3":
                    case "kerbalMainGrey4":
                    case "kerbalMainGrey5":
                        level = originalName.Last() - 0x30;
                        levelSuits = levelSuits ?? new Texture2D[5];

                        for (int i = level - 1; i < 5; ++i)
                            levelSuits[i] = texture;
                        return true;

                    case "kerbalHelmetGrey1":
                    case "kerbalHelmetGrey2":
                    case "kerbalHelmetGrey3":
                    case "kerbalHelmetGrey4":
                    case "kerbalHelmetGrey5":
                        level = originalName.Last() - 0x30;
                        levelHelmets = levelHelmets ?? new Texture2D[5];

                        for (int i = level - 1; i < 5; ++i)
                            levelHelmets[i] = texture;
                        return true;

                    case "EVAtexture1":
                    case "EVAtexture2":
                    case "EVAtexture3":
                    case "EVAtexture4":
                    case "EVAtexture5":
                        level = originalName.Last() - 0x30;
                        levelEvaSuits = levelEvaSuits ?? new Texture2D[5];

                        for (int i = level - 1; i < 5; ++i)
                            levelEvaSuits[i] = texture;
                        return true;

                    case "EVAhelmet1":
                    case "EVAhelmet2":
                    case "EVAhelmet3":
                    case "EVAhelmet4":
                    case "EVAhelmet5":
                        level = originalName.Last() - 0x30;
                        levelEvaHelmets = levelEvaHelmets ?? new Texture2D[5];

                        for (int i = level - 1; i < 5; ++i)
                            levelEvaHelmets[i] = texture;
                        return true;

                    default:
                        return false;
                }
            }
        }

        public class KerbalData
        {
            public int hash;
            public int gender;
            public bool isVeteran;

            public Head head;
            public Suit suit;
            public Suit cabinSuit;
        }

        /**
         * Component bound to internal models that triggers Kerbal texture personalisation when the
         * internal model changes.
         */

        private class TRIvaModule : MonoBehaviour
        {
            public void Start()
            {
                Personaliser.instance.personaliseIva(GetComponent<Kerbal>());
                Destroy(this);
            }
        }

        private class TREvaModule : PartModule
        {
            private Reflections.Script reflectionScript = null;

            [KSPField(isPersistant = true)]
            private bool isInitialised = false;

            [KSPField(isPersistant = true)]
            public bool hasEvaSuit = false;

            [KSPEvent(guiActive = true, guiName = "Toggle EVA Suit")]
            public void toggleEvaSuit()
            {
                Personaliser personaliser = Personaliser.instance;

                if (personaliser.personaliseEva(part, !hasEvaSuit))
                {
                    hasEvaSuit = !hasEvaSuit;

                    if (reflectionScript != null)
                        reflectionScript.setActive(hasEvaSuit);
                }
                else
                {
                    ScreenMessages.PostScreenMessage("No breathable atmosphere", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            public override void OnStart(StartState state)
            {
                Personaliser personaliser = Personaliser.instance;

                if (!isInitialised)
                {
                    if (!personaliser.isAtmSuitEnabled)
                    {
                        Events.First().active = false;
                        hasEvaSuit = true;
                    }

                    isInitialised = true;
                }

                if (!personaliser.personaliseEva(part, hasEvaSuit))
                    hasEvaSuit = true;

                if (Reflections.instance.isVisorReflectionEnabled
                    && Reflections.instance.reflectionType == Reflections.Type.REAL)
                {
                    reflectionScript = new Reflections.Script(part, 1);
                    reflectionScript.setActive(hasEvaSuit);
                }
            }

            public void Update()
            {
                Personaliser personaliser = Personaliser.instance;

                if (!hasEvaSuit && !personaliser.isAtmBreathable())
                {
                    personaliser.personaliseEva(part, true);
                    hasEvaSuit = true;

                    if (reflectionScript != null)
                        reflectionScript.setActive(true);
                }
            }

            public void OnDestroy()
            {
                if (reflectionScript != null)
                    reflectionScript.destroy();
            }
        }

        private static readonly string DIR_DEFAULT = Util.DIR + "Default/";
        private static readonly string DIR_HEADS = Util.DIR + "Heads/";
        private static readonly string DIR_SUITS = Util.DIR + "Suits/";
        private static readonly string[] VETERANS = { "Jebediah Kerman", "Bill Kerman", "Bob Kerman", "Valentina Kerman" };
        // Default textures (from `Default/`).
        public readonly Head[] defaultHead = { new Head { name = "DEFAULT" }, new Head { name = "DEFAULT" } };

        public readonly Suit defaultSuit = new Suit { name = "DEFAULT" };
        // All Kerbal textures, including excluded by configuration.
        public readonly List<Head> heads = new List<Head>();

        public readonly List<Suit> suits = new List<Suit>();
        // Male/female textures (minus excluded).
        private readonly List<Head>[] kerbalHeads = { new List<Head>(), new List<Head>() };

        private readonly List<Suit>[] kerbalSuits = { new List<Suit>(), new List<Suit>() };
        // Personalised Kerbal textures.
        private readonly Dictionary<string, KerbalData> gameKerbals = new Dictionary<string, KerbalData>();

        // Backed-up personalised textures from main configuration files. These are used to initialise kerbals if a saved
        // game doesn't contain `TRScenario`.
        private ConfigNode customKerbalsNode = new ConfigNode();

        // Class-specific suits.
        public readonly Dictionary<string, Suit> classSuits = new Dictionary<string, Suit>();

        public readonly Dictionary<string, Suit> defaultClassSuits = new Dictionary<string, Suit>();
        // Cabin-specific suits.
        private readonly Dictionary<string, Suit> cabinSuits = new Dictionary<string, Suit>();

        // Helmet removal.
        private Mesh[] helmetMesh = { null, null };

        private Mesh[] visorMesh = { null, null };
        public bool isHelmetRemovalEnabled = true;
        // Convert all females to males but still use female textures for them.
        private bool forceLegacyFemales = false;

        // Atmospheric IVA suit parameters.
        public bool isAtmSuitEnabled = true;

        private double atmSuitPressure = 50.0;
        private readonly HashSet<string> atmSuitBodies = new HashSet<string>();
        // Instance.
        public static Personaliser instance = null;

        /**
         * Whether a vessel is in a "safe" situation, so Kerbals don't need helmets (landed/splashed or in orbit).
         */

        private static bool isSituationSafe(Vessel vessel)
        {
            return vessel.situation != Vessel.Situations.FLYING && vessel.situation != Vessel.Situations.SUB_ORBITAL;
        }

        /**
         * Whether atmosphere is breathable.
         */

        public bool isAtmBreathable()
        {
            bool value = !HighLogic.LoadedSceneIsFlight
                         || (FlightGlobals.getStaticPressure() >= atmSuitPressure
                         && atmSuitBodies.Contains(FlightGlobals.currentMainBody.bodyName));
            return value;
        }

        private Suit getClassSuit(ProtoCrewMember kerbal)
        {
            Suit suit;
            classSuits.TryGetValue(kerbal.experienceTrait.Config.Name, out suit);
            return suit;
        }

        public KerbalData getKerbalData(ProtoCrewMember kerbal)
        {
            KerbalData kerbalData;

            if (!gameKerbals.TryGetValue(kerbal.name, out kerbalData))
            {
                kerbalData = new KerbalData
                {
                    hash = kerbal.name.GetHashCode(),
                    gender = (int)kerbal.gender,
                    isVeteran = VETERANS.Any(n => n == kerbal.name)
                };
                gameKerbals.Add(kerbal.name, kerbalData);

                if (forceLegacyFemales)
                    kerbal.gender = ProtoCrewMember.Gender.Male;
            }
            return kerbalData;
        }

        public Head getKerbalHead(ProtoCrewMember kerbal, KerbalData kerbalData)
        {
            if (kerbalData.head != null)
                return kerbalData.head;

            List<Head> genderHeads = kerbalHeads[kerbalData.gender];
            if (genderHeads.Count == 0)
                return defaultHead[(int)kerbal.gender];

            // Hash is multiplied with a large prime to increase randomisation, since hashes returned by `GetHashCode()` are
            // close together if strings only differ in the last (few) char(s).
            int number = (kerbalData.hash * 4099) & 0x7fffffff;
            return genderHeads[number % genderHeads.Count];
        }

        public Suit getKerbalSuit(ProtoCrewMember kerbal, KerbalData kerbalData)
        {
            Suit suit = kerbalData.suit ?? getClassSuit(kerbal);
            if (suit != null)
                return suit;

            List<Suit> genderSuits = kerbalSuits[0];

            // Use female suits only if available, fall back to male suits otherwise.
            if (kerbalData.gender != 0 && kerbalSuits[1].Count != 0)
                genderSuits = kerbalSuits[1];
            else if (genderSuits.Count == 0)
                return defaultSuit;

            // We must use a different prime here to increase randomisation so that the same head is not always combined with
            // the same suit.
            int number = ((kerbalData.hash + kerbal.name.Length) * 2053) & 0x7fffffff;
            return genderSuits[number % genderSuits.Count];
        }

        /**
         * Replace textures on a Kerbal model.
         */

        private void personaliseKerbal(Component component, ProtoCrewMember kerbal, Part cabin, bool needsSuit)
        {
            KerbalData kerbalData = getKerbalData(kerbal);
            bool isEva = cabin == null;

            Head head = getKerbalHead(kerbal, kerbalData);
            Suit suit = null;

            if (isEva || !cabinSuits.TryGetValue(cabin.partInfo.name, out kerbalData.cabinSuit))
                suit = getKerbalSuit(kerbal, kerbalData);

            head = head == defaultHead[(int)kerbal.gender] ? null : head;
            suit = (isEva && needsSuit) || kerbalData.cabinSuit == null ? suit : kerbalData.cabinSuit;
            suit = suit == defaultSuit ? null : suit;

            Transform model = isEva ? component.transform.Find("model01") : component.transform.Find("kbIVA@idle/model01");
            Transform flag = isEva ? component.transform.Find("model/kbEVA_flagDecals") : null;

            if (isEva)
                flag.GetComponent<Renderer>().enabled = needsSuit;

            // We must include hidden meshes, since flares are hidden when light is turned off.
            // All other meshes are always visible, so no performance hit here.
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var smr = renderer as SkinnedMeshRenderer;

                // Thruster jets, flag decals and headlight flares.
                if (smr == null)
                {
                    if (renderer.name != "screenMessage")
                        renderer.enabled = needsSuit;
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
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballLeft":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_eyeballRight":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilLeft":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pupilRight":
                            if (head != null && head.isEyeless)
                                smr.sharedMesh = null;

                            break;

                        case "headMesh01":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_pCube1":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_polySurface51":
                        case "headMesh":
                        case "ponytail":
                            if (head != null)
                            {
                                newTexture = head.head;
                                newNormalMap = head.headNRM;
                            }
                            break;

                        case "tongue":
                        case "upTeeth01":
                        case "upTeeth02":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_upTeeth01":
                        case "mesh_female_kerbalAstronaut01_kerbalGirl_mesh_downTeeth01":
                        case "downTeeth01":
                            break;

                        case "body01":
                        case "mesh_female_kerbalAstronaut01_body01":
                            bool isEvaSuit = isEva && needsSuit;

                            if (suit != null)
                            {
                                newTexture = isEvaSuit ? suit.getEvaSuit(kerbal.experienceLevel) : suit.getSuit(kerbal.experienceLevel);
                                newNormalMap = isEvaSuit ? suit.evaSuitNRM : suit.suitNRM;
                            }

                            if (newTexture == null)
                            {
                                // This required for two reasons: to fix IVA suits after KSP resetting them to the stock ones all the
                                // time and to fix the switch from non-default to default texture during EVA suit toggle.
                                newTexture = isEvaSuit ? defaultSuit.evaSuit
                                  : kerbalData.isVeteran ? defaultSuit.suitVeteran
                                  : defaultSuit.suit;
                            }

                            if (newNormalMap == null)
                                newNormalMap = isEvaSuit ? defaultSuit.evaSuitNRM : defaultSuit.suitNRM;

                            // Update textures in Kerbal IVA object since KSP resets them to these values a few frames later.
                            if (!isEva)
                            {
                                Kerbal kerbalIVA = (Kerbal)component;

                                kerbalIVA.textureStandard = newTexture;
                                kerbalIVA.textureVeteran = newTexture;
                            }
                            break;
                        
                        case "helmet":
                        case "mesh_female_kerbalAstronaut01_helmet":
                            if (isEva)
                                smr.enabled = needsSuit;
                            else
                                smr.sharedMesh = needsSuit ? helmetMesh[(int)kerbal.gender] : null;

                            // Textures have to be replaced even when hidden since it may become visible later on situation change.
                            if (suit != null)
                            {
                                newTexture = isEva ? suit.getEvaHelmet(kerbal.experienceLevel) : suit.getHelmet(kerbal.experienceLevel);
                                newNormalMap = suit.helmetNRM;
                            }
                            break;

                        case "visor":
                        case "mesh_female_kerbalAstronaut01_visor":
                            if (isEva)
                                smr.enabled = needsSuit;
                            else
                                smr.sharedMesh = needsSuit ? visorMesh[(int)kerbal.gender] : null;

                            // Textures have to be replaced even when hidden since it may become visible later on situation change.
                            if (suit != null)
                            {
                                newTexture = isEva ? suit.evaVisor : suit.visor;

                                if (newTexture != null)
                                    material.color = Color.white;
                            }
                            break;

                        default: // Jetpack.
                            if (isEva)
                            {
                                smr.enabled = needsSuit;

                                if (needsSuit && suit != null)
                                {
                                    newTexture = suit.evaJetpack;
                                    newNormalMap = suit.evaJetpackNRM;
                                }
                            }
                            break;
                    }

                    if (newTexture != null)
                        material.mainTexture = newTexture;

                    if (newNormalMap != null)
                        material.SetTexture(Util.BUMPMAP_PROPERTY, newNormalMap);
                }
            }
        }

        /**
         * Personalise Kerbals in an internal space of a vessel. Used by IvaModule.
         */

        public void personaliseIva(Kerbal kerbal)
        {
            bool needsSuit = !isHelmetRemovalEnabled || !isSituationSafe(kerbal.InVessel);

            personaliseKerbal(kerbal, kerbal.protoCrewMember, kerbal.InPart, needsSuit);
        }

        private void updateHelmets(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> action)
        {
            Vessel vessel = action.host;
            if (!isHelmetRemovalEnabled || vessel == null)
                return;

            foreach (Part part in vessel.parts.Where(p => p.internalModel != null))
            {
                Kerbal[] kerbals = part.internalModel.GetComponentsInChildren<Kerbal>();
                if (kerbals.Length != 0)
                {
                    bool hideHelmets = isSituationSafe(vessel);

                    foreach (Kerbal kerbal in kerbals.Where(k => k.showHelmet))
                    {
                        // `Kerbal.ShowHelmet(false)` irreversibly removes a helmet while
                        // `Kerbal.ShowHelmet(true)` has no effect at all. We need the following workaround.
                        foreach (SkinnedMeshRenderer smr in kerbal.helmetTransform.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            if (smr.name.EndsWith("helmet", StringComparison.Ordinal))
                                smr.sharedMesh = hideHelmets ? null : helmetMesh[(int)kerbal.protoCrewMember.gender];
                            else if (smr.name.EndsWith("visor", StringComparison.Ordinal))
                                smr.sharedMesh = hideHelmets ? null : visorMesh[(int)kerbal.protoCrewMember.gender];
                        }
                    }
                }
            }
        }

        /**
         * Set external EVA/IVA suit. Fails and return false iff trying to remove EVA suit outside of breathable atmosphere.
         * This function is used by EvaModule.
         */

        private bool personaliseEva(Part evaPart, bool evaSuit)
        {
            bool success = true;

            List<ProtoCrewMember> crew = evaPart.protoModuleCrew;
            if (crew.Count != 0)
            {
                if (!evaSuit && !isAtmBreathable())
                {
                    evaSuit = true;
                    success = false;
                }

                personaliseKerbal(evaPart, crew[0], null, evaSuit);
            }
            return success;
        }

        /**
         * Load per-game custom kerbals mapping.
         */

        private void loadKerbals(ConfigNode node)
        {
            node = node ?? customKerbalsNode;

            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned))
            {
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead
                    && kerbal.type != ProtoCrewMember.KerbalType.Unowned)
                {
                    continue;
                }

                KerbalData kerbalData = getKerbalData(kerbal);

                string value = node.GetValue(kerbal.name);
                if (value != null)
                {
                    string[] tokens = Util.splitConfigValue(value);
                    string genderName = tokens.Length >= 1 ? tokens[0] : null;
                    string headName = tokens.Length >= 2 ? tokens[1] : null;
                    string suitName = tokens.Length >= 3 ? tokens[2] : null;

                    if (genderName != null)
                        kerbalData.gender = genderName == "F" ? 1 : 0;

                    if (headName != null && headName != "GENERIC")
                    {
                        kerbalData.head = headName == "DEFAULT" ? defaultHead[(int)kerbal.gender]
                          : heads.Find(h => h.name == headName);
                    }

                    if (suitName != null && suitName != "GENERIC")
                        kerbalData.suit = suitName == "DEFAULT" ? defaultSuit : suits.Find(s => s.name == suitName);

                    kerbal.gender = forceLegacyFemales ? ProtoCrewMember.Gender.Male : (ProtoCrewMember.Gender)kerbalData.gender;
                }
            }
        }

        /**
         * Save per-game custom Kerbals mapping.
         */

        private void saveKerbals(ConfigNode node)
        {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            foreach (ProtoCrewMember kerbal in roster.Crew.Concat(roster.Tourist).Concat(roster.Unowned))
            {
                if (kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Dead
                    && kerbal.type != ProtoCrewMember.KerbalType.Unowned)
                {
                    continue;
                }

                KerbalData kerbalData = getKerbalData(kerbal);

                string genderName = kerbalData.gender == 0 ? "M" : "F";
                string headName = kerbalData.head == null ? "GENERIC" : kerbalData.head.name;
                string suitName = kerbalData.suit == null ? "GENERIC" : kerbalData.suit.name;

                node.AddValue(kerbal.name, genderName + " " + headName + " " + suitName);
            }
        }

        /**
         * Load suit mapping.
         */

        private void loadSuitMap(ConfigNode node, IDictionary<string, Suit> map, IDictionary<string, Suit> defaultMap = null)
        {
            if (node == null)
            {
                if (defaultMap != null)
                {
                    foreach (var entry in defaultMap)
                        map[entry.Key] = entry.Value;
                }
            }
            else
            {
                foreach (ConfigNode.Value entry in node.values)
                {
                    map.Remove(entry.name);

                    string suitName = entry.value;

                    if (suitName != null && suitName != "GENERIC")
                    {
                        if (suitName == "DEFAULT")
                        {
                            map[entry.name] = defaultSuit;
                        }
                        else
                        {
                            Suit suit = suits.Find(s => s.name == suitName);
                            if (suit != null)
                                map[entry.name] = suit;
                        }
                    }
                }
            }
        }

        /**
         * Save suit mapping.
         */

        private static void saveSuitMap(Dictionary<string, Suit> map, ConfigNode node)
        {
            foreach (var entry in map)
            {
                string suitName = entry.Value == null ? "GENERIC" : entry.Value.name;

                node.AddValue(entry.Key, suitName);
            }
        }

        /**
         * Fill config for custom Kerbal heads and suits.
         */

        private void readKerbalsConfigs()
        {
            var excludedHeads = new List<string>();
            var excludedSuits = new List<string>();
            var femaleHeads = new List<string>();
            var femaleSuits = new List<string>();
            var eyelessHeads = new List<string>();

            foreach (UrlDir.UrlConfig file in GameDatabase.Instance.GetConfigs("TextureReplacer"))
            {
                ConfigNode customNode = file.config.GetNode("CustomKerbals");
                if (customNode != null)
                {
                    // Merge into `customKerbalsNode`.
                    foreach (ConfigNode.Value entry in customNode.values)
                    {
                        customKerbalsNode.RemoveValue(entry.name);
                        customKerbalsNode.AddValue(entry.name, entry.value);
                    }
                }

                ConfigNode genericNode = file.config.GetNode("GenericKerbals");
                if (genericNode != null)
                {
                    Util.addLists(genericNode.GetValues("excludedHeads"), excludedHeads);
                    Util.addLists(genericNode.GetValues("excludedSuits"), excludedSuits);
                    Util.addLists(genericNode.GetValues("femaleHeads"), femaleHeads);
                    Util.addLists(genericNode.GetValues("femaleSuits"), femaleSuits);
                    Util.addLists(genericNode.GetValues("eyelessHeads"), eyelessHeads);
                }

                ConfigNode classNode = file.config.GetNode("ClassSuits");
                if (classNode != null)
                    loadSuitMap(classNode, defaultClassSuits);

                ConfigNode cabinNode = file.config.GetNode("CabinSuits");
                if (cabinNode != null)
                    loadSuitMap(cabinNode, cabinSuits);
            }

            // Tag female and eye-less heads.
            foreach (Head head in heads)
            {
                head.isFemale = femaleHeads.Contains(head.name);
                head.isEyeless = eyelessHeads.Contains(head.name);
            }
            // Tag female suits.
            foreach (Suit suit in suits)
                suit.isFemale = femaleSuits.Contains(suit.name);

            // Create lists of male heads and suits.
            kerbalHeads[0].AddRange(heads.Where(h => !h.isFemale && !excludedHeads.Contains(h.name)));
            kerbalSuits[0].AddRange(suits.Where(s => !s.isFemale && !excludedSuits.Contains(s.name)));

            // Create lists of female heads and suits.
            kerbalHeads[1].AddRange(heads.Where(h => h.isFemale && !excludedHeads.Contains(h.name)));
            kerbalSuits[1].AddRange(suits.Where(s => s.isFemale && !excludedSuits.Contains(s.name)));

            // Trim lists.
            heads.TrimExcess();
            suits.TrimExcess();
            kerbalHeads[0].TrimExcess();
            kerbalSuits[0].TrimExcess();
            kerbalHeads[1].TrimExcess();
            kerbalSuits[1].TrimExcess();
        }

        /**
         * Read configuration and perform pre-load initialisation.
         */

        public void readConfig(ConfigNode rootNode)
        {
            Util.parse(rootNode.GetValue("isHelmetRemovalEnabled"), ref isHelmetRemovalEnabled);
            Util.parse(rootNode.GetValue("isAtmSuitEnabled"), ref isAtmSuitEnabled);
            Util.parse(rootNode.GetValue("atmSuitPressure"), ref atmSuitPressure);
            Util.addLists(rootNode.GetValues("atmSuitBodies"), atmSuitBodies);
            Util.parse(rootNode.GetValue("forceLegacyFemales"), ref forceLegacyFemales);
        }

        /**
         * Post-load initialisation.
         */

        public void load()
        {
            var suitDirs = new Dictionary<string, int>();
            string lastTextureName = "";

            foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
            {
                Texture2D texture = texInfo.texture;
                if (texture == null || !texture.name.StartsWith(Util.DIR, StringComparison.Ordinal))
                    continue;

                // Add a head texture.
                if (texture.name.StartsWith(DIR_HEADS, StringComparison.Ordinal))
                {
                    texture.wrapMode = TextureWrapMode.Clamp;

                    string headName = texture.name.Substring(DIR_HEADS.Length);
                    if (headName.EndsWith("NRM", StringComparison.Ordinal))
                    {
                        string baseName = headName.Substring(0, headName.Length - 3);

                        Head head = heads.Find(h => h.name == baseName);
                        if (head != null)
                            head.headNRM = texture;
                    }
                    else if (heads.All(h => h.name != headName))
                    {
                        Head head = new Head { name = headName, head = texture };
                        heads.Add(head);
                    }
                }
                // Add a suit texture.
                else if (texture.name.StartsWith(DIR_SUITS, StringComparison.Ordinal))
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

                        int index;
                        if (!suitDirs.TryGetValue(dirName, out index))
                        {
                            index = suits.Count;
                            suits.Add(new Suit { name = dirName });
                            suitDirs.Add(dirName, index);
                        }

                        Suit suit = suits[index];
                        if (!suit.setTexture(originalName, texture))
                            Util.log("Unknown suit texture name \"{0}\": {1}", originalName, texture.name);
                    }
                }
                else if (texture.name.StartsWith(DIR_DEFAULT, StringComparison.Ordinal))
                {
                    int lastSlash = texture.name.LastIndexOf('/');
                    string originalName = texture.name.Substring(lastSlash + 1);

                    if (originalName == "kerbalHead")
                    {
                        defaultHead[0].head = texture;
                        texture.wrapMode = TextureWrapMode.Clamp;
                    }
                    else if (originalName == "kerbalHeadNRM")
                    {
                        defaultHead[0].headNRM = texture;
                        texture.wrapMode = TextureWrapMode.Clamp;
                    }
                    else if (originalName == "kerbalGirl_06_BaseColor")
                    {
                        defaultHead[1].head = texture;
                        texture.wrapMode = TextureWrapMode.Clamp;
                    }
                    else if (originalName == "kerbalGirl_06_BaseColorNRM")
                    {
                        defaultHead[1].headNRM = texture;
                        texture.wrapMode = TextureWrapMode.Clamp;
                    }
                    else if (defaultSuit.setTexture(originalName, texture) || originalName == "kerbalMain")
                    {
                        texture.wrapMode = TextureWrapMode.Clamp;
                    }
                }

                lastTextureName = texture.name;
            }

            readKerbalsConfigs();

            // Initialise default Kerbal, which is only loaded when the main menu shows.
            foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (texture.name != null)
                {
                    if (texture.name == "kerbalHead")
                        defaultHead[0].head = defaultHead[0].head ?? texture;
                    else if (texture.name == "kerbalGirl_06_BaseColor")
                        defaultHead[1].head = defaultHead[1].head ?? texture;
                    else
                        defaultSuit.setTexture(texture.name, texture);
                }
            }

            foreach (Kerbal kerbal in Resources.FindObjectsOfTypeAll<Kerbal>())
            {
                int gender = kerbal.transform.name == "kerbalFemale" ? 1 : 0;

                // Save pointer to helmet & visor meshes so helmet removal can restore them.
                foreach (SkinnedMeshRenderer smr in kerbal.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.name.EndsWith("helmet", StringComparison.Ordinal))
                        helmetMesh[gender] = smr.sharedMesh;
                    else if (smr.name.EndsWith("visor", StringComparison.Ordinal))
                        visorMesh[gender] = smr.sharedMesh;
                }

                // After na IVA space is initialised, suits are reset to these values. Replace stock textures with default ones.
                kerbal.textureStandard = defaultSuit.suit;
                kerbal.textureVeteran = defaultSuit.suitVeteran;

                if (kerbal.GetComponent<TRIvaModule>() == null)
                    kerbal.gameObject.AddComponent<TRIvaModule>();
            }

            Part[] evas = {
        PartLoader.getPartInfoByName("kerbalEVA").partPrefab,
        PartLoader.getPartInfoByName("kerbalEVAfemale").partPrefab
      };

            foreach (Part eva in evas)
            {
                if (eva.GetComponent<TREvaModule>() == null)
                    eva.gameObject.AddComponent<TREvaModule>();
            }

            // Re-read scenario if database is reloaded during the space centre scene to avoid losing all per-game settings.
            if (HighLogic.CurrentGame != null)
            {
                ConfigNode scenarioNode = HighLogic.CurrentGame.config.GetNodes("SCENARIO")
                  .FirstOrDefault(n => n.GetValue("name") == "TRScenario");

                if (scenarioNode != null)
                    loadScenario(scenarioNode);
            }
        }

        public void beginFlight()
        {
            GameEvents.onVesselSituationChange.Add(updateHelmets);
        }

        public void endFlight()
        {
            GameEvents.onVesselSituationChange.Remove(updateHelmets);
        }

        public void loadScenario(ConfigNode node)
        {
            gameKerbals.Clear();
            classSuits.Clear();

            loadKerbals(node.GetNode("Kerbals"));
            loadSuitMap(node.GetNode("ClassSuits"), classSuits, defaultClassSuits);

            Util.parse(node.GetValue("isHelmetRemovalEnabled"), ref isHelmetRemovalEnabled);
            Util.parse(node.GetValue("isAtmSuitEnabled"), ref isAtmSuitEnabled);
        }

        public void saveScenario(ConfigNode node)
        {
            saveKerbals(node.AddNode("Kerbals"));
            saveSuitMap(classSuits, node.AddNode("ClassSuits"));

            node.AddValue("isHelmetRemovalEnabled", isHelmetRemovalEnabled);
            node.AddValue("isAtmSuitEnabled", isAtmSuitEnabled);
        }

        public void resetKerbals()
        {
            gameKerbals.Clear();
            classSuits.Clear();

            loadKerbals(null);
            loadSuitMap(null, classSuits, defaultClassSuits);
        }
    }
}