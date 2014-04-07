/*
 * Copyright © 2014 Davorin Učakar
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
using UnityEngine;

namespace TextureReplacer
{
  class Util
  {
    private static readonly char[] CONFIG_DELIMITERS = { ' ', ',' };
    public static readonly string DIR = typeof(Util).Namespace + "/";
    public static readonly string PATH = KSPUtil.ApplicationRootPath + "GameData/" + DIR;

    /**
     * True iff `i` is a power of two.
     */
    public static bool isPow2(int i)
    {
      return i > 0 && (i & (i - 1)) == 0;
    }

    /**
     * Split a space- and/or comma-separated configuration file value into its tokens.
     */
    public static string[] splitConfigValue(string value)
    {
      return value.Split(CONFIG_DELIMITERS, StringSplitOptions.RemoveEmptyEntries);
    }
  }
}
