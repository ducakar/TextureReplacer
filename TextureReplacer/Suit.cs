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

using System;
using System.Linq;
using UnityEngine;
using Gender = ProtoCrewMember.Gender;
using KerbalSuit = ProtoCrewMember.KerbalSuit;

namespace TextureReplacer
{
  internal class Suit
  {
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
      Gender = Util.HasSuffix(name, 'm')
        ? ProtoCrewMember.Gender.Male
        : Util.HasSuffix(name, 'f')
          ? ProtoCrewMember.Gender.Female
          : (Gender?) null;

      Kind = Util.HasSuffix(name, 'V')
        ? KerbalSuit.Vintage
        : Util.HasSuffix(name, 'F')
          ? KerbalSuit.Future
          : KerbalSuit.Default;

      Excluded = Util.HasSuffix(name, 'x');
    }

    public Texture2D GetSuit(bool useEvaSuit, ProtoCrewMember kerbal)
    {
      (int level, bool veteran) = kerbal == null ? (0, false) : (kerbal.experienceLevel, kerbal.veteran);
      return useEvaSuit
        ? EvaSuit[level]
        : veteran && IvaSuitVeteran != null
          ? IvaSuitVeteran
          : IvaSuit[level];
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
      switch (originalName) {
        case "orangeSuite_normal":
        case "futureSuitMainNRM": {
          IvaSuitNRM ??= texture;
          EvaSuitNRM ??= texture;
          return true;
        }
        case "paleBlueSuite_diffuse":
        case "me_suit_difuse_low_polyBrown":
        case "futureSuit_diffuse_whiteBlue":
        case "kerbalMainGrey": {
          for (int i = 0; i < IvaSuit.Length && IvaSuit[i] == null; ++i) {
            IvaSuit[i] = texture;
          }
          return true;
        }
        case "kerbalMainGrey1":
        case "kerbalMainGrey2":
        case "kerbalMainGrey3":
        case "kerbalMainGrey4":
        case "kerbalMainGrey5": {
          int level = originalName.Last() - '0';
          Array.Fill(IvaSuit, texture, level, IvaSuit.Length - level);
          return true;
        }
        case "kerbalMainNRM": {
          IvaSuitNRM ??= texture;
          return true;
        }
        case "orangeSuite_diffuse":
        case "me_suit_difuse_orange":
        case "futureSuit_diffuse_whiteOrange":
        case "kerbalMain": {
          IvaSuitVeteran ??= texture;
          return true;
        }
        case "kerbalVisor": {
          IvaVisor ??= texture;
          return true;
        }
        case "whiteSuite_diffuse":
        case "me_suit_difuse_blue":
        case "futureSuit_diffuse_orange":
        case "EVAtexture": {
          for (int i = 0; i < EvaSuit.Length && EvaSuit[i] == null; ++i) {
            EvaSuit[i] = texture;
          }
          return true;
        }
        case "EVAtexture1":
        case "EVAtexture2":
        case "EVAtexture3":
        case "EVAtexture4":
        case "EVAtexture5": {
          int level = originalName.Last() - '0';
          Array.Fill(EvaSuit, texture, level, EvaSuit.Length - level);
          return true;
        }
        case "EVAtextureNRM": {
          EvaSuitNRM ??= texture;
          return true;
        }
        case "futureSuit_emissive": {
          EvaSuitEmissive ??= texture;
          return true;
        }
        case "EVAvisor": {
          EvaVisor ??= texture;
          return true;
        }
        case "EVAjetpack": {
          Jetpack ??= texture;
          return true;
        }
        case "EVAjetpackNRM": {
          JetpackNRM ??= texture;
          return true;
        }
        case "EVAjetpackEmmisive": {
          JetpackEmissive ??= texture;
          return true;
        }
        case "backpack_Diff": {
          ParachutePack ??= texture;
          return true;
        }
        case "backpack_NM": {
          ParachutePackNRM ??= texture;
          return true;
        }
        case "canopy_Diff": {
          ParachuteCanopy ??= texture;
          return true;
        }
        case "canopy_NR": {
          ParachuteCanopyNRM ??= texture;
          return true;
        }
        case "cargoContainerPack_diffuse": {
          CargoPack ??= texture;
          return true;
        }
        case "cargoContainerPack_NRM": {
          CargoPackNRM ??= texture;
          return true;
        }
        case "cargoContainerPack_emissive": {
          CargoPackEmissive ??= texture;
          return true;
        }
        default: {
          return false;
        }
      }
    }
  }
}
