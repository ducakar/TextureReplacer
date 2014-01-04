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

public class VegaCrewMover : PartModule
{
  private static Part srcPart = null;
  private static Part destPart = null;

  private static void log(string s, params object[] args)
  {
    Debug.Log("[CrewMover] " + String.Format(s, args));
  }

  private static void embark(int index)
  {
    log("Embark");

    if (srcPart == null || destPart == null || srcPart.vessel != destPart.vessel
        || srcPart.protoModuleCrew.Count <= index
        || destPart.CrewCapacity >= destPart.protoModuleCrew.Count)
    {
      return;
    }

    ProtoCrewMember cm = srcPart.protoModuleCrew[index];

    srcPart.protoModuleCrew.RemoveAt(index);
    destPart.protoModuleCrew.Add(cm);

    srcPart.vessel.SpawnCrew();
  }

  public static void foo(Part part)
  {
    log("foo {0} {1}", part.protoModuleCrew.Count, Input.GetMouseButton(3));

//    if (!Input.GetMouseButton(2) || part.CrewCapacity == 0)
//      return;

    srcPart = destPart;
    destPart = part;
    destPart.Actions.RemoveAll(a => a.name.StartsWith("Embark "));

    if (srcPart == null || srcPart.vessel != destPart.vessel)
    {
      log("setting null");
      srcPart = null;
      destPart = null;
    }
    else if (srcPart != null && srcPart != destPart
             && destPart.protoModuleCrew.Count < destPart.CrewCapacity)
    {
      for (int i = 0; i < 8 && i < srcPart.protoModuleCrew.Count; ++i)
      {
        ProtoCrewMember cm = srcPart.protoModuleCrew[i];
        log("{0} {1}", i, cm.name);

        destPart.Actions.Add(new BaseAction(destPart.Actions, "Embark " + cm.name, ap => embark(i),
                                            new KSPAction("Embark " + cm.name)));
      }
    }
    else
    {
      log("else");
    }
  }

  public override void OnAwake()
  {
    log("AWAKE {0}", part.name);
    part.AddOnMouseDown(foo);
  }
}
