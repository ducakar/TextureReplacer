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
General replacement texture is of the form

    GameData/TextureReplacer/Default/[<subDir>]/<internalName>

where `<internalName>` is the texture's internal name in KSP (plus .png/.jpg/
.tga/.mbm extension, of course). The subdirectory is optional.

Examples:

    GameData/TextureReplacer/
      Default/kerbalHead              // Default Kerbal head
      Default/kerbalMain              // Default IVA suit (veteran/orange)
      Default/kerbalMainGrey          // Default IVA suit (standard/grey)
      Default/kerbalMainNRM           // Default IVA suit normal map
      Default/kerbalHelmetGrey        // Default IVA helmet
      Default/kerbalHelmetNRM         // Default IVA & EVA helmet normal map
      Default/EVAtexture              // Default EVA suit
      Default/EVAtextureNRM           // Default EVA suit normal map
      Default/EVAhelmet               // Default EVA helmet
      Default/EVAjetpack              // Default EVA jetpack
      Default/EVAjetpackNRM           // Default EVA jetpack normal map

      Default/GalaxyTex_NegativeX     // Skybox -X
      Default/GalaxyTex_PositiveX     // Skybox +X
      Default/GalaxyTex_NegativeY     // Skybox -Y
      Default/GalaxyTex_PositiveY     // Skybox +Y
      Default/GalaxyTex_NegativeZ     // Skybox -Z
      Default/GalaxyTex_PositiveZ     // Skybox +Z

      Default/moho00                  // Moho
      Default/moho01                  // Moho normal map
      Default/Eve2_00                 // Eve
      Default/Eve2_01                 // Eve normal map
      Default/evemoon100              // Gilly
      Default/evemoon101              // Gilly normal map
      Default/KerbinScaledSpace300    // Kerbin
      Default/KerbinScaledSpace401    // Kerbin normal map
      Default/NewMunSurfaceMapDiffuse // Mün
      Default/NewMunSurfaceMapNormals // Mün normal map
      Default/NewMunSurfaceMap00      // Minmus
      Default/NewMunSurfaceMap01      // Minmus normal map
      Default/Duna5_00                // Duna
      Default/Duna5_01                // Duna normal map
      Default/desertplanetmoon00      // Ike
      Default/desertplanetmoon01      // Ike normal map
      Default/dwarfplanet100          // Dres
      Default/dwarfplanet101          // Dres normal map
      Default/gas1_clouds             // Jool
      Default/cloud_normal            // Jool normal map
      Default/newoceanmoon00          // Laythe
      Default/newoceanmoon01          // Laythe normal map
      Default/gp1icemoon00            // Vall
      Default/gp1icemoon01            // Vall normal map
      Default/rockymoon100            // Tylo
      Default/rockymoon101            // Tylo normal map
      Default/gp1minormoon100         // Bop
      Default/gp1minormoon101         // Bop normal map
      Default/gp1minormoon200         // Pol
      Default/gp1minormoon201         // Pol normal map
      Default/snowydwarfplanet00      // Eeloo
      Default/snowydwarfplanet01      // Eeloo normal map

### Personalised Kerbal Textures ###
Personalised Kerbal textures are bound to a specific Kerbal. Texture names are
the same as for default textures, except that there is no `kerbalMain` texture
(`kerbalMainGrey` replaces both veteran and standard suits):

    GameData/TextureReplacer/
      CustomKerbals/<kerbalName>/kerbalHead       // Kerbal head
      CustomKerbals/<kerbalName>/kerbalMainGrey   // IVA suit
      CustomKerbals/<kerbalName>/kerbalMainNRM    // IVA suit normal map
      CustomKerbals/<kerbalName>/kerbalHelmetGrey // IVA helmet
      CustomKerbals/<kerbalName>/kerbalHelmetNRM  // IVA & EVA helmet normal map
      CustomKerbals/<kerbalName>/EVAtexture       // EVA suit
      CustomKerbals/<kerbalName>/EVAtextureNRM    // EVA suit normal map
      CustomKerbals/<kerbalName>/EVAhelmet        // EVA helmet
      CustomKerbals/<kerbalName>/EVAjetpack       // EVA jetpack
      CustomKerbals/<kerbalName>/EVAjetpackNRM    // EVA jetpack normal map

### Generic Kerbal Textures ###
Generic textures are assigned pseudo-randomly, based on the hash of a Kerbal's
name. This ensures the same textures are always assigned to the given Kerbal.

Generic head textures should be of the form

    GameData/TextureReplacer/GenericKerbals/[<subDir>]/kerbalHead*

i.e. head texture files should begin with `kerbalHead` and can optionally be
inside a subdirectory.

Suit textures' names are the same as for personalised Kerbals, but each suit
must reside in its own directory:

    GameData/TextureReplacer/
      GenericKerbals/<suit>/kerbalMainGrey   // IVA suit
      GenericKerbals/<suit>/kerbalMainNRM    // IVA suit normal map
      GenericKerbals/<suit>/kerbalHelmetGrey // IVA helmet
      GenericKerbals/<suit>/kerbalHelmetNRM  // IVA & EVA helmet normal map
      GenericKerbals/<suit>/EVAtexture       // EVA suit
      GenericKerbals/<suit>/EVAtextureNRM    // EVA suit normal map
      GenericKerbals/<suit>/EVAhelmet        // EVA helmet
      GenericKerbals/<suit>/EVAjetpack       // EVA jetpack
      GenericKerbals/<suit>/EVAjetpackNRM    // EVA jetpack normal map



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
* 0.14.1
    - better hashing and randomisation & other smaller code tweaks
    - improved instructions in README
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
