![logo](http://i.imgur.com/ZljAQdy.jpg)

TextureReplacer
===============

[GitHub page](http://github.com/ducakar/VegaTech)

TextureReplacer is a plugin for Kerbal Space Program that replaces and improves
textures. It can replace Kerbal textures (face and suits), skybox, high-altitude
planet textures etc. Additionally, it also enforces trilinear texture filtering
(i.e. smooth transitions between mipmaps) and compresses all non-compressed
textures from `GameData/` that are found in RAM.

Special thanks to:
* Tingle for Universe Replacer; studying his code helped me a lot while
  developing this plugin,
* rbray89 for TextureCompressor (a.k.a. Active Memory Reduction Mod) which has
  been merged into TextureReplacer and
* therealcrow999 for testing and benchmarking this plugin.


Directory Layout
----------------
The textures should be put into `GameData/TextureReplacer/Textures` and have the
same names as the internal KSP textures they should replace (plus .tga/.png/.mbm
extensions, of course).

Here is a list of some internal KSP texture names:

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


Notes
-----
* TGAs are recommended over PNGs since Unity fails to generate mipmaps for the
  latter.
* When you convert PNGs to TGAs, make sure you change indexed and greyscale
  images to RGB. Only TGAs with RGB colours work in Unity.
* If only diffuse textures are replaced for Kerbals, the stock normal maps are
  kept.
* Replacing only the normal map texture is not supported.
* The planet textures being replaced are the high-altitude textures, which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface, those textures are slowly interpolated into the high-resolution
  ones that cannot be replaced by this plugin.
* If you use Module Manager, make sure it is updated to the latest version.
  TextureReplacer is known to conflict with the version 1.0.


Change Log
----------
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
    Copyright © 2013 Davorin Učakar
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
