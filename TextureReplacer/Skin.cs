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

using UnityEngine;
using Gender = ProtoCrewMember.Gender;

namespace TextureReplacer
{
  class Skin
  {
    public string Name;
    public Gender Gender;
    public bool IsEyeless;

    public Texture2D Head;
    public Texture2D HeadNRM;
    public Texture2D Arms;
    public Texture2D ArmsNRM;

    public bool SetTexture(string originalName, Texture2D texture)
    {
      switch (originalName) {
        case "kerbalHead":
          Gender = Gender.Male;
          Head = Head ?? texture;
          return true;

        case "kerbalHeadNRM":
          Gender = Gender.Male;
          HeadNRM = HeadNRM ?? texture;
          return true;

        case "kerbalGirl_06_BaseColor":
          Gender = Gender.Female;
          Head = Head ?? texture;
          return true;

        case "kerbalGirl_06_BaseColorNRM":
          Gender = Gender.Female;
          HeadNRM = HeadNRM ?? texture;
          return true;

        case "kerbal_armHands":
          Arms = Arms ?? texture;
          return true;

        case "kerbal_armHandsNRM":
          ArmsNRM = ArmsNRM ?? texture;
          return true;

        default:
          return false;
      }
    }
  }
}
