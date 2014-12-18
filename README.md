![logo](http://i.imgur.com/0esQrqA.jpg)

TextureReplacer
===============

* [Forum page](http://forum.kerbalspaceprogram.com/threads/60961)
* [GitHub page](http://github.com/ducakar/TextureReplacer)
* [Kerbal Stuff page](https://kerbalstuff.com/mod/150/TextureReplacer)
* [CurseForge page](http://kerbal.curseforge.com/plugins/220217-texturereplacer)

TextureReplacer is a plugin for Kerbal Space Program that allows you to replace
stock textures and customise your Kerbals. More specifically, it can:

* replace stock textures with custom ones,
* set personalised head and suit textures for selected Kerbals,
* set persistent random head and suit textures for other Kerbals,
* set suits based on Kerbals' experiences,
* set cockpit-specific IVA suits,
* remove IVA helmets in safe situations,
* spawn Kerbals in IVA suit without helmet and jetpack in breathable atmosphere,
* add normal map for head texture,
* add helmet visor texture,
* enable (fake) reflection on helmet visors and parts,
* generate missing mipmaps for PNG and JPEG model textures (to fix a KSP bug),
* compress uncompressed textures from `GameData/` and reduce RAM usage,
* change bilinear texture filter to trilinear to improve mipmap quality and
* unload textures from RAM after KSP finishes loading.

Special thanks to:

* rbray89 who contributed a reflective visor shader and for Active Texture
  Management and Visual Enhancements where some code has been borrowed from,
* Tingle for Universe Replacer; studying his code helped me a lot while
  developing this plugin,
* taniwha for KerbalStats that is optionally used by this plugin for gender
  determination and role-based suit assignment,
* Razchek and Starwaster for Reflection Plugin where I learnt how to implement
  reflections,
* sarbian for DDS loader where I learnt how to create an image loader for
  GameDatabase,
* therealcrow999 for testing and benchmarking this plugin,
* Ippo343 for contributing KSP-AVC configuration,
* Proot, Green Skull and others for making texture packs for this plugin and
* Sylith and Scart91 for giving others permissions to make derivatives of their
  texture packs.


Instructions
------------

### General Textures ###

General replacement textures are of the form

    GameData/TextureReplacer/Default/<internalName>

where `<internalName>` is the texture's internal name in KSP or path of a
texture inside `GameData/` (plus .dds/.png/.jpg/.tga extension, of course).

Examples:

    GameData/TextureReplacer/
      Default/kerbalHead               // default Kerbal head
      Default/kerbalHeadNRM            // default Kerbal head normal map
      Default/kerbalMain               // default IVA suit (veteran/orange)
      Default/kerbalMainGrey           // default IVA suit (standard/grey)
      Default/kerbalMainNRM            // default IVA suit normal map
      Default/kerbalHelmetGrey         // default IVA helmet
      Default/kerbalHelmetNRM          // default IVA & EVA helmet normal map
      Default/kerbalVisor              // default IVA helmet visor
      Default/EVAtexture               // default EVA suit
      Default/EVAtextureNRM            // default EVA suit normal map
      Default/EVAhelmet                // default EVA helmet
      Default/EVAvisor                 // default EVA helmet visor
      Default/EVAjetpack               // default EVA jetpack
      Default/EVAjetpackNRM            // default EVA jetpack normal map

      Default/HUDNavBall               // HUD NavBall
      Default/IVANavBall               // IVA NavBall, horizontally flipped

      Default/GalaxyTex_PositiveX      // skybox right face
      Default/GalaxyTex_NegativeX      // skybox left face
      Default/GalaxyTex_PositiveY      // skybox bottom face, rotated for 180°
      Default/GalaxyTex_NegativeY      // skybox top face
      Default/GalaxyTex_PositiveZ      // skybox front face
      Default/GalaxyTex_NegativeZ      // skybox back face

      Default/moho00                   // Moho
      Default/moho01                   // Moho normal map
      Default/Eve2_00                  // Eve
      Default/Eve2_01                  // Eve normal map
      Default/evemoon100               // Gilly
      Default/evemoon101               // Gilly normal map
      Default/KerbinScaledSpace300     // Kerbin
      Default/KerbinScaledSpace401     // Kerbin normal map
      Default/NewMunSurfaceMapDiffuse  // Mün
      Default/NewMunSurfaceMapNormals  // Mün normal map
      Default/NewMunSurfaceMap00       // Minmus
      Default/NewMunSurfaceMap01       // Minmus normal map
      Default/Duna5_00                 // Duna
      Default/Duna5_01                 // Duna normal map
      Default/desertplanetmoon00       // Ike
      Default/desertplanetmoon01       // Ike normal map
      Default/dwarfplanet100           // Dres
      Default/dwarfplanet101           // Dres normal map
      Default/gas1_clouds              // Jool
      Default/cloud_normal             // Jool normal map
      Default/newoceanmoon00           // Laythe
      Default/newoceanmoon01           // Laythe normal map
      Default/gp1icemoon00             // Vall
      Default/gp1icemoon01             // Vall normal map
      Default/rockyMoon00              // Tylo
      Default/rockyMoon01              // Tylo normal map
      Default/gp1minormoon100          // Bop
      Default/gp1minormoon101          // Bop normal map
      Default/gp1minormoon200          // Pol
      Default/gp1minormoon201          // Pol normal map
      Default/snowydwarfplanet00       // Eeloo
      Default/snowydwarfplanet01       // Eeloo normal map

It's also possible to replace textures from `GameData/` if one specifies
the full directory hierarchy:

    GameData/TextureReplacer/
      Default/Squad/Parts/Command/Mk1-2Pod/model000  // Mk1-2 pod texture
      Default/Squad/Parts/Command/Mk1-2Pod/model001  // Mk1-2 pod normal map

Note that all texture and directory names are case-sensitive!

### Reflections ###

Reflections are shown on visors of Kerbal's helmets and on parts that include
`TRReflection` module, e.g.:

    MODULE
    {
      name = TRReflection
      colour = 0.5 0.5 0.5
      meshes = OuterShell FrontWindow SideWindow
    }

`colour` ("0.5 0.5 0.5" by default) and `meshes` (empty by default) parameters
are optional. Reflection texture is multiplied by `colour` and applied on the
meshes from `meshes` list or on whole part if `meshes` is empty or missing.

Environment cube map texture for reflections is included with the plugin:

    GameData/TextureReplacer/
      EnvMap/PositiveX         // fake skybox right face, vertically flipped
      EnvMap/NegativeX         // fake skybox left face, vertically flipped
      EnvMap/PositiveY         // fake skybox top face, vertically flipped
      EnvMap/NegativeY         // fake skybox bottom face, vertically flipped
      EnvMap/PositiveZ         // fake skybox front face, vertically flipped
      EnvMap/NegativeZ         // fake skybox back face, vertically flipped

Note that all textures must be quares and have the same dimensions that are
powers of two. Cube map textures are slow, so keep them as low-res as possible.

### Personalised Kerbal Textures ###

Heads and suits are assigned either manually or automatically (configured in the
GUI while configuration files only provide initial settings). Pseudo-random
assignment is based on a Kerbal's name, which ensures the same head/suit is
always assigned to a given Kerbal. Additionally it is also possible to assign
suits consecutively, based on rooster index, or based on Kerbal experience.

Head textures reside inside `Heads/` directory (and its subdirectories) and have
arbitrary names. Normal maps are optional. To provide a normal map, name it the
same as the head texture but add "NRM" suffix.

    GameData/TextureReplacer/
      Heads/[<subDir>/]<head>     // Head texture
      Heads/[<subDir>/]<head>NRM  // Normal map for <head> (optional)

Suit textures' names are identical as for the default texture replacement except
that there is no `kerbalMain` texture (`kerbalMainGrey` replaces both). Each
suit must reside in its own directory:

    GameData/TextureReplacer/
      Suits/[<subDir>/]<suit>/kerbalMainGrey    // IVA suit
      Suits/[<subDir>/]<suit>/kerbalMainNRM     // IVA suit normal map
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey  // IVA helmet
      Suits/[<subDir>/]<suit>/kerbalHelmetNRM   // IVA & EVA helmet normal map
      Suits/[<subDir>/]<suit>/kerbalVisor       // IVA helmet visor
      Suits/[<subDir>/]<suit>/EVAtexture        // EVA suit
      Suits/[<subDir>/]<suit>/EVAtextureNRM     // EVA suit normal map
      Suits/[<subDir>/]<suit>/EVAhelmet         // EVA helmet
      Suits/[<subDir>/]<suit>/EVAvisor          // EVA helmet visor
      Suits/[<subDir>/]<suit>/EVAjetpack        // EVA jetpack
      Suits/[<subDir>/]<suit>/EVAjetpackNRM     // EVA jetpack normal map

For generic Kerbals, heads are selected independently form suits so any head can
be paired with any of the suits and each head has an equal chance of being
selected.

See configuration file contents for how to configure head/suit assignment rules.

### Configuration File ###

Main configuration file:

    GameData/TextureReplacer/@Default.cfg

One can also use additional configuration files; configuration is merged from
all `*.cfg` files that contain `TextureReplacer { ... }` as the root node. This
should prove useful to developers of texture packs so they can distribute
pack-specific head/suit assignment rules in a separate file. All `*.cfg` files
(including `@Default.cfg`) are processed in alphabetical order (the reason
behind the leading "@" in `@Default.cfg` is that it is processed first and can
be overridden by subsequent custom configuration files).

### Normal Maps ###

Unity uses "grey" normal maps (RGBA = YYYX) to avoid artefacts when applying
DXT5 texture compression on them. When a normal map has a "NRM" suffix Unity
converts it from RGB = XYZ ("blue") to RGBA = YYYX ("grey") normal map unless
DDSLoader is used to load it.

In short: you should supply "blue" normal maps when image has "NRM" suffix
and is in PNG format (JPEGs and TGAs are not recommended for normal maps)
and "grey" normal map when DDS format is used or the texture does not have
"NRM" suffix.

"Grey" normal maps can be created by saving the standard "blue" normal map
as a DDS with DXT5nm compression or by manually shuffling its colour
channels as RGBA -> GGGR.


Notes
-----

* Use DDS format (requires DDSLoader) for optimal RAM usage and loading times
  since DDS textures are not shadowed in RAM and can be pre-compressed and can
  have pre-generated mipmaps.
* TGA and JPEG images are not suitable for normal maps.
* Try to keep dimensions of all textures powers of two.
* KSP can only load TGAs with RGB or RGBA colours. Paletteised 256-colour TGAs
  cause corruptions in the game database!
* By default, texture compression and mipmap generation are handled by ATM when
  it is present rather than by TextureReplacer.
* KSP never generates mipmaps for PNGs and JPEGs. TextureReplacer fixes this by
  generating mipmaps under paths specified in the configuration file. Other
  images are omitted to avoid making UI icons of various plugins blurry when not
  using the full texture quality.
* The planet textures being replaced are the high-altitude textures, which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface those textures are slowly interpolated into the high-resolution
  ones that cannot be replaced by this plugin.


Known Bugs
----------

* Cabin-specific IVA suits don't persist through scene switches while on EVA.
* IVA visor texture is applied to EVA Kerbals if game database is reloaded while
  in the main menu.


Change Log
----------

* 2.1.1
    - support for non-stock experience traits
    - experience suits in GUI now show at the end of the roster
    - fixed `CustomKerbals` config settings being ignored
    - fixed persistence of cabin suits, but not through scene switches
    - fixed duplicated TextureReplacer after GameDatabase reloads
    - fixed toolbar button to survive GameDatabase reload
    - code cleanup
* 2.1
    - added several new options to GUI
    - cabin suits are now persistent until embarking another capsule
    - changed experiance-based suit assignment to work with stock exp. traits
      rather than KerbalStats exp.
    - completely changed settings for experiance-based suits
    - removed KerbalStats support
    - removed `headMultiplier` and `suitMultiplier` settings
* 2.0.2
    - added option to hide the toolbar icon
    - dead Kerbals are omitted from the GUI and when saving settings
    - assigned and missing Kerbals in the GUI are cyan and yellow respectively
* 2.0.1
    - improved IVA replacement; it is now immediate and completely reliable
    - fixed inheritance of `Default/kerbalHeadNRM` to heads without normal maps
* 2.0
    - added GUI for configuring per-Kerbal heads and suits to Space Centre
    - per-Kerbal head and suit assignment is saved for each game separately
    - `CustomKerbals` from config files are now used only as initial settings
    - fixed assignment of `Default/kerbalMain` veteran suit to Jeb, Bill and Bob
    - fixed atmospheric IVA suits when using stock suits
    - fixed `CustomKerbals` overriding when `headTex` or `suitDir` is missing
    - less verbose log output
* 1.10.2
    - fixed visor shader loading
* 1.10.1
    - changed the way how texture replacements for KerbalEVA are triggered
    - further optimised personalised IVA replacements
    - the state of EVA suit is saved for Kerbals on EVA
    - when conditions for IVA suit are not met any more, a Kerbal on EVA
      automatically wears EVA suit
    - removed `isToggleEvaSuitEnabled` config option, the EVA PartModule is now
      mandatory for texture replacement to work
* 1.10
    - added "Toggle EVA Suit" option to Kerbal context menu
    - optimised Kerbal personalisation by removing many redundant replacements
    - old textures are really unloaded when replaced by textures from `Default/`
    - updated default configuration for new mods
* 1.9.2
    - fixed crash when there is no navball replacement texture
* 1.9.1
    - updated configuration to cover more mods
    - omitted navballs from mipmap generation
    - fixed crash when there are no Kerbal suits
    - fixed crash when environment map textures are not readable
* 1.9
    - added integration with KerbalStats (optional) for gender determination and
      experience-based suit assignment
    - new `commanderSuit`, `pilotSuit`, `scientistSuit`, `passengerSuit` config
      options for experience-based suits
    - new `headMultiplier` and `suitMultiplier` options for tweaking
      randomisation algorithm for head and suit assignment
    - added support for setting normal map of the default head
    - fixed IVA helmet removal when using the default suit
    - fixed a crash which might occur when rebuilding game database
* 1.8.1
    - fixed crash when environment map is missing
* 1.8
    - added TRReflection part module for visor-like reflections on parts
    - added support for NavBall texture replacement
    - added a configurable list of bodies with breathable atmospheres (since not
      all atmospheres with oxygen are breathable, e.g. Laythe)
    - added extras: low-res environment map textures, Module Manager script to
      add the new reflections to some stock parts
* 1.7.4
    - better handling of DDS files
    - updated documentation
    - converted environment map textures to PNG format
    - some code cleanups
* 1.7.3
    - added `logTextures` config option to dump material/texture names
    - added `TextureReplacer.version` file for KSP-AVC (thanks to Ippo343)
* 1.7.2
    - improved head/suit randomisation algorithm
    - fixed Kerbal personalisation for stock crew transfer
    - rebuilt for KSP 0.25
* 1.7.1
    - default configuration tweaked to detect female names better
* 1.7
    - gender is determined form name
    - fixed merging of duplicated nodes in configuration files
    - fixed `@Default.cfg` to be up-to-date with other mods
    - code cleanup
* 1.6.1
    - rebuilt for KSP 0.24
* 1.6
    - changed the way how internal spaces are treated, it should now work fine
      with transparent pods using JSITransparentPod and sfr mods
    - helmets are also removed in pre-launch to handle rovers & stuff correctly
    - tab characters can be used as list separators in configuration files
* 1.5.10
    - IVA helmets are removed in safe situations (landed/splashed, in orbit)
* 1.5.2
    - improved options for configuring texture unloading
    - fixed spawning in IVA suit on Laythe and its orbit when leaving ext. seat
    - removed ATM configuration, normal maps cannot be configured correctly
* 1.5.1
    - fixed unnecessary texture replacement passes on scene switches
    - fixed default config for Lazor System and KSI MFDs compatibility
* 1.5
    - textures are now (mostly) unloaded from RAM just before the main menu
    - added configuration option to prevent textures from being unloaded
    - changed compression and mipmap generation logic
    - changed configuration file options for mipmap generation; RE supported
    - changed general texture replacement to time-based
    - reverted to the old way of removing (some) meshes to prevent helmets or
      eyes from re-appearing when using certain mods
    - added compatibility for ATM
* 1.4.2
    - added option to remove eyes for certain heads
    - original texture's parameters are kept on replacement
    - fixed several minor issues in reading configuration files
* 1.4.1
    - better environment map textures, now with stars
    - changed default `visorReflectionColour` to `1 1 1` to keep the original
      environment map colour
    - added `GENERIC` option for custom Kerbals' head and suit settings
    - some improvements in log messages
    - fixed trilinear filter not being applied to personalised Kerbal textures
    - fixed texture clamp mode not being set for `Default/kerbalHead`
* 1.4
    - configuration files use `.cfg` extension again to avoid conflicts with ATM
    - all configuration files are merged, all options can now be in any file
    - re-added female-specific heads/suits functionality
    - fixed issues with jetpack texture replacement for 0.23.5
    - fixed several crashes
    - built against 0.23.5
* 1.3.4
    - added support for normal maps for head textures
    - jetpack thruster jets are now (really) hidden for atmospheric suit
* 1.3.3
    - fixed jetpack flag showing for atmospheric suit in 0.23.5
    - headlight flares are now hidden for atmospheric suit
* 1.3.2
    - added ability to replace arbitrary textures from `GameData/`, directory
      hierarchy inside `Default/` matters now
    - fixed trilinear filter that was not applied to normal maps
* 1.3.1
    - added cabin-specific IVA suits
    - fixed head/suit exclusions when using multiple config files
* 1.3
    - new directory layout:
        + removed `CustomKerbals/`, `GenericKerbals/` and `GenericKermins/`
        + all heads are in `Heads/`
        + all suits are in `Suits/`
        + `Config.cfg` moved to TR's root directory as `TextureReplacer.tcfg`
    - assignment of head and suit textures is now defined in `*.tcfg`
    - fixed IVA replacement that failed for suits sometimes when docking
* 1.2.2
    - changed texture wrapping mode for Kerbal textures to "clamp", which
      eliminates the green patch at the top of heads
    - changed default setting for mipmap generation to `always`, since TC
      doesn't generate mipmaps for TR textures (and many others) by default
    - fixed personalisation on ext. seats that are not attached to the root part
    - refactored reflection code
* 1.2.1
    - fixed visor shader, reflection is now correctly blended onto visor
    - changed environment map for reflections
* 1.2
    - added support for custom visor shader
    - added reflective shader for visor that supports transparency
    - fixed environment map textures
    - code refactored, split into multiple smaller classes
* 1.1
    - added fake reflections for helmet visor
    - added new modes for assigning suits
    - added several new options in configuration file:
        + `auto`, `always` & `never` options for texture compression and mipmap
          generation instead of `true` & `false`
        + `fallbackSuit` setting that specifies whether the default or a generic
          suit is used for a custom Kerbal with only a head texture
        + `suitAssignment` setting to control how generic suits are assigned
        + reflection colour for visor
* 1.0.1
    - disabled mipmap generation when TextureCompressor is detected
* 1.0
    - non-power-of-two textures are never compressed to avoid corruption
    - added option to configure paths where mipmaps may be generated
    - fixed regression form 0.21 loading JPEGs as entirely black
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

Plugin code and binaries are distributed under the following licence:

Copyright © 2013-2014 Davorin Učakar, Ryan Bray

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


Environment map textures are based on skybox from Proot's KSP Renaissance
Compilation and are distributed under the terms of CC BY-NC-SA 4.0 licence.
