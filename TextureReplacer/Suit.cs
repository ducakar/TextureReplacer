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
    Texture2D[] levelBodies;
    Texture2D[] levelHelmets;
    Texture2D[] levelEvaBodies;
    Texture2D[] levelEvaHelmets;

    public string Name;
    public Gender Gender;

    public Texture2D BodyVeteran;
    public Texture2D Body;
    public Texture2D BodyNRM;
    public Texture2D Helmet;
    public Texture2D HelmetNRM;
    public Texture2D Visor;
    public Texture2D EvaBody;
    public Texture2D EvaBodyNRM;
    public Texture2D EvaHelmet;
    public Texture2D EvaVisor;
    public Texture2D EvaJetpack;
    public Texture2D EvaJetpackNRM;

    public Texture2D GetBody(int level)
    {
      return level != 0 && levelBodies != null ? levelBodies[level - 1] : Body;
    }

    public Texture2D GetHelmet(int level)
    {
      return level != 0 && levelHelmets != null ? levelHelmets[level - 1] : Helmet;
    }

    public Texture2D GetEvaSuit(int level)
    {
      return level != 0 && levelEvaBodies != null ? levelEvaBodies[level - 1] : EvaBody;
    }

    public Texture2D GetEvaHelmet(int level)
    {
      return level != 0 && levelEvaHelmets != null ? levelEvaHelmets[level - 1] : EvaHelmet;
    }

    public bool SetTexture(string originalName, Texture2D texture)
    {
      int level;

      switch (originalName) {
        case "kerbalMain":
          BodyVeteran = BodyVeteran ?? texture;
          return true;

        case "kerbalMainGrey":
          Body = Body ?? texture;
          return true;

        case "kerbalMainNRM":
          BodyNRM = BodyNRM ?? texture;
          return true;

        case "kerbalHelmetGrey":
          Helmet = Helmet ?? texture;
          return true;

        case "kerbalHelmetNRM":
          HelmetNRM = HelmetNRM ?? texture;
          return true;

        case "kerbalVisor":
          Visor = Visor ?? texture;
          return true;

        case "EVAtexture":
          EvaBody = EvaBody ?? texture;
          return true;

        case "EVAtextureNRM":
          EvaBodyNRM = EvaBodyNRM ?? texture;
          return true;

        case "EVAhelmet":
          EvaHelmet = EvaHelmet ?? texture;
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

          levelBodies = levelBodies ?? new Texture2D[5];
          for (int i = level - 1; i < 5; ++i) {
            levelBodies[i] = texture;
          }
          return true;

        case "kerbalHelmetGrey1":
        case "kerbalHelmetGrey2":
        case "kerbalHelmetGrey3":
        case "kerbalHelmetGrey4":
        case "kerbalHelmetGrey5":
          level = originalName.Last() - 0x30;

          levelHelmets = levelHelmets ?? new Texture2D[5];
          for (int i = level - 1; i < 5; ++i) {
            levelHelmets[i] = texture;
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

        case "EVAhelmet1":
        case "EVAhelmet2":
        case "EVAhelmet3":
        case "EVAhelmet4":
        case "EVAhelmet5":
          level = originalName.Last() - 0x30;

          levelEvaHelmets = levelEvaHelmets ?? new Texture2D[5];
          for (int i = level - 1; i < 5; ++i) {
            levelEvaHelmets[i] = texture;
          }
          return true;

        default:
          return false;
      }
    }
  }
}
