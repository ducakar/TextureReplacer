# Change Log #

* 4.2.2
  - update for KSP 1.9
  - fix some comments in `@Default.cfg`
* 4.1.1
  - fix emissive texture for helmet and neck ring
* 4.1
  - do not hide the current part when rendering reflections to avoid subtle
    physics glitches and jaw twitching
  - change default `reflectionResolution` to 256 and `reflectionInterval` to 4
  - remove `isVisorReflectionEnabled` setting
  - fix atmosphere reflection
  - fix visor shader to reflect terrain
* 4.0.3
  - update reflection shader to match implementations published on Unity forum
  - clean up some code leftovers in GUI to fix a crash
* 4.0.2
  - fix IVA navball texture replacement
* 4.0.1
  - update Unity subproject and recompile shaders for Unity 2019.2
  - fix a crash when missing a DLC
* 4.0
  - update for KSP 1.8.x
  - add support for "future" (Breaking Ground) suits
  - add support for skin and suit directory suffixes
  - add ability to replace cargo and parachute backpack and parachute canopy
    textures for each suit
  - add support for replacing emissive textures
  - add option for exporting loaded textures as PNG in GUI
  - re-add normal map support for heads
  - rename `hideParachuteBackpack` setting to `hideBackpack` as it hides both
    parachute and cargo backpacks now
  - GUI only iterates through skins and suits of the selected type and gender
  - remove neck rings from future suits in IVA
  - remove `excludedSkins`, `excludedSuits`, `eyelessSkins` and `femaleSuits`
    config options; these should be configured via directory suffixes now
  - remove `skinningQuality` setting
  - disable reflections by default
  - code cleanup, reorganisation and switch to C# 8.0
* 3.7
  - add option to hide parachute backpacks
  - add vintage IVA kerbals to logKerbalHierarchy
  - remove head normal maps because of defective normals on head meshes
* 3.6.2
  - fix pupil textures
* 3.6.1
  - tune reflection settings to mitigate ragdoll jitter and performance issues
  - fix jetpack, visor and visor reflection when spawning in EVA suit
* 3.6
  - leverage stock helmet removal system for managing atmospheric IVA suit
  - remove `IsAtmSuitEnabled`, `atmSuitPressure` and `atmSuitBodies` config
    options, these things are now managed by the stock helmet removal system
  - remove atmospheric suit option from GUI
* 3.5.1
  - adjusted default reflection colour to (1.0, 1.0, 1.0)
  - fixed trilinear texture filter not applying on some textures
  - fixed for KSP 1.6
* 3.5
  - textures are now searched in `GameData/**/TextureReplacer/...`, not just in
    `GameData/TextureReplacer/...`.
  - add eye texture replacements
  - apply specular shader to all Kerbals' eyes
  - adjusted reflection colour to (0.7, 0.7, 0.7)
  - fixed visors in IVA
  - fixed level suits
  - fixed some head texture artefacts
* 3.4
  - removed texture compression, mipmap generation and unloading
  - unified navball textures as `Default/NavBall`
  - added vintage EVA models and material names to `logKerbalHierarchy` dumps
  - fixed visor texture replacement and visor reflections for females
  - fixed veteran suit replacement for atmospheric EVA
* 3.3
  - updated for KSP 1.5
  - updated default configuration files
  - changed suit texture names to reflect suit model changes
  - removed IVA helmet removal for safe situations
  - added normal maps to `logKerbalHierarchy` dumps
  - fixed transparency for non-textured visors
  - skin tweaks: removed teeth, softer eyelashes and fixed some artefacts
* 3.2
  - added Veteran and Vintage suit toggles in GUI
  - removed legacy females option
  - removed static reflections
  - removed cabin-specific suits
  - removed support for old, non-existent mods from configuration
  - removed detection of ATM
  - only true/false values for `isCompressionEnabled`, `isMipmapGenEnabled` and
    `isUnloadingEnabled` options (no more ATM-specific `auto` option)
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
  - disabling spawning in IVA suits also disables "Toggle EVA Suit" in Kerbal's
    context menu
  - disabled texture compression, mipmap generation and unloading by default,
    these functions will be removed in the next major release
  - fixed radial attachment and click ignoring for reflective parts
* 2.4.3
  - all Kerbals' teeth now use `Default/kerbalHead` texture and are not
    personalised any more to solve the female teeth texturing problem
* 2.4.2
  - fixed white visors when not using visor texture
* 2.4.1
  - shaders on Kerbals are now changed to make them consistent between males and
    females and fix bumpmapping and specular lighting for female suits
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
  - added `forceLegacyFemales` option to convert all females to use male models
    but female textures (pre-1.0 behaviour)
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
  - completely changed settings for experience-based suits
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
  - new `headMultiplier` and `suitMultiplier` options for tweaking randomisation
    algorithm for head and suit assignment
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
  - added extras: low-res environment map textures, Module Manager script to add
    the new reflections to some stock parts
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
  - reverted to the old way of removing (some) meshes to prevent helmets or eyes
    from re-appearing when using certain mods
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
    * removed `CustomKerbals/`, `GenericKerbals/` and `GenericKermins/`
    * all heads are in `Heads/`
    * all suits are in `Suits/`
    * `Config.cfg` moved to TR's root directory as `TextureReplacer.tcfg`
  - assignment of head and suit textures is now defined in `*.tcfg`
  - fixed IVA replacement that failed for suits sometimes when docking
* 1.2.2
  - changed texture wrapping mode for Kerbal textures to "clamp", which
    eliminates the green patch at the top of heads
  - changed default setting for mipmap generation to `always`, since TC doesn't
    generate mipmaps for TR textures (and many others) by default
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
    * `auto`, `always` & `never` options for texture compression and mipmap
      generation instead of `true` & `false`
    * `fallbackSuit` setting that specifies whether the default or a generic
      suit is used for a custom Kerbal with only a head texture
    * `suitAssignment` setting to control how generic suits are assigned
    * reflection colour for visor
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
    * EVA propellant is not removed any more
    * no more jetpack removal setting, it is always removed now
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
    * textures are compressed immediately when loaded, which should enable more
      textures to load before running out of memory on the 32-bit KSP
    * no more errors for non-readable textures
    * reports about memory savings in log
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
