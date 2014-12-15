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

namespace TextureReplacer
{
  [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT)]
  public class TRScenario : ScenarioModule
  {
    public override void OnLoad(ConfigNode node)
    {
      Personaliser personaliser = Personaliser.instance;
      personaliser.gameKerbals.Clear();
      personaliser.perkSuits.Clear();

      ConfigNode kerbalsNode = node.GetNode("Kerbals") ?? node.GetNode("CustomKerbals");
      if (kerbalsNode == null)
      {
        foreach (var entry in personaliser.customKerbals)
          personaliser.gameKerbals.Add(entry.Key, entry.Value);
      }
      else
      {
        personaliser.readKerbals(kerbalsNode);
      }

      ConfigNode perkSuitsNode = node.GetNode("PerkSuits");
      if (perkSuitsNode == null)
      {
        foreach (var entry in personaliser.defaultPerkSuits)
          personaliser.perkSuits.Add(entry.Key, entry.Value);
      }
      else
      {
        personaliser.readPerkSuits(perkSuitsNode);
      }

      string sIsHelmetRemovalEnabled = node.GetValue("isHelmetRemovalEnabled");
      if (sIsHelmetRemovalEnabled != null)
        bool.TryParse(sIsHelmetRemovalEnabled, out personaliser.isHelmetRemovalEnabled);

      string sIsAtmSuitEnabled = node.GetValue("isAtmSuitEnabled");
      if (sIsAtmSuitEnabled != null)
        bool.TryParse(sIsAtmSuitEnabled, out personaliser.isAtmSuitEnabled);

      string sSuitAssignment = node.GetValue("suitAssignment");
      if (sSuitAssignment != null)
      {
        try
        {
          personaliser.suitAssignment =
            (Personaliser.SuitAssignment) Enum.Parse(typeof(Personaliser.SuitAssignment),
                                                     sSuitAssignment);
        }
        catch (ArgumentException)
        {
          personaliser.suitAssignment = Personaliser.SuitAssignment.RANDOM;
        }
      }
    }

    public override void OnSave(ConfigNode node)
    {
      Personaliser personaliser = Personaliser.instance;

      personaliser.saveKerbals(node.AddNode("Kerbals"));
      personaliser.savePerkSuits(node.AddNode("PerkSuits"));

      node.AddValue("isHelmetRemovalEnabled", personaliser.isHelmetRemovalEnabled);
      node.AddValue("isAtmSuitEnabled", personaliser.isAtmSuitEnabled);
      node.AddValue("suitAssignment", personaliser.suitAssignment);
    }
  }
}
