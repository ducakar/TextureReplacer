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

#if false
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TextureReplacer
{
  internal class Asteroids
  {
    private class AsteroidMaterial
    {
      public string name;
      public Texture2D texture;
      public Texture2D textureNRM;
    }

    private static readonly string DIR_ASTEROIDS = Util.DIR + "Heads/";
    // Set of asteroid textures.
    private List<AsteroidMaterial> materials = new List<AsteroidMaterial>();
    // List of asteroid vessels that have to be updated.
    private List<Vessel> asteroidVessels = new List<Vessel>();
    // Instance.
    public static Asteroids instance = null;

    /**
     * Replace asteroid textures.
     */
    private void replaceAsteroidSkins()
    {
      foreach (Vessel vessel in asteroidVessels)
      {
        if (vessel == null || !vessel.loaded || vessel.vesselName == null)
          continue;

        MeshRenderer mr = vessel.GetComponentInChildren<MeshRenderer>();

        int index = (vessel.id.GetHashCode() & 0x7fffffff) % materials.Count;
        mr.material.mainTexture = materials[index].texture;

        string s = "";
        foreach(var sk in mr.material.shaderKeywords)
          s += " :: " + sk;

        Util.log("{0} :: {1} :: {2}", vessel, mr, s);
      }

      asteroidVessels.Clear();
      // Prevent list capacity from growing too much.
      if (asteroidVessels.Capacity > 16)
        asteroidVessels.TrimExcess();
    }

    public void readConfig(ConfigNode rootNode)
    {
    }

    /**
     * Post-load initialisation.
     */
    public void initialise()
    {
      string lastTextureName = "";

      foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture)
      {
        Texture2D texture = texInfo.texture;
        if (texture == null || !texture.name.StartsWith(Util.DIR))
          continue;

        // When a TGA loading fails, IndexOutOfBounds exception is thrown and GameDatabase gets
        // corrupted. The problematic TGA is duplicated in GameDatabase so that it also overrides
        // the preceding texture.
        if (texture.name == lastTextureName)
        {
          Util.log("Corrupted GameDatabase! Problematic TGA? {0}", texture.name);
        }
        // Add a head texture.
        else if (texture.name.StartsWith(DIR_ASTEROIDS))
        {
          string name = texture.name.Substring(DIR_ASTEROIDS.Length);
          if (name.EndsWith("NRM"))
          {
            string baseName = name.Substring(0, name.Length - 3);

            AsteroidMaterial material = materials.Find(m => m.name == baseName);
            if (material != null)
            {
              material.textureNRM = texture;
              Util.log("Mapped asteroid normal map \"{0}\" -> {1}", material.name, texture.name);
            }
          }
          else
          {
            AsteroidMaterial material = new AsteroidMaterial() { name = name, texture = texture };
            materials.Add(material);
            Util.log("Mapped asteroid texture \"{0}\" -> {1}", material.name, texture.name);
          }
        }

        lastTextureName = texture.name;
      }

      // Update EVA textures when a new Kerbal is created.
      GameEvents.onVesselCreate.Add(delegate(Vessel v) {
        if (v.vesselType == VesselType.SpaceObject)
          asteroidVessels.Add(v);
      });

      // Update EVA textures when a Kerbal comes into 2.4 km range.
      GameEvents.onVesselLoaded.Add(delegate(Vessel v) {
        if (v.vesselType == VesselType.SpaceObject)
          asteroidVessels.Add(v);
      });
    }

    public void resetScene()
    {
      asteroidVessels.Clear();
    }

    public void updateScene()
    {
      // IVA/EVA texture replacement pass. It is scheduled via event callbacks.
      if (HighLogic.LoadedSceneIsFlight)
      {
        if (asteroidVessels.Count != 0)
          replaceAsteroidSkins();
      }
    }
  }
}
#endif
