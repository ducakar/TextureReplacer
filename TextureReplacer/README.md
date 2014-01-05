![logo](http://i.imgur.com/ZljAQdy.jpg)

TextureReplacer
===============

[GitHub page](http://github.com/ducakar/VegaTech)

TextureReplacer is a plugin for Kerbal Space Program that replaces and improves
textures. It can replace Kerbal textures (face and suits), skybox, high-altitude
planet textures etc. Additionally, it also enforces trilinear texture filtering
(i.e. smooth transitions between mipmaps), generates mipmaps for PNGs and JPEGs
and compresses all non-compressed textures from `GameData/` that are found in
RAM (i.e. TGAs and sometimes MBMs).

Special thanks to:
* Tingle for Universe Replacer; studying his code helped me a lot while
  developing this plugin,
* rbray89 for TextureCompressor (a.k.a. Active Memory Reduction Mod) and Visual
  Enhancements where some code has been borrowed from and
* therealcrow999 for testing and benchmarking this plugin.


Instructions
------------
### General Textures ###
The textures should be put into `GameData/TextureReplacer/Default/` (or its
subdirectories) and have the same names as the internal KSP textures they should
replace (plus .png/.jpg/.tga/.mbm extensions, of course).

Here is a list of some internal KSP texture names:

    kerbalHead              // Default Kerbal head
    kerbalMain              // Default IVA suit (veteran)
    kerbalMainGrey          // Default IVA suit (standard)
    kerbalMainNRM           // Default IVA suit normal map
    kerbalHelmetGrey        // Default IVA helmet (veteran & standard)
    kerbalHelmetNRM         // Default IVA & EVA helmet normal map
    EVAtexture              // Default EVA suit
    EVAtextureNRM           // Default EVA suit normal map
    EVAhelmet               // Default EVA helmet
    EVAjetpack              // Default EVA jetpack
    EVAjetpackNRM           // Default EVA jetpack normal map

    GalaxyTex_NegativeX     // Skybox -X
    GalaxyTex_PositiveX     // Skybox +X
    GalaxyTex_NegativeY     // Skybox -Y
    GalaxyTex_PositiveY     // Skybox +Y
    GalaxyTex_NegativeZ     // Skybox -Z
    GalaxyTex_PositiveZ     // Skybox +Z

    moho00                  // Moho
    moho01                  // Moho normal map
    Eve2_00                 // Eve
    Eve2_01                 // Eve normal map
    evemoon100              // Gilly
    evemoon101              // Gilly normal map
    KerbinScaledSpace300    // Kerbin
    KerbinScaledSpace401    // Kerbin normal map
    NewMunSurfaceMapDiffuse // Mün
    NewMunSurfaceMapNormals // Mün normal map
    NewMunSurfaceMap00      // Minmus
    NewMunSurfaceMap01      // Minmus normal map
    Duna5_00                // Duna
    Duna5_01                // Duna normal map
    desertplanetmoon00      // Ike
    desertplanetmoon01      // Ike normal map
    dwarfplanet100          // Dres
    dwarfplanet101          // Dres normal map
    gas1_clouds             // Jool
    cloud_normal            // Jool normal map
    newoceanmoon00          // Laythe
    newoceanmoon01          // Laythe normal map
    gp1icemoon00            // Vall
    gp1icemoon01            // Vall normal map
    rockymoon100            // Tylo
    rockymoon101            // Tylo normal map
    gp1minormoon100         // Bop
    gp1minormoon101         // Bop normal map
    gp1minormoon200         // Pol
    gp1minormoon201         // Pol normal map
    snowydwarfplanet00      // Eeloo
    snowydwarfplanet01      // Eeloo normal map

### Personalised Kerbal Textures ###
Personalised Kerbal textures that are bound to a specific Kerbal name, should
reside in `GameData/TextureReplacer/CustomKerbals/<name>/`, where `<name>` is
the full name of some Kerbal.

The list of personalised Kerbal textures (note that there is no `kerbalMain`):

    kerbalHead              // Kerbal head
    kerbalMainGrey          // IVA suit
    kerbalMainNRM           // IVA suit normal map
    kerbalHelmetGrey        // IVA helmet
    kerbalHelmetNRM         // IVA & EVA helmet normal map
    EVAtexture              // EVA suit
    EVAtextureNRM           // EVA suit normal map
    EVAhelmet               // EVA helmet
    EVAjetpack              // EVA jetpack
    EVAjetpackNRM           // EVA jetpack normal map

### Generic Kerbal Textures ###
Generic head textures should be in `GameData/TextureReplacer/GenericKerbals/`
(or its subdirecoties) and have names begining with `kerbalHead`.

Textures for each generic suit should reside inside its own subdirectory in
`GameData/TextureReplacer/GenericKerbals/`. Naming scheme for suit textures is
the same as for personalised Kerbals (i.e. no `kerbalMain` texture).


Notes
-----
* Texture compression step is disabled if TextureCompressor is detected. Texture
  compression is then left to TextureCompressor which is a more specialised mod
  for that purpuse.
* The planet textures being replaced are the high-altitude textures, which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface, those textures are slowly interpolated into the high-resolution
  ones that cannot be replaced by this plugin.
* KSP never generates mipmaps for PNGs and JPEGs by itself. TextureReplacer
  fixes this by generating mipmaps for all PNGs and JPEGs in (subdirectories of)
  `TextureReplacer/` and those whose paths contain `/FX/`, `/Parts/` or
  `/Spaces/` (ignoring case). Other images are excluded to prevent generating
  mipmaps for UI icons used by various plugins and thus making them blurry when
  not using the full texture quality.
* KSP can only load TGAs with RGB colours.
* If you use Module Manager, make sure it is updated to the latest version.
  TextureReplacer is known to conflict with Module Manager 1.0.


Change Log
----------
* 0.14
    - added support for per-Kerbal suits
    - added generic (random) Kerbal head & suit textures
    - normal maps can be replaced without replacing the main textures
* 0.13
    - added support for per-Kerbal head textures
    - other textures can now be in any subdirectory of `TextureReplacer/` other
      than `CustomKerbals/`
    - RGBA/DTX5 textures are converted to RGB/DXT1 during mipmap generation if
      fully opaque, to fix KSP bug that always loads PNGs/JPEGs as transparent
    - `*/FX/*` and `*/Spaces/*` paths included in mipmap generation
    - some code refactoring and more comments
* 0.12.1
    - reverted change from 0.12 that made textures unreadable
* 0.12
    - added mipmap generation (for most textures)
    - textures are made unreadable after compression/mipmap generation
    - less verbose log output
* 0.11.1
    - fixed bug in 0.11 updating main menu every second frame
* 0.11
    - textures can be organised in subdirectories
    - fixed trilinear filtering not applied everywhere in 0.10
* 0.10.3
    - replacement is run on docking
* 0.10.2
    - prevent crashing when game database is corrupted
* 0.10.1
    - fixed 0.10 not loading any textures
* 0.10
    - set of texture names is not hard-coded any more
* 0.9
    - replacement is only run on vehicle switch and every 10 frames in main menu
    - texture compression is disabled when TextureCompressor mod is detected
    - rebuilt for KSP 0.23
* 0.8
    - merged TextureCompressor:
        + textures are compressed immediately when loaded, which should enable
          more textures to load before running out of memory on the 32-bit KSP
        + no more errors for non-readable textures
        + reports about memory savings in log
* 0.7.1
    - fixed normal maps
* 0.7
    - more verbose log output
    - some code refactoring
* 0.6.1
    - bug from 0.6 that caused slowdown fixed
* 0.6
    - texture replacement on vehicle switch is postponed for 1 frame
    - fixed skybox loading
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
    Copyright © 2014 Davorin Učakar
    Copyright © 2013 Ryan Bray

    Permission is hereby granted, free of charge, to any person obtaining a
    copy of this software and associated documentation files (the "Software"),
    to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense,
    and/or sell copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
    THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
    DEALINGS IN THE SOFTWARE.
