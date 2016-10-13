![logo](https://cloud.githubusercontent.com/assets/11577601/19346143/808bc27c-914a-11e6-8bc9-4d80a0c08ae2.png)

TextureReplacer
===============

* [Forum thread](http://forum.kerbalspaceprogram.com/index.php?/topic/96851-11-texturereplacer-2413-442016)
* [GitHub page](http://github.com/RangeMachine/TextureReplacer)

TextureReplacer is a plugin for Kerbal Space Program that allows you to replace
stock textures and customise your Kerbals. More specifically, it can:

* replace stock textures with custom ones,
* assign personalised head and suit textures for each Kerbal,
* assign suits based on class and experience level,
* remove IVA helmets in safe situations,
* toggle between EVA suit and IVA suit without helmet in breathable atmosphere,
* add reflections to parts and helmet visors,
* generate missing mipmaps for PNG and JPEG model textures,
* compress uncompressed textures from `GameData/` to shrink textures in VRAM,
* unload textures from RAM after KSP finishes loading to reduce RAM usage and
* change bilinear texture filter to trilinear to improve mipmap quality.

Special thanks to:

* rbray89 who contributed a reflective visor shader and for Active Texture
  Management and Visual Enhancements where some code has been borrowed from,
* Tingle for Universe Replacer; studying his code helped me a lot while
  developing this plugin,
* taniwha for KerbalStats that was optionally used by this plugin for gender
  determination and role-based suit assignment,
* Razchek and Starwaster for Reflection Plugin where I learnt how to implement
  reflections,
* sarbian for fixing an issue with non-mupliple-of-4 texture dimensions,
* Ippo343 for contributing KSP-AVC configuration,
* JPLRepo for contributing DeepFreeze compatibility fixes,
* therealcrow999 for testing and benchmarking this plugin,
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
      Default/kerbalMainGrey              // IVA suit (standard/grey)
      Default/kerbalMainNRM               // IVA suit normal map
      Default/kerbalHelmetGrey            // IVA helmet
      Default/kerbalHelmetNRM             // IVA & EVA helmet normal map
      Default/kerbalVisor                 // IVA helmet visor
      Default/EVAtexture                  // EVA suit
      Default/EVAtextureNRM               // EVA suit normal map
      Default/EVAhelmet                   // EVA helmet
      Default/EVAvisor                    // EVA helmet visor
      Default/EVAjetpack                  // EVA jetpack
      Default/EVAjetpackNRM               // EVA jetpack normal map

      Default/HUDNavBall                  // HUD NavBall
      Default/IVANavBall                  // IVA NavBall, horizontally flipped

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

Note that all textures must be quares and have the same dimensions that are
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

Head textures reside inside `Heads/` directory (and its subdirectories) and have
arbitrary names. Normal maps are optional. To provide a normal map, name it the
same as the head texture but add "NRM" suffix.

    GameData/TextureReplacer/
      Heads/[<subDir>/]<head>     // Head texture
      Heads/[<subDir>/]<head>NRM  // Normal map for <head> (optional)

Suit textures' names are identical as for the default texture replacement except
that there is no `kerbalMain` texture (`kerbalMainGrey` replaces both) and class
level variants of suit and helmet textures are possible. Each suit must reside
inside its own directory:

    GameData/TextureReplacer/
      Suits/[<subDir>/]<suit>/kerbalMainGrey     // IVA suit
      Suits/[<subDir>/]<suit>/kerbalMainGrey1    // IVA suit (level 1)
      Suits/[<subDir>/]<suit>/kerbalMainGrey2    // IVA suit (level 2)
      Suits/[<subDir>/]<suit>/kerbalMainGrey3    // IVA suit (level 3)
      Suits/[<subDir>/]<suit>/kerbalMainGrey4    // IVA suit (level 4)
      Suits/[<subDir>/]<suit>/kerbalMainGrey5    // IVA suit (level 5)
      Suits/[<subDir>/]<suit>/kerbalMainNRM      // IVA suit normal map
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey   // IVA helmet
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey1  // IVA helmet (level 1)
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey2  // IVA helmet (level 2)
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey3  // IVA helmet (level 3)
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey4  // IVA helmet (level 4)
      Suits/[<subDir>/]<suit>/kerbalHelmetGrey5  // IVA helmet (level 5)
      Suits/[<subDir>/]<suit>/kerbalHelmetNRM    // IVA & EVA helmet normal map
      Suits/[<subDir>/]<suit>/kerbalVisor        // IVA helmet visor
      Suits/[<subDir>/]<suit>/EVAtexture         // EVA suit
      Suits/[<subDir>/]<suit>/EVAtexture1        // EVA suit (level 1)
      Suits/[<subDir>/]<suit>/EVAtexture2        // EVA suit (level 2)
      Suits/[<subDir>/]<suit>/EVAtexture3        // EVA suit (level 3)
      Suits/[<subDir>/]<suit>/EVAtexture4        // EVA suit (level 4)
      Suits/[<subDir>/]<suit>/EVAtexture5        // EVA suit (level 5)
      Suits/[<subDir>/]<suit>/EVAtextureNRM      // EVA suit normal map
      Suits/[<subDir>/]<suit>/EVAhelmet          // EVA helmet
      Suits/[<subDir>/]<suit>/EVAhelmet1         // EVA helmet (level 1)
      Suits/[<subDir>/]<suit>/EVAhelmet2         // EVA helmet (level 2)
      Suits/[<subDir>/]<suit>/EVAhelmet3         // EVA helmet (level 3)
      Suits/[<subDir>/]<suit>/EVAhelmet4         // EVA helmet (level 4)
      Suits/[<subDir>/]<suit>/EVAhelmet5         // EVA helmet (level 5)
      Suits/[<subDir>/]<suit>/EVAvisor           // EVA helmet visor
      Suits/[<subDir>/]<suit>/EVAjetpack         // EVA jetpack
      Suits/[<subDir>/]<suit>/EVAjetpackNRM      // EVA jetpack normal map

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
"grey" normal map for textures in DDS format or without "NRM" suffix.

"Grey" normal maps can be created by saving the standard "blue" normal map as a
DDS with DXT5nm compression or by manually shuffling its channels RGBA -> GGGR.


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

* Atmospheric skybox is not reflected.
* Reflections disable part highlighting.
* Clouds from EVE are only reflected when on/near the ground or over 160 km.
* Clouds from EVE Overhaul are not correctly reflected.
* Cabin-specific IVA suits don't persist through scene switches while on EVA.


Licence
-------

Copyright © 2013-2016 Davorin Učakar, Ryan Bray, RangeMachine

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
