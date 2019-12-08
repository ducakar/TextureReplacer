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
    public readonly ProtoCrewMember.KerbalSuit Kind;

    public Gender Gender;

    public Texture2D IvaSuitVeteran;
    public readonly Texture2D[] IvaSuit = new Texture2D[6];
    public Texture2D IvaSuitNRM;
    public Texture2D IvaVisor;
    public readonly Texture2D[] EvaSuit = new Texture2D[6];
    public Texture2D EvaSuitNRM;
    public Texture2D EvaVisor;
    public Texture2D EvaJetpack;
    public Texture2D EvaJetpackNRM;

    public Suit(string name)
    {
      Name = name;

      if (name == "VINTAGE" || name.EndsWith(".vintage")) {
        Kind = KerbalSuit.Vintage;
      } else if (name == "FUTURE" || name.EndsWith(".future")) {
        Kind = KerbalSuit.Future;
      } else {
        Kind = KerbalSuit.Default;
      }
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
        case "orangeSuite_diffuse":
        case "me_suit_difuse_orange":
        case "futureSuit_diffuse_whiteOrange":
        case "kerbalMain": {
          IvaSuitVeteran ??= texture;
          return true;
        }
        case "paleBlueSuite_diffuse":
        case "me_suit_difuse_low_polyBrown":
        case "kerbalMainGrey": {
          for (int i = 0; i < IvaSuit.Length && IvaSuit[i] == null; ++i) {
            IvaSuit[i] = texture;
          }

          return true;
        }
        case "kerbalMainNRM": {
          IvaSuitNRM ??= texture;
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
        case "EVAtextureNRM": {
          EvaSuitNRM ??= texture;
          return true;
        }
        case "EVAvisor": {
          EvaVisor ??= texture;
          return true;
        }
        case "EVAjetpack": {
          EvaJetpack ??= texture;
          return true;
        }
        case "EVAjetpackNRM": {
          EvaJetpackNRM ??= texture;
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
        case "EVAtexture1":
        case "EVAtexture2":
        case "EVAtexture3":
        case "EVAtexture4":
        case "EVAtexture5": {
          int level = originalName.Last() - '0';
          Array.Fill(EvaSuit, texture, level, EvaSuit.Length - level);
          return true;
        }
        default: {
          return false;
        }
      }
    }
  }
}
