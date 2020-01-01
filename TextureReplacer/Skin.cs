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

namespace TextureReplacer
{
  internal class Skin
  {
    public readonly string Name;
    public readonly Gender Gender;
    public readonly bool Eyeless;
    public readonly bool Excluded;

    public Texture2D Head;
    public Texture2D HeadNRM;
    public Texture2D EyeballLeft;
    public Texture2D EyeballRight;
    public Texture2D PupilLeft;
    public Texture2D PupilRight;

    public Skin(string name)
    {
      Name = name;
      Gender = Util.HasSuffix(name, 'f') ? Gender.Female : Gender.Male;
      Eyeless = Util.HasSuffix(name, 'e');
      Excluded = Util.HasSuffix(name, 'x');
    }

    public bool SetTexture(string originalName, Texture2D texture)
    {
      switch (originalName) {
        case "kerbalHead":
        case "kerbalGirl_06_BaseColor": {
          Head ??= texture;
          return true;
        }
        case "kerbalHeadNRM":
        case "kerbalGirl_06_BaseColorNRM": {
          HeadNRM ??= texture;
          return true;
        }
        case "eyeballLeft": {
          EyeballLeft ??= texture;
          return true;
        }
        case "eyeballRight": {
          EyeballRight ??= texture;
          return true;
        }
        case "pupilLeft": {
          PupilLeft ??= texture;
          return true;
        }
        case "pupilRight": {
          PupilRight ??= texture;
          return true;
        }
        default: {
          return false;
        }
      }
    }
  }
}
