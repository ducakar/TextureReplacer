![logo](http://i.imgur.com/0esQrqA.jpg)

TextureReplacer
===============
* [GitHub page](http://github.com/ducakar/TextureReplacer).
* [Forum page](http://forum.kerbalspaceprogram.com/threads/60961).
* [SpacePort page](http://kerbalspaceport.com/texturereplacer-0-21/).

TextureReplacer is a plugin for Kerbal Space Program that allows you to replace
stock textures and customise you Kerbals. More specifically, it can:
* replace stock textures with custom ones,
* set personalised head and suit textures for selected Kerbals,
* set persistent random head and suit textures for other Kerbals,
* set helmet visor texture,
* spawn Kerbals in IVA suit without helmet and jetpack in breathable atmosphere,
* generate missing mipmaps for PNG and JPEG model textures (to fix a KSP bug),
* compress uncompressed textures from `GameData/` and reduce RAM usage and
* change bilinear texture filter to trilinear to improve mipmap quality.

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
      Default/kerbalHead              // default Kerbal head
      Default/kerbalMain              // default IVA suit (veteran/orange)
      Default/kerbalMainGrey          // default IVA suit (standard/grey)
      Default/kerbalMainNRM           // default IVA suit normal map
      Default/kerbalHelmetGrey        // default IVA helmet
      Default/kerbalHelmetNRM         // default IVA & EVA helmet normal map
      Default/kerbalVisor             // default IVA helmet visor
      Default/EVAtexture              // default EVA suit
      Default/EVAtextureNRM           // default EVA suit normal map
      Default/EVAhelmet               // default EVA helmet
      Default/EVAvisor                // default EVA helmet visor
      Default/EVAjetpack              // default EVA jetpack
      Default/EVAjetpackNRM           // default EVA jetpack normal map

      Default/GalaxyTex_NegativeX     // skybox -X
      Default/GalaxyTex_PositiveX     // skybox +X
      Default/GalaxyTex_NegativeY     // skybox -Y
      Default/GalaxyTex_PositiveY     // skybox +Y
      Default/GalaxyTex_NegativeZ     // skybox -Z
      Default/GalaxyTex_PositiveZ     // skybox +Z

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
      Default/rockyMoon00             // Tylo
      Default/rockyMoon01             // Tylo normal map
      Default/gp1minormoon100         // Bop
      Default/gp1minormoon101         // Bop normal map
      Default/gp1minormoon200         // Pol
      Default/gp1minormoon201         // Pol normal map
      Default/snowydwarfplanet00      // Eeloo
      Default/snowydwarfplanet01      // Eeloo normal map

### Personalised Kerbal Textures ###
Personalised Kerbal textures are bound to a specific Kerbal. Texture names are
the same as for the default textures except there is no `kerbalMain` texture
(`kerbalMainGrey` replaces both veteran and standard suits):

    GameData/TextureReplacer/
      CustomKerbals/<kerbalName>/kerbalHead       // Kerbal head
      CustomKerbals/<kerbalName>/kerbalMainGrey   // IVA suit
      CustomKerbals/<kerbalName>/kerbalMainNRM    // IVA suit normal map
      CustomKerbals/<kerbalName>/kerbalHelmetGrey // IVA helmet
      CustomKerbals/<kerbalName>/kerbalHelmetNRM  // IVA & EVA helmet normal map
      CustomKerbals/<kerbalName>/kerbalVisor      // IVA helmet visor
      CustomKerbals/<kerbalName>/EVAtexture       // EVA suit
      CustomKerbals/<kerbalName>/EVAtextureNRM    // EVA suit normal map
      CustomKerbals/<kerbalName>/EVAhelmet        // EVA helmet
      CustomKerbals/<kerbalName>/EVAvisor         // EVA helmet visor
      CustomKerbals/<kerbalName>/EVAjetpack       // EVA jetpack
      CustomKerbals/<kerbalName>/EVAjetpackNRM    // EVA jetpack normal map

### Generic Kerbal Textures ###
Generic textures are assigned pseudo-randomly, based on the hash of a Kerbal's
name, which ensures the same textures are always assigned to a given Kerbal.

Generic head textures should be of the form

    GameData/TextureReplacer/GenericKerbals/[<subDir>]/kerbalHead*

i.e. head texture files should begin with `kerbalHead` and can optionally be
inside a subdirectory.

Suit textures' names are identical as for the personalised Kerbals but each suit
must reside in its own directory:

    GameData/TextureReplacer/
      GenericKerbals/<suit>/kerbalMainGrey   // IVA suit
      GenericKerbals/<suit>/kerbalMainNRM    // IVA suit normal map
      GenericKerbals/<suit>/kerbalHelmetGrey // IVA helmet
      GenericKerbals/<suit>/kerbalHelmetNRM  // IVA & EVA helmet normal map
      GenericKerbals/<suit>/kerbalVisor      // IVA helmet visor
      GenericKerbals/<suit>/EVAtexture       // EVA suit
      GenericKerbals/<suit>/EVAtextureNRM    // EVA suit normal map
      GenericKerbals/<suit>/EVAhelmet        // EVA helmet
      GenericKerbals/<suit>/EVAvisor         // EVA helmet visor
      GenericKerbals/<suit>/EVAjetpack       // EVA jetpack
      GenericKerbals/<suit>/EVAjetpackNRM    // EVA jetpack normal map

Heads are selected independently form suits so any head can be paired with any
of the suits. Such behaviour may not work as desired when one has
gender-specific suits. This is resolved by moving the female textures to
`GenericKermins/` while leaving the male ones in `GenericKerbals/`. The heads
will be paired only with the suits from the same root directory.

Each generic head (from either `GenericKerbals/` or `GenericKermins/`) has equal
chance of being selected.

### Configuration File ###
Configuration is located in

    GameData/TextureReplacer/PluginData/TextureReplacer/Config.cfg

One can edit it to:
* disable texture compression,
* disable mipmap generation,
* change list of substrings of paths where mipmap generation is allowed,
* disable atmospheric IVA suit or
* change air pressure required for atmospheric IVA suit.


Notes
-----
* Texture compression and mipmap generation are disabled if TextureCompressor is
  detected. Those features are then left to TextureCompressor which is a more
  specialised mod for that purpose.
* The planet textures being replaced are the high-altitude textures, which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface those textures are slowly interpolated into the high-resolution
  ones that cannot be replaced by this plugin.
* KSP never generates mipmaps for PNGs and JPEGs by itself. TextureReplacer
  fixes this by generating mipmaps for PNGs and JPEGs whose paths contain
  substrings specified in the configuration file. Other images are omitted to
  prevent generating mipmaps for UI icons used by various plugins and thus
  making them blurry when not using the full texture quality.
* If there is no IVA suit replacement the EVA suit texture is used for
  atmospheric EVAs. Helmet and jetpack are still removed though.
* KSP can only load TGAs with RGB colours.
* If you use Module Manager make sure it is updated to the latest version.
  TextureReplacer is known to conflict with Module Manager 1.0.


Known Issues
------------
* When using sfr mod, personalised/generic Kerbal IVA textures are often not set
  in transparent pods of non-active vessels.


Change Log
----------
* 1.0.1
    - disabled mipmap generation when TextureCompressor is detected
* 1.0
    - non-power-of-two textures are never compressed to avoid curruption
    - added option to configure paths where mipmaps may be generated
    - fixed regeression form 0.21 loading JPEGs as entirely black
* 0.21
    - fixed personalisation when a Kerbal is thrown from a seat
    - texture compression option is now respected when mipmaps are generated
    - some smaller code tweaks
* 0.20.1
    - fixed some external seat-related issues not properly setting personalised
      textures and spawning Kerbals without helmets in space
* 0.20
    - fixed personalised / randomised Kerbal textures not being set for teeth,
      tongue and jetpack arms and thrusters
    - some code polishing, updated comments, README etc.
* 0.19
    - added `GenericKermins/` directory to enable gender-specific suits
* 0.18.1
    - jetpack logic for atmospheric IVA suit changed:
      + EVA propellant is not removed any more
      + no more jetpack removal setting, it is always removed now
* 0.18
    - added proper visor texture setting (not just colour)
    - added (optional) jetpack removal for atmospheric IVA suit
    - atmospheric IVA suit is enabled by default
    - fixed personalised textures for Kerbals on external seats
    - fixed all issues of atmospheric IVA suit
    - fixed to work with sfr mod (mostly)
* 0.17
    - added configuration file
    - added support for setting helmet visor colour
    - added experimental feature for Kerbals on EVA to spawn in IVA suit without
      helmet when in breathable atmosphere (must be enabled in Config.cfg)
    - changed suit assignment logic for personalised Kerbals with only the head
      texture: they get a generic suit if one exists and default suit otherwise
* 0.16
    - more targeted (and faster) personalised texture replacement
    - fixed loosing personalised textures when boarding an external seat
    - fixed IVA suits resetting to stock when a Kerbal boards an external seat
* 0.15.1
    - made skybox replacement in the main menu more reliable
* 0.15
    - better logic for triggering texture replacement
    - full texture replacement is performed on each scene switch
    - only personalised Kerbal textures are updated during flight
    - much faster and more reliable way of detecting events that require
      personalised texture replacement
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
