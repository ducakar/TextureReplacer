/*
 * Copyright © 2013-2018 Davorin Učakar
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
  class Suit
  {
    Texture2D[] levelIvaBodies;
    Texture2D[] levelEvaBodies;

    public string Name;
    public Gender Gender;

    public Texture2D IvaBodyVeteran;
    public Texture2D IvaBody;
    public Texture2D IvaBodyNRM;
    public Texture2D IvaVisor;
    public Texture2D EvaBody;
    public Texture2D EvaBodyNRM;
    public Texture2D EvaVisor;
    public Texture2D EvaJetpack;
    public Texture2D EvaJetpackNRM;

    public Texture2D GetIvaSuit(ProtoCrewMember kerbal)
    {
      int level = kerbal.experienceLevel;
      return level != 0 && levelIvaBodies != null ? levelIvaBodies[level - 1]
        : kerbal.veteran ? IvaBodyVeteran ?? IvaBody
        : IvaBody;
    }

    public Texture2D GetEvaSuit(ProtoCrewMember kerbal)
    {
      int level = kerbal.experienceLevel;
      return level != 0 && levelEvaBodies != null ? levelEvaBodies[level - 1] : EvaBody;
    }

    public bool SetTexture(string originalName, Texture2D texture)
    {
      int level;

      switch (originalName) {
        case "kerbalMain":
        case "orangeSuite_diffuse":
          IvaBodyVeteran = IvaBodyVeteran ?? texture;
          return true;

        case "kerbalMainGrey":
        case "paleBlueSuite_diffuse":
          IvaBody = IvaBody ?? texture;
          return true;

        case "kerbalMainNRM":
          IvaBodyNRM = IvaBodyNRM ?? texture;
          return true;

        case "orangeSuite_normal":
          IvaBodyNRM = IvaBodyNRM ?? texture;
          EvaBodyNRM = EvaBodyNRM ?? texture;
          return true;

        case "kerbalVisor":
          IvaVisor = IvaVisor ?? texture;
          return true;

        case "EVAtexture":
        case "whiteSuite_diffuse":
          EvaBody = EvaBody ?? texture;
          return true;

        case "EVAtextureNRM":
          EvaBodyNRM = EvaBodyNRM ?? texture;
          return true;

        case "EVAvisor":
          EvaVisor = EvaVisor ?? texture;
          return true;

        case "EVAjetpack":
          EvaJetpack = EvaJetpack ?? texture;
          return true;

        case "EVAjetpackNRM":
          EvaJetpackNRM = EvaJetpackNRM ?? texture;
          return true;

        case "kerbalMainGrey1":
        case "kerbalMainGrey2":
        case "kerbalMainGrey3":
        case "kerbalMainGrey4":
        case "kerbalMainGrey5":
          level = originalName.Last() - 0x30;

          levelIvaBodies = levelIvaBodies ?? new Texture2D[5];
          for (int i = level - 1; i < 5; ++i) {
            levelIvaBodies[i] = texture;
          }
          return true;

        case "EVAtexture1":
        case "EVAtexture2":
        case "EVAtexture3":
        case "EVAtexture4":
        case "EVAtexture5":
          level = originalName.Last() - 0x30;

          levelEvaBodies = levelEvaBodies ?? new Texture2D[5];
          for (int i = level - 1; i < 5; ++i) {
            levelEvaBodies[i] = texture;
          }
          return true;

        default:
          return false;
      }
    }
  }
}
