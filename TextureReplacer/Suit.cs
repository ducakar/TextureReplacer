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

using System;
using System.Linq;
using UnityEngine;
using Gender = ProtoCrewMember.Gender;
using KerbalSuit = ProtoCrewMember.KerbalSuit;

namespace TextureReplacer
{
    public class Suit
    {
        public const KerbalSuit Default = KerbalSuit.Default;
        public const KerbalSuit Slim = (KerbalSuit)3;
        public const KerbalSuit Vintage = KerbalSuit.Vintage;
        public const KerbalSuit Future = KerbalSuit.Future;

        public readonly string Name;
        public readonly Gender? Gender;
        public readonly KerbalSuit Kind;
        public readonly bool Excluded;

        public readonly Texture2D[] IvaSuit = new Texture2D[6];
        public Texture2D IvaSuitNRM;
        public Texture2D IvaSuitVeteran;
        public Texture2D IvaVisor;

        public readonly Texture2D[] EvaSuit = new Texture2D[6];
        public Texture2D EvaSuitNRM;
        public Texture2D EvaSuitEmissive;
        public Texture2D EvaVisor;

        public Texture2D Jetpack;
        public Texture2D JetpackNRM;
        public Texture2D JetpackEmissive;

        public Texture2D ParachutePack;
        public Texture2D ParachutePackNRM;
        public Texture2D ParachuteCanopy;
        public Texture2D ParachuteCanopyNRM;

        public Texture2D CargoPack;
        public Texture2D CargoPackNRM;
        public Texture2D CargoPackEmissive;

        public Suit(string name)
        {
            Name = name;
            Gender = true switch
            {
                _ when Util.HasSuffix(name, 'm') => ProtoCrewMember.Gender.Male,
                _ when Util.HasSuffix(name, 'f') => ProtoCrewMember.Gender.Female,
                _ => null
            };
            Kind = true switch
            {
                _ when Util.HasSuffix(name, 'S') => Slim,
                _ when Util.HasSuffix(name, 'V') => Vintage,
                _ when Util.HasSuffix(name, 'F') => Future,
                _ => KerbalSuit.Default
            };
            Excluded = Util.HasSuffix(name, 'x');
        }

        public Texture2D GetSuit(bool useEvaSuit, ProtoCrewMember kerbal)
        {
            int level = kerbal.experienceLevel;
            if (useEvaSuit)
            {
                return EvaSuit[level];
            }
            if (kerbal.veteran && IvaSuitVeteran.HasValue())
            {
                return IvaSuitVeteran;
            }
            return IvaSuit[level];
        }

        public Texture2D GetSuitNRM(bool useEvaSuit)
        {
            return useEvaSuit ? EvaSuitNRM : IvaSuitNRM;
        }

        public Texture2D GetVisor(bool useEvaSuit)
        {
            return useEvaSuit ? EvaVisor : IvaVisor;
        }

        public bool SetTexture(string originalName, Texture2D texture)
        {
            int level;
            switch (originalName)
            {
                case "orangeSuite_normal":
                case "slimSuitNormals":
                case "futureSuitMainNRM":
                    IvaSuitNRM = IvaSuitNRM != null ? IvaSuitNRM : texture;
                    EvaSuitNRM = EvaSuitNRM != null ? EvaSuitNRM : texture;
                    return true;

                case "slimSuitDiffuse_blue":
                case "paleBlueSuite_diffuse":
                case "me_suit_difuse_low_polyBrown":
                case "futureSuit_diffuse_whiteBlue":
                case "kerbalMainGrey":
                    for (int i = 0; i < IvaSuit.Length && IvaSuit[i] == null; ++i)
                    {
                        IvaSuit[i] = texture;
                    }
                    return true;

                case "kerbalMainGrey1":
                case "kerbalMainGrey2":
                case "kerbalMainGrey3":
                case "kerbalMainGrey4":
                case "kerbalMainGrey5":
                    level = originalName.Last() - '0';
                    for (int i = level; i < IvaSuit.Length; ++i)
                    {
                        IvaSuit[i] = texture;
                    }
                    return true;

                case "kerbalMainNRM":
                    IvaSuitNRM = IvaSuitNRM != null ? IvaSuitNRM : texture;
                    if (Kind == KerbalSuit.Vintage)
                    {
                        EvaSuitNRM = EvaSuitNRM != null ? EvaSuitNRM : texture;
                    }
                    return true;

                case "orangeSuite_diffuse":
                case "slimSuitDiffuse_orange":
                case "me_suit_difuse_orange":
                case "futureSuit_diffuse_whiteOrange":
                case "kerbalMain":
                    IvaSuitVeteran = IvaSuitVeteran != null ? IvaSuitVeteran : texture;
                    return true;

                case "kerbalVisor":
                    IvaVisor = IvaVisor != null ? IvaVisor : texture;
                    return true;

                case "whiteSuite_diffuse":
                case "slimSuitDiffuse_white":
                case "me_suit_difuse_blue":
                case "futureSuit_diffuse_orange":
                case "EVAtexture":
                    for (int i = 0; i < EvaSuit.Length && EvaSuit[i] == null; ++i)
                    {
                        EvaSuit[i] = texture;
                    }
                    return true;

                case "EVAtexture1":
                case "EVAtexture2":
                case "EVAtexture3":
                case "EVAtexture4":
                case "EVAtexture5":
                    level = originalName.Last() - '0';
                    for (int i = level; i < EvaSuit.Length; ++i)
                    {
                        EvaSuit[i] = texture;
                    }
                    return true;

                case "EVAtextureNRM":
                    EvaSuitNRM = EvaSuitNRM != null ? EvaSuitNRM : texture;
                    return true;

                case "futureSuit_emissive":
                    EvaSuitEmissive = EvaSuitEmissive != null ? EvaSuitEmissive : texture;
                    return true;

                case "EVAvisor":
                    EvaVisor = EvaVisor != null ? EvaVisor : texture;
                    return true;

                case "ksp_ig_jetpack_diffuse":
                case "EVAjetpackscondary":
                case "EVAjetpack":
                    Jetpack = Jetpack != null ? Jetpack : texture;
                    return true;

                case "EVAjetpacksecondary_N":
                case "EVAjetpackNRM":
                    JetpackNRM = JetpackNRM != null ? JetpackNRM : texture;
                    return true;

                case "EVAjetpackEmmisive":
                    JetpackEmissive = JetpackEmissive != null ? JetpackEmissive : texture;
                    return true;

                case "backpack_Diff":
                    ParachutePack = ParachutePack != null ? ParachutePack : texture;
                    return true;

                case "backpack_NM":
                    ParachutePackNRM = ParachutePackNRM != null ? ParachutePackNRM : texture;
                    return true;

                case "canopy_Diff":
                    ParachuteCanopy = ParachuteCanopy != null ? ParachuteCanopy : texture;
                    return true;

                case "canopy_NR":
                    ParachuteCanopyNRM = ParachuteCanopyNRM != null ? ParachuteCanopyNRM : texture;
                    return true;

                case "cargoContainerPack_diffuse":
                    CargoPack = CargoPack != null ? CargoPack : texture;
                    return true;

                case "cargoContainerPack_NRM":
                    CargoPackNRM = CargoPackNRM != null ? CargoPackNRM : texture;
                    return true;

                case "cargoContainerPack_emissive":
                    CargoPackEmissive = CargoPackEmissive != null ? CargoPackEmissive : texture;
                    return true;

                default:
                    return false;
            }
        }
    }
}
