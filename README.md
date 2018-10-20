![logo](http://i.imgur.com/0esQrqA.jpg)

TextureReplacer
===============

* [Forum thread](http://forum.kerbalspaceprogram.com/index.php?/topic/96851-/)
* [GitHub page](https://github.com/ducakar/TextureReplacer)
* [Default texture pack](https://github.com/ducakar/TextureReplacer-pack)
* [CurseForge page](https://kerbal.curseforge.com/projects/texturereplacer)
* [SpaceDock page](https://spacedock.info/mod/1824/TextureReplacer)

TextureReplacer is a plugin for Kerbal Space Program that allows you to replace
stock textures and customise your Kerbals. More specifically, it can:

* replace stock textures with custom ones,
* assign personalised head and suit textures for each Kerbal,
* assign suits based on class and experience level,
* toggle between EVA suit and IVA suit without helmet in breathable atmosphere,
* add reflections to parts and helmet visors,
* generate missing mipmaps for PNG and JPEG model textures,
* compress uncompressed textures from `GameData/` to shrink textures in VRAM,
* unload textures from RAM after KSP finishes loading to reduce RAM usage and
* change bilinear texture filter to trilinear to improve mipmap quality.

Special thanks to:

* RangeMachine for contributing reflection shaders and maintaing the mod in my
  absence.
* rbray89 who contributed a reflective visor shader and for Active Texture
  Management and Visual Enhancements where some code has been borrowed from,
* Tingle for Universe Replacer; studying his code helped me a lot while
  developing this plugin,
* taniwha for KerbalStats that was optionally used by this plugin for gender
  determination and role-based suit assignment,
* Razchek and Starwaster for Reflection Plugin where I learnt how to implement
  reflections,
* sarbian for fixing an issue with non-mupliple-of-4 texture dimensions,
* therealcrow999 for testing and benchmarking this plugin,
* Ippo343 for contributing KSP-AVC configuration,
* JPLRepo for contributing DeepFreeze compatibility fixes,
* Proot, Scart91, Green Skull and others for creating texture packs and
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
      Default/kerbalHead                  // teeth and male head
      Default/kerbalHeadNRM               // teeth and male head normal map
      Default/kerbalGirl_06_BaseColor     // female head
      Default/kerbalGirl_06_BaseColorNRM  // female head normal map
      Default/kerbalMain                  // IVA suit (veteran/orange)
      Default/kerbalMainGrey              // IVA suit (standard/grey blue)
      Default/kerbalMainNRM               // IVA suit normal map
      Default/kerbalVisor                 // IVA helmet visor
      Default/EVAtexture                  // EVA suit
      Default/EVAtextureNRM               // EVA suit normal map
      Default/EVAvisor                    // EVA helmet visor
      Default/EVAjetpack                  // EVA jetpack
      Default/EVAjetpackNRM               // EVA jetpack normal map

      Default/NavBall                     // HUD & IVA NavBall

      Default/GalaxyTex_PositiveX         // skybox right face
      Default/GalaxyTex_NegativeX         // skybox left face
      Default/GalaxyTex_PositiveY         // skybox bottom face rotated for 180°
      Default/GalaxyTex_NegativeY         // skybox top face
      Default/GalaxyTex_PositiveZ         // skybox front face
      Default/GalaxyTex_NegativeZ         // skybox back face

      Default/moho00                      // Moho
      Default/moho01                      // Moho normal map
      Default/Eve2_00                     // Eve
      Default/Eve2_01                     // Eve normal map
      Default/evemoon100                  // Gilly
      Default/evemoon101                  // Gilly normal map
      Default/KerbinScaledSpace300        // Kerbin
      Default/KerbinScaledSpace401        // Kerbin normal map
      Default/NewMunSurfaceMapDiffuse     // Mün
      Default/NewMunSurfaceMapNormals     // Mün normal map
      Default/NewMunSurfaceMap00          // Minmus
      Default/NewMunSurfaceMap01          // Minmus normal map
      Default/Duna5_00                    // Duna
      Default/Duna5_01                    // Duna normal map
      Default/desertplanetmoon00          // Ike
      Default/desertplanetmoon01          // Ike normal map
      Default/dwarfplanet100              // Dres
      Default/dwarfplanet101              // Dres normal map
      Default/gas1_clouds                 // Jool
      Default/cloud_normal                // Jool normal map
      Default/newoceanmoon00              // Laythe
      Default/newoceanmoon01              // Laythe normal map
      Default/gp1icemoon00                // Vall
      Default/gp1icemoon01                // Vall normal map
      Default/rockyMoon00                 // Tylo
      Default/rockyMoon01                 // Tylo normal map
      Default/gp1minormoon100             // Bop
      Default/gp1minormoon101             // Bop normal map
      Default/gp1minormoon200             // Pol
      Default/gp1minormoon201             // Pol normal map
      Default/snowydwarfplanet00          // Eeloo
      Default/snowydwarfplanet01          // Eeloo normal map

It's also possible to replace textures from `GameData/` if one specifies
the full directory hierarchy:

    GameData/TextureReplacer/
      Default/Squad/Parts/Command/Mk1-2Pod/model000  // Mk1-2 pod texture
      Default/Squad/Parts/Command/Mk1-2Pod/model001  // Mk1-2 pod normal map

Note that all texture and directory names are case-sensitive!

### Reflections ###

Reflections are shown on visors of Kerbals' helmets and on parts that include
`TRReflection` module. There are two types of reflections: real and static.
Real reflections reflect the environment of a part while static reflections
reflect the skybox from `EnvMap/` directory:

    GameData/TextureReplacer/
      EnvMap/PositiveX         // fake skybox right face, vertically flipped
      EnvMap/NegativeX         // fake skybox left face, vertically flipped
      EnvMap/PositiveY         // fake skybox top face, vertically flipped
      EnvMap/NegativeY         // fake skybox bottom face, vertically flipped
      EnvMap/PositiveZ         // fake skybox front face, vertically flipped
      EnvMap/NegativeZ         // fake skybox back face, vertically flipped

Note that all textures must be squares and have the same dimensions that are
powers of two. Cube map textures are slow, so keep them as low-res as possible.

`TRReflection` part module can be used as in the following example that adds
reflections onto the windows of Mk1-2 pod:

    MODULE
    {
      name = TRReflection
      shader = Reflective/Bumped Diffuse
      colour = 0.5 0.5 0.5
      interval = 1
      meshes = FrontWindow SideWindow
    }

There are several parameters, all optional:

* `shader`: Most shaders should be automatically mapped to their reflective
  counterparts. In some cases, however, thare are no reflective version of a
  shader, so you will have to manually specify appropriate shader.
* `colour`: Reflection is pre-multiplied by this RGB value before added to the
  material. "0.5 0.5 0.5" by default.
* `interval`: Once in how many steps the reflection is updated. "1" by default.
* `meshes`: Space- and/or comma-sparated list of mesh names where to apply
  reflections. Reflection is applied to whole part if this parameter is empty or
  non-existent. You may find `logReflectiveMeshes` configuration option very
  helpful as it prints names of all meshes for each part with `TRReflection`
  module into your log.

One face of one reflection cube texture is updated every `reflectionInterval`
frames (2 by default, it can be changed in a configuration file), so each
reflective part has to be updated six times to update all six texture faces.
More reflective parts there are on the scene less frequently they are updated.
`interval` field on TRReflection module can lessen the update rate for a part;
e.g. `interval = 2` makes the part update half less frequently.

### Personalised Kerbal Textures ###

Heads and suits are assigned either manually or automatically (configured in the
GUI while configuration files can provide initial settings). "Random" assignment
of heads and suits is based on Kerbals' names, which ensures the same head/suit
is always assigned to a given Kerbal. Additionally, special per-class suit can
be set for each class.

Head textures reside inside `Skins/` directory, each in its own subdirectory:

    GameData/TextureReplacer/
      Skins/[<subDir>/]<skin>/kerbalHead     // Head texture
      Skins/[<subDir>/]<skin>/kerbalHeadNRM  // Normal map (optional)

for males and

    GameData/TextureReplacer/
      Skins/[<subDir>/]<skin>/kerbalGirl_06_BaseColor     // Head texture
      Skins/[<subDir>/]<skin>/kerbalGirl_06_BaseColorNRM  // Normal map (optional)

for females.

Suit textures' names are identical as for the default texture replacement except
that class level variants of suit texture are possible. If `kerbalMain` is not
present `kerbalMainGrey` is used for veterans. Each suit must reside inside its
own directory:

    GameData/TextureReplacer/
      Suits/[<subDir>/]<suit>/kerbalMain       // IVA suit (level 0 veteran)
      Suits/[<subDir>/]<suit>/kerbalMainGrey   // IVA suit (level 0 standard)
      Suits/[<subDir>/]<suit>/kerbalMainGrey1  // IVA suit (level 1)
      Suits/[<subDir>/]<suit>/kerbalMainGrey2  // IVA suit (level 2)
      Suits/[<subDir>/]<suit>/kerbalMainGrey3  // IVA suit (level 3)
      Suits/[<subDir>/]<suit>/kerbalMainGrey4  // IVA suit (level 4)
      Suits/[<subDir>/]<suit>/kerbalMainGrey5  // IVA suit (level 5)
      Suits/[<subDir>/]<suit>/kerbalMainNRM    // IVA suit normal map
      Suits/[<subDir>/]<suit>/kerbalVisor      // IVA helmet visor
      Suits/[<subDir>/]<suit>/EVAtexture       // EVA suit
      Suits/[<subDir>/]<suit>/EVAtexture1      // EVA suit (level 1)
      Suits/[<subDir>/]<suit>/EVAtexture2      // EVA suit (level 2)
      Suits/[<subDir>/]<suit>/EVAtexture3      // EVA suit (level 3)
      Suits/[<subDir>/]<suit>/EVAtexture4      // EVA suit (level 4)
      Suits/[<subDir>/]<suit>/EVAtexture5      // EVA suit (level 5)
      Suits/[<subDir>/]<suit>/EVAtextureNRM    // EVA suit normal map
      Suits/[<subDir>/]<suit>/EVAvisor         // EVA helmet visor
      Suits/[<subDir>/]<suit>/EVAjetpack       // EVA jetpack
      Suits/[<subDir>/]<suit>/EVAjetpackNRM    // EVA jetpack normal map

The level textures are optional. If a level texture is missing, the one from the
previous level is inherited.

### Configuration File ###

NOTE: All options that can be configured in the GUI are saved per-game and not
in the configuration files. Configuration files only provide initial settings
for those options.

Main/default configuration file:

    GameData/TextureReplacer/@Default.cfg

One can also use additional configuration files; configuration is merged from
all `*.cfg` files containing `TextureReplacer { ... }` as the root node. This
should prove useful to developers of texture packs so they can distribute
pack-specific head/suit assignment rules in a separate file. All `*.cfg` files
(including `@Default.cfg`) are processed in alphabetical order (the leading "@"
in `@Default.cfg` ensures it is processed first and overridden by subsequent
custom configuration files).

### Normal Maps ###

Unity uses "grey" normal maps (RGBA = YYYX) to minimise artefacts when applying
DXT5 texture compression on them. When a normal map has a "NRM" suffix Unity
converts it from RGB = XYZ ("blue") to RGBA = YYYX ("grey") normal map unless
it is in DDS format.

In short: you should supply "blue" normal maps when a texture has "NRM" suffix
and is in PNG format (JPEGs and TGAs are not recommended for normal maps) and
"grey" normal maps for textures in DDS format or without "NRM" suffix.

"Grey" normal maps can be created by saving the standard "blue" normal maps as
DDS with DXT5nm compression or by manually shuffling channels: RGBA -> GGGR.


Notes
-----

* Use DDS format for optimal RAM usage and loading times since DDS textures are
  not shadowed in RAM and can be pre-compressed and can have pre-built mipmaps.
* Try to keep dimensions of all textures powers of two.
* The planet textures being replaced are the high-altitude textures, which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface those textures are slowly interpolated into the high-resolution
  ones that cannot be replaced by this plugin.


Known Issues
------------

* For vintage suits, EVA suit is used for IVA.
* Reflections disable part highlighting along reflective surfaces.
* Only the top face of the atmospheric skybox is reflected.


Change Log
----------

* 3.4
    - unified navball textures as `Default/NavBall`
* 3.3
    - updated for KSP 1.5
    - updated default configuration files
    - changed suit texture names to reflect suit model changes
    - removed IVA helmet removal for safe situations
    - fixed transparency for non-textured visors
    - skin tweaks: removed teeth, softer eyelashes and fixed some artefacts
* 3.2
    - added Veteran and Vintage [suit] toggles in GUI
    - removed leagacy females option
    - removed static reflections
    - removed cabin-specific suits
    - removed support for old, non-existent mods from configuration
    - removed detection of ATM
    - only true/false values for `isCompressionEnabled`, `isMipmapGenEnabled`
      and `isUnloadingEnabled` options (no more ATM-specific `auto` option)
    - fixed visor shader
    - fixed visor texture personalisation
    - fixed vintage kerbal personalisation
* 3.1
    - renamed `Heads` directory to `Skins`
    - new `logKerbalHierarchy` option to dump structure of Kerbal models to log
    - fixed nav ball texture replacement
    - fixed kerbals missing in reflections
    - README & copyright updates
* 3.0
    - heads now reside in `Heads/<name>/<texture>`, named as stock textures
    - female heads are detected from file names rather than from config files
    - hide parachute pack when in IVA suit
    - big code refactorisation and cleanup
    - disabled visor reflections and nav ball replacement since both are broken
      due to changes in KSP (to be fixed)
* 2.4.13
    - fixed for 1.1 thanks to RangeMachine
    - disabled visor reflections because visor shader is broken
* 2.4.12
    - removed / replaced Kerbal Stuff links
    - possibly fixed a reflections-related crash
* 2.4.11
    - added alternate directories for general texture replacement
* 2.4.10
    - added compatibility for DeepFreeze
* 2.4.9
    - head and suit names shown in GUI
    - fixed inheritance of level textures for suits
* 2.4.8
    - fixed some contract-related issues by keeping agencies' flags loaded
    - fixed Reset to Defaults button resizing
* 2.4.7
    - suit's level textures are inherited from the previous level when missing
* 2.4.6
    - added GUI button to reset head/suit assignments to ones from config files
    - fixed male -> female material copy mixing IVA and EVA textures on females
    - fixed IVA helmet personalisation when helmets are hidden on scene start
* 2.4.5
    - fixing female model is more consistent, female helmet normal map enabled
    - (hopefully) fixed a rare toolbar icon crash that breaks space centre scene
    - the top quad of atmospheric skybox is not rendered any more
    - re-enabled texture compression, mipmap generation and unloading
* 2.4.4
    - disabling spawning in IVA suits also disables "Toggle EVA Suit" in
      Kerbal's context menu
    - disabled texture compression, mipmap generation and unloading by default,
      these functions will be removed in the next major release
    - fixed radial attachment and click ignoring for reflective parts
* 2.4.3
    - all Kerbals' teeth now use `Default/kerbalHead` texture and are not
      personalised any more to solve the female teeth texturing problem
* 2.4.2
    - fixed white visors when not using visor texture
* 2.4.1
    - shaders on Kerbals are now changed to make them consistent between males
      and females and fix bumpmapping and specular lighting for female suits
    - removed consecutive suit assignment
    - removed (now redundant) suit assignment setting, since a random suit is
      automatically used when the class suit is not set
    - fixed legacy females feature for tourists
    - fixed random suit assignment when the class suit is not available
    - fixed default veteran suit assignment for Valentina
    - fixed skybox reflection
* 2.4
    - updated for KSP 1.0
    - updated personalisation to work with stock female Kerbals
    - added `forceLegacyFemales` option to convert all females to use male
      models but female textures (pre-1.0 behaviour)
    - removed gender detection from names and `femaleNames` config option
    - fixed all issues with game database reloading
    - fixed mipmap generation for navballs
* 2.3.1
    - further improved IVA personalisation; it should now work with all mods
    - addition/removal of toolbar button is now done correctly
* 2.3
    - major code refactoring
    - removed most of code that had to run per-frame, mechanisms provided by
      Unity/KSP exploited instead
    - IVA personalisation is now triggered via a component bound to internal
      models instead of multiple event methods, which makes it simpler, more
      reliable and without need for other mods to manually call event methods
    - general texture replacement is now completely reliable and in one pass
    - per-frame reflection updater only runs when real reflections are enabled
    - GUI is only loaded during the space centre scene
* 2.2.6
    - new and more robust method for hiding meshes during reflection generation
    - fixed `isVisorReflectionEnabled` being ignored for real reflections
    - fixed issue with non-multiple-of-4 texture dimensions (thanks to sarbian)
* 2.2.5
    - reverted immediate texture unloading
* 2.2.4
    - fixed TRReflection making airlocks non-clickable
    - made texture unloading immediate during general replacement
* 2.2.3
    - really fixed crash that may occur when a reflective part is removed
* 2.2.2
    - fixed crash that may occur when a reflective part is removed
* 2.2.1
    - updated default configuration for Kopernicus and Kittopia
* 2.2
    - added real reflections, generated from environment in real time
    - added level-specific textures for suits
    - added new GUI option to switch between reflection types
    - added compatibility for Reflection Plugin
      * TRReflection now recognises some Reflection Plugin parameters
      * MM patch in `Extras/MM_ReflectionPluginWrapper.cfg`
    - erased default `EnvMap/*` textures, they should come bundled with skyboxes
    - embedded visor shader into DLL
    - simplified and optimised code for IVA personalisations
    - reverted default value for `colour` in TRReflection to "0.5 0.5 0.5"
* 2.1.2
    - GUI now shows generic heads and suits instead of just printing "Generic"
    - fixed crash with duplicated entries in `CustomKerbals`
    - fixed experience suits for non-stock traits when `name` != `title`
* 2.1.1
    - GUI now adds buttons for assigning suits to non-stock experience traits
    - experience suits in GUI now show at the end of the roster
    - added `skinningQuality` config option
    - changed default value for `colour` in TRReflection module to "1 1 1"
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

### Plugin: MIT

    Copyright © 2013-2018 Davorin Učakar
    Copyright © 2016 RangeMachine
    Copyright © 2013 rbray89

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

### Skins: CC BY 4.0

    This pack can be modified and distributed under the terms of CC BY 4.0
    licence. The original author is Sylith, with additional modifications by
    shaw and IOI-655362.
