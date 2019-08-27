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

using System.Linq;
using UnityEngine;
using Gender = ProtoCrewMember.Gender;

namespace TextureReplacer
{
  internal class Suit
  {
    public string Name;
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

    public Texture2D GetSuit(bool useEvaSuit, ProtoCrewMember kerbal)
    {
      int level = kerbal.experienceLevel;
      return useEvaSuit ? EvaSuit[level] : kerbal.veteran && IvaSuitVeteran != null ? IvaSuitVeteran : IvaSuit[level];
    }

    public Texture2D GetSuitNRM(bool useEvaSuit)
    {
      return useEvaSuit ? EvaSuitNRM : IvaSuitNRM;
    }

    public Texture2D GetVisor(bool useEvaSuit)
    {
      return useEvaSuit ? EvaVisor : IvaVisor;
    }

    public void SetIvaSuit(Texture2D texture, int fromLevel, bool force)
    {
      for (int i = fromLevel; i < IvaSuit.Length; ++i) {
        IvaSuit[i] = force ? texture : (IvaSuit[i] ?? texture);
      }
    }

    public void SetEvaSuit(Texture2D texture, int fromLevel, bool force)
    {
      for (int i = fromLevel; i < EvaSuit.Length; ++i) {
        EvaSuit[i] = force ? texture : (EvaSuit[i] ?? texture);
      }
    }

    public bool SetTexture(string originalName, Texture2D texture)
    {
      int level;

      switch (originalName) {
        case "orangeSuite_diffuse":
          IvaSuitVeteran = texture;
          return true;

        case "paleBlueSuite_diffuse":
          SetIvaSuit(texture, 0, true);
          return true;

        case "whiteSuite_diffuse":
          SetEvaSuit(texture, 0, true);
          return true;

        case "orangeSuite_normal":
          IvaSuitNRM = texture;
          EvaSuitNRM = texture;
          return true;

        case "kerbalMain":
          IvaSuitVeteran = IvaSuitVeteran ? IvaSuitVeteran : texture;
          return true;

        case "kerbalMainGrey":
          SetIvaSuit(texture, 0, false);
          return true;

        case "kerbalMainNRM":
          IvaSuitNRM = IvaSuitNRM ? IvaSuitNRM : texture;
          return true;

        case "kerbalVisor":
          IvaVisor = IvaVisor ? IvaVisor : texture;
          return true;

        case "EVAtexture":
          SetEvaSuit(texture, 0, false);
          return true;

        case "EVAtextureNRM":
          EvaSuitNRM = EvaSuitNRM ? EvaSuitNRM : texture;
          return true;

        case "EVAvisor":
          EvaVisor = EvaVisor ? EvaVisor : texture;
          return true;

        case "EVAjetpack":
          EvaJetpack = EvaJetpack ? EvaJetpack : texture;
          return true;

        case "EVAjetpackNRM":
          EvaJetpackNRM = EvaJetpackNRM ? EvaJetpackNRM : texture;
          return true;

        case "kerbalMainGrey1":
        case "kerbalMainGrey2":
        case "kerbalMainGrey3":
        case "kerbalMainGrey4":
        case "kerbalMainGrey5":
          level = originalName.Last() - '0';
          SetIvaSuit(texture, level, true);
          return true;

        case "EVAtexture1":
        case "EVAtexture2":
        case "EVAtexture3":
        case "EVAtexture4":
        case "EVAtexture5":
          level = originalName.Last() - '0';
          SetEvaSuit(texture, level, true);
          return true;

        default:
          return false;
      }
    }
  }
}
