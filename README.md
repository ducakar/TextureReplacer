TextureReplacer
===============

[GitHub page](http://github.com/ducakar/TextureReplacer)

TextureReplacer is a simple plugin for Kerbal Space Program that replaces
specific textures. For now, only replacing Kerbal (face and suits) and skybox
textures is supported.

The second thing it does is enforcing trilinear texture filter for all textures
that originally use bilinear filter.

Special thanks to Tingle who created UniverseReplacer. Studying his code helped
me a lot when writing this plugin.


Direcory Layout
---------------
For now ony Kerbal textures can be replaced. The textures to be replaced should
be put into `GameData/TextureReplacer/Textures` and have the following names:

    kerbalHead           // Kerbal head
    kerbalMain           // IVA Suit (Veteran)
    kerbalMainGrey       // IVA Suit
    kerbalHelmetGrey     // IVA Helmet
    EVAtexture           // EVA Suit
    EVAhelmet            // EVA Helmet
    EVAjetpack           // EVA Jetpack

    GalaxyTex_NegativeX  // Skybox -X
    GalaxyTex_PositiveX  // Skybox +X
    GalaxyTex_NegativeY  // Skybox -Y
    GalaxyTex_PositiveY  // Skybox +Y
    GalaxyTex_NegativeZ  // Skybox -Z
    GalaxyTex_PositiveZ  // Skybox +Z

TGAs are recommended since Unity fails to generate mipmaps for PNGs.


Change Log
----------
0.1
  - initial version
  - Kerbal textures replaceability

0.2
  - enforcement of trilinear texture filter in place of bilinear
  - skybox textures replaceability


Licence
-------
Copyright © 2013 Davorin Učakar

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
