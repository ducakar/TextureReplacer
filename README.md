![logo](http://i.imgur.com/ZljAQdy.jpg)

TextureReplacer
===============

[GitHub page](http://github.com/ducakar/TextureReplacer)

TextureReplacer is a simple plugin for Kerbal Space Program that replaces
and improves textures. For now, replacing Kerbal (face and suits), skybox and
high-altitude planet textures is supported. Additionaly, this plugin enforces
trilinear texture filtering (i.e. smooth transitions between mipmaps) and
compresses uncompressed textures from `GameData/` that are loaded in RAM.

Special thanks to Tingle for Universe Replacer and therealcrow999 for testing
and benchmarks of my plugin. Studying Tingle's Universe Replacer code helped me
a lot when writing this plugin.


Directory Layout
----------------
The textures to be replaced should be put into
`GameData/TextureReplacer/Textures` and have the following names (plus
.tga/.png/.mbm extensions, of course):

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
* 0.5
    - replacement is run every 16 frames in all non-flight scenes
    - comments added to the code
* 0.4
    - replacement is only run on startup and on vehicle switch
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

This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from
the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software in
   a product, an acknowledgement in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
