TextureReplacer
===============

[GitHub page](http://github.com/ducakar/TextureReplacer)

TextureReplacer is a simple plugin for Kerbal Space Program that replaces
and improves textures. For now, replacing Kerbal (face and suits), skybox and
high-altitude planet textures is supported. Additionaly, this plugin enforces
trilinear texture filtering (i.e. smooth transitions between mipmaps) and
compresses uncompressed textures from `GameData/` that are loaded in RAM.

Special thanks to Tingle who created UniverseReplacer. Studying his code helped
me a lot when writing this plugin.


Direcory Layout
---------------
For now ony Kerbal textures can be replaced. The textures to be replaced should
be put into `GameData/TextureReplacer/Textures` and have the following names
(plus .tga/.png/.mbm extensions, of course):

    kerbalHead              // Kerbal head
    kerbalMain              // IVA suit (veteran)
    kerbalMainGrey          // IVA suit (standard)
    kerbalHelmetGrey        // IVA helmet
    EVAtexture              // EVA suit
    EVAhelmet               // EVA helmet
    EVAjetpack              // EVA jetpack
    kerbalMainNRM           // normal map for IVA suit (standard & veteran)
    kerbalHelmetNRM         // normal map for IVA & EVA helmet
    EVAtextureNRM           // normal map for EVA suit
    EVAjetpackNRM           // normal map for EVA jetpack

    GalaxyTex_NegativeX     // Skybox -X
    GalaxyTex_PositiveX     // Skybox +X
    GalaxyTex_NegativeY     // Skybox -Y
    GalaxyTex_PositiveY     // Skybox +Y
    GalaxyTex_NegativeZ     // Skybox -Z
    GalaxyTex_PositiveZ     // Skybox +Z

    suncoronanew            // Sun corona
    moho00                  // Moho
    Eve2_00                 // Eve
    evemoon100              // Gilly
    KerbinScaledSpace300    // Kerbin
    NewMunSurfaceMapDiffuse // Mün
    NewMunSurfaceMap00      // Minmus
    Duna5_00                // Duna
    desertplanetmoon00      // Ike
    dwarfplanet100          // Dres
    gas1_clouds             // Jool
    newoceanmoon00          // Laythe
    gp1icemoon00            // Vall
    rockymoon100            // Tylo
    gp1minormoon100         // Bop
    gp1minormoon200         // Pol
    snowydwarfplanet00      // Eeloo

* TGAs are recommended over PNGs since Unity fails to generate mipmaps for the
  latter.
* If only diffuse textures for Kerbals are replaced but not normal maps, the
  stock normal maps are kept.
* Replacing only normal map is not supported.
* The planet textures being replaced are the high-altitude textures which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface, those textures are slowly interpolated into high-resolution ones
  that cannot be replaced by this plugin.
* Replacing normal maps for planets is not supported since it doesn't make
  sense as a planet's normal map must match its heightmap data.


Change Log
----------
* 0.3
    - all uncompressed textures in `GameData/` are compressed on startup
    - normal maps for Kerbal textures can be replaced
    - planet textures can be replaced
* 0.2
    - enforcement of trilinear texture filter in place of bilinear
    - skybox textures can be replaced
* 0.1
    - initial version
    - Kerbal textures can be replaced


Licence
-------
Copyright © 2013 Davorin Učakar

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
