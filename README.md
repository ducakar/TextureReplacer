# TextureReplacer #

![logo](http://i.imgur.com/0esQrqA.jpg)

* [Forum thread](http://forum.kerbalspaceprogram.com/index.php?/topic/96851-/)
* [GitHub page](https://github.com/ducakar/TextureReplacer)
* [Default texture pack](https://github.com/ducakar/TextureReplacer-pack)
* [CurseForge page](https://kerbal.curseforge.com/projects/texturereplacer)
* [SpaceDock page](https://spacedock.info/mod/1824/TextureReplacer)

TextureReplacer is a plugin for Kerbal Space Program that allows you to replace
stock textures and customise your Kerbals. More specifically, it can:

* replace stock textures with custom ones,
* assign personalised skin and suit textures for each Kerbal,
* assign suits based on class and experience level,
* toggle between EVA suit and IVA suit without helmet in breathable atmosphere,
* add reflections to parts and helmet visors and
* change bilinear texture filter to trilinear to improve mipmap quality.

Special thanks to:

* RangeMachine for contributing reflection shaders and maintaining the mod in my
  absence.
* rbray89 who contributed a reflective visor shader and for Active Texture
  Management and Visual Enhancements where some code has been borrowed from,
* Tingle for Universe Replacer; studying his code helped me a lot while
  developing this plugin,
* taniwha for KerbalStats that was optionally used by this plugin for gender
  determination and role-based suit assignment,
* Razchek and Starwaster for Reflection Plugin where I learnt how to implement
  reflections,
* sarbian for fixing an issue with non-multiple-of-4 texture dimensions,
* therealcrow999 for testing and benchmarking this plugin,
* Ippo343 for contributing KSP-AVC configuration,
* JPLRepo for contributing DeepFreeze compatibility fixes,
* Proot, Scart91, Green Skull and others for creating texture packs and
* Sylith and Scart91 for giving others permissions to make derivatives of their
  texture packs.

## Instructions ##

### General Textures ###

General replacement textures are of the form

    GameData/**/TextureReplacer/Default/<internalName>

where `<internalName>` is the texture's internal name in KSP or path of a
texture inside `GameData/` (plus `.dds`/`.png`/`.jpg`/`.tga` extension, of
course).

Examples:

    GameData/**/TextureReplacer/Default/
      kerbalHead                      // teeth and male head
      kerbalHeadNRM                   // teeth and male head normal map
      kerbalGirl_06_BaseColor         // female head
      kerbalGirl_06_BaseColorNRM      // female head normal map

      orangeSuite_diffuse             // default IVA suit (veteran)
      paleBlueSuite_diffuse           // default IVA suit (non-veteran)
      orangeSuite_normal              // default IVA & EVA suit normal map
      whiteSuite_diffuse              // default EVA suit
      EVAjetpack                      // default EVA suit jetpack
      EVAjetpackNRM                   // default EVA suit jetpack normal map
      EVAjetpackEmmisive              // default EVA suit jetpack emissive

      me_suit_difuse_orange           // vintage IVA suit (veteran)
      me_suit_difuse_low_polyBrown    // vintage IVA suit (non-veteran)
      kerbalMainNRM                   // vintage IVA & EVA suit normal map
      me_suit_difuse_blue             // vintage EVA suit
      EVAjetpackscondary              // vintage suit jetpack
      EVAjetpacksecondary_N           // vintage suit jetpack normal map

      futureSuit_diffuse_whiteOrange  // future IVA suit (veteran)
      futureSuit_diffuse_whiteBlue    // future IVA suit (non-veteran)
      futureSuitMainNRM               // future IVA suit normal map
      futureSuit_diffuse_orange       // future EVA suit
      futureSuitMainNRM               // future EVA suit normal map
      futureSuit_emissive             // future EVA suit emissive
      ksp_ig_jetpack_diffuse          // future EVA suit jetpack
      EVAjetpackNRM                   // future EVA suit normal map

      kerbalVisor                     // IVA helmet visor
      EVAvisor                        // EVA helmet visor

      cargoContainerPack_diffuse      // EVA cargo backpack
      cargoContainerPack_NRM          // EVA cargo backpack normal map
      cargoContainerPack_emissive     // EVA cargo backpack emissive

      backpack_Diff                   // EVA parachute backpack
      backpack_NM                     // EVA parachute backpack normal map
      canopy_Diff                     // EVA parachute canopy
      canopy_NR                       // EVA parachute canopy normal map

      NavBall                         // HUD & IVA NavBall
      NavBallEmissive                 // HUD & IVA NavBall emissive

      GalaxyTex_PositiveX             // skybox right face
      GalaxyTex_NegativeX             // skybox left face
      GalaxyTex_PositiveY             // skybox bottom face rotated by 180°
      GalaxyTex_NegativeY             // skybox top face
      GalaxyTex_PositiveZ             // skybox front face
      GalaxyTex_NegativeZ             // skybox back face

      moho00                          // Moho
      moho01                          // Moho normal map
      Eve2_00                         // Eve
      Eve2_01                         // Eve normal map
      evemoon100                      // Gilly
      evemoon101                      // Gilly normal map
      KerbinScaledSpace300            // Kerbin
      KerbinScaledSpace401            // Kerbin normal map
      NewMunSurfaceMapDiffuse         // Mün
      NewMunSurfaceMapNormals         // Mün normal map
      NewMunSurfaceMap00              // Minmus
      NewMunSurfaceMap01              // Minmus normal map
      Duna5_00                        // Duna
      Duna5_01                        // Duna normal map
      desertplanetmoon00              // Ike
      desertplanetmoon01              // Ike normal map
      dwarfplanet100                  // Dres
      dwarfplanet101                  // Dres normal map
      gas1_clouds                     // Jool
      cloud_normal                    // Jool normal map
      newoceanmoon00                  // Laythe
      newoceanmoon01                  // Laythe normal map
      gp1icemoon00                    // Vall
      gp1icemoon01                    // Vall normal map
      rockyMoon00                     // Tylo
      rockyMoon01                     // Tylo normal map
      gp1minormoon100                 // Bop
      gp1minormoon101                 // Bop normal map
      gp1minormoon200                 // Pol
      gp1minormoon201                 // Pol normal map
      snowydwarfplanet00              // Eeloo
      snowydwarfplanet01              // Eeloo normal map

It's also possible to replace textures from `GameData/` if one specifies
the full directory hierarchy:

    GameData/**/TextureReplacer/Default/
      Squad/Parts/Command/Mk1-2Pod/model000  // Mk1-2 pod texture
      Squad/Parts/Command/Mk1-2Pod/model001  // Mk1-2 pod normal map

Note that all texture and directory names are case-sensitive!

### Reflections ###

Reflections are shown on visors of Kerbals' helmets and on parts that include
`TRReflection` module that can be used like in the following example adding
reflections onto the windows of Mk1-2 pod:

    MODULE
    {
      name = TRReflection
      shader = Reflective/Bumped Diffuse
      colour = 1.0 1.0 1.0
      interval = 1
      meshes = FrontWindow SideWindow
    }

There are several parameters, all optional:

* `shader`: Most shaders should be automatically mapped to their reflective
  counterparts. In some cases, however, there are no reflective version of a
  shader, so you will have to manually specify appropriate shader.
* `colour`: Reflection is pre-multiplied by this RGB value before added to the
  material. `1.0 1.0 1.0` by default.
* `interval`: Once in how many steps the reflection is updated. `1` by default.
* `meshes`: Space- and/or comma-separated list of mesh names where to apply
  reflections. Reflection is applied to whole part if this parameter is empty or
  non-existent. You may find `logReflectiveMeshes` configuration option very
  helpful as it prints names of all meshes for each part with `TRReflection`
  module into your log.

One face of one reflection cube texture is updated every `reflectionInterval`
frames (`2` by default, it can be changed in a configuration file), so each
reflective part has to be updated six times to update all six texture faces.
More reflective parts there are on the scene less frequently they are updated.
`interval` field on TRReflection module can lessen the update rate for a part;
e.g. `interval = 2` makes the part update half less frequently.

### Personalised Kerbal Textures ###

Skins and suits are assigned either manually or automatically (configured in the
GUI while configuration files can provide initial settings). "Random" assignment
of skins and suits is based on Kerbals' names, which ensures the same skin/suit
is always assigned to a given Kerbal. Additionally, special per-class suit can
be set for each class.

Skin textures reside inside `Skins/` directory, each in its own subdirectory:

    GameData/**/TextureReplacer/Skins/[<subDir>/]<skin>/
      kerbalHead    // Head texture
      eyeballLeft   // Left eyeball
      eyeballRight  // Right eyeball
      pupilLeft     // Left pupil
      pupilRight    // Right pupil

Directory name should have an extension containing letters that convey
additional information about that skin:

* 'm' male,
* 'f' female,
* 'e' hide eyes (for skins that have eyes drawn on texture),
* 'x' excluded from automatic assignment, can only be assigned manually.

E.g. skin `GameData/TextureReplacer/Skins/MySkin.fx` will be assigned to females
only and only if explicitly selected (by `CustomKerbals` in a config file or
later in the in-game GUI).

Suit textures' names are identical as for the default texture replacement except
that class level variants of suit texture are possible. Each suit must reside
inside its own directory.

    GameData/**/TextureReplacer/Suits/[<subDir>/]<suit>/
      kerbalMain                   // IVA suit (veteran)
      kerbalMainGrey               // IVA suit (level 0)
      kerbalMainGrey1              // IVA suit (level 1)
      kerbalMainGrey2              // IVA suit (level 2)
      kerbalMainGrey3              // IVA suit (level 3)
      kerbalMainGrey4              // IVA suit (level 4)
      kerbalMainGrey5              // IVA suit (level 5)
      kerbalMainNRM                // IVA suit normal map
      kerbalVisor                  // IVA helmet visor

      EVAtexture                   // EVA suit (level 0)
      EVAtexture1                  // EVA suit (level 1)
      EVAtexture2                  // EVA suit (level 2)
      EVAtexture3                  // EVA suit (level 3)
      EVAtexture4                  // EVA suit (level 4)
      EVAtexture5                  // EVA suit (level 5)
      EVAtextureNRM                // EVA suit normal map
      futureSuit_emissive          // EVA suit emissive (future suit only)
      EVAvisor                     // EVA helmet visor
      EVAjetpack                   // EVA jetpack
      EVAjetpackNRM                // EVA jetpack normal map
      EVAjetpackEmmisive           // EVA jetpack emissive

      cargoContainerPack_diffuse   // EVA cargo backpack
      cargoContainerPack_NRM       // EVA cargo backpack normal map
      cargoContainerPack_emissive  // EVA cargo backpack emissive

      backpack_Diff                // EVA parachute backpack
      backpack_NM                  // EVA parachute backpack normal map
      canopy_Diff                  // EVA parachute canopy
      canopy_NR                    // EVA parachute canopy normal map

The veteran and level textures are optional. If a level texture is missing the
one from the previous level is inherited. If the veteran texture is present it
is used for all levels on veterans.

Directories may optionally have a suffix containing any comination of the
following letters (in an arbitrary order):

* 'm' to make the suit male-only,
* 'f' to make the suit female-only,
* 'x' exclude form automatic assignment,
* 'V' if containing textures for vintage suit model (Making History),
* 'F' if containing textures for future suit model (Breaking Ground).

### Configuration File ###

***NOTE:*** All options that can be configured in the GUI are saved per-game and
not in the configuration files. Configuration files only provide initial
settings for those options.

Main/default configuration file:

    GameData/TextureReplacer/@Default.cfg

One can also use additional configuration files; configuration is merged from
all `*.cfg` files containing `TextureReplacer { ... }` as the root node. This
should prove useful to developers of texture packs so they can distribute
pack-specific skin/suit assignment rules in a separate file. All `*.cfg` files
(including `@Default.cfg`) are processed in alphabetical order (the leading `@`
in `@Default.cfg` ensures it is processed first and overridden by subsequent
custom configuration files).

### Normal Maps ###

Unity uses _grey_ normal maps (RGBA = YYYX) to minimise artefacts when applying
DXT5 texture compression on them. When a normal map has a `NRM` suffix Unity
converts it from RGB = XYZ (_blue_) to RGBA = YYYX (_grey_) normal map unless
it is in DDS format.

In short: you should supply _blue_ normal maps when a texture has `NRM` suffix
and is in PNG format (JPEGs and TGAs are not recommended for normal maps) and
_grey_ normal maps for textures in DDS format or without `NRM` suffix.

_Grey_ normal maps can be created by saving the standard _blue_ normal maps as
DDS with DXT5nm compression or by manually shuffling channels: RGBA -> GGGR.

## Notes ##

* Use DDS format for optimal RAM usage and loading times since DDS textures are
  not shadowed in RAM and can be pre-compressed and can have pre-built mipmaps.
* Try to keep dimensions of all textures powers of two.
* The planet textures being replaced are the high-altitude textures, which are
  also used in the map mode and in the tracking station. When getting closer to
  the surface those textures are slowly interpolated into the high-resolution
  ones that cannot be replaced by this plugin.

## Known Issues ##

* Switching between default, vintage and future suit models:
  - TextureReplacer will only detect the switch done via clothes hanger on
    second flight scene load.
  - EVA models on existing flights will only be updated on second flight scene
    load when switched via TextureReplacer's GUI.
* Head normal maps:
  - [KSP bug] Head meshes have mismatched tangents or binormals along lines
    where head texture is "stitched" together.
* Reflections:
  - Reflective shaders do not support part highlighting.
  - Only the top face of the atmospheric skybox is reflected.
  - Visor reflections cause jaw twitching.
* Issues with other mods:
  - Clouds from EVE are not reflected at certain altitudes.
