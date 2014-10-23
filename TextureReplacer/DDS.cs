/*
 * Copyright © 2014 Davorin Učakar
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

#if TR_DDS

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TextureReplacer
{
  [DatabaseLoaderAttrib(new[] { "dds" })]
  public class DDS : DatabaseLoader<GameDatabase.TextureInfo>
  {
    private const int DDSD_MIPMAPCOUNT_BIT = 0x00020000;
    private const int DDPF_ALPHAPIXELS = 0x00000001;
    private const int DDPF_FOURCC = 0x00000004;
    private const int DDPF_RGB = 0x00000040;

    private static bool fourCCEquals(IList<byte> bytes, string s)
    {
      return bytes[0] == s[0] && bytes[1] == s[1] && bytes[2] == s[2] && bytes[3] == s[3];
    }

    private static GameDatabase.TextureInfo loadDDS(string path)
    {
      try
      {
        FileStream file = File.Open(path, FileMode.Open, FileAccess.Read);
        BinaryReader reader = new BinaryReader(file);

        if (!reader.BaseStream.CanRead)
          throw new IOException("Cannot read DDS file");

        // Implementation is based on specifications from
        // http://msdn.microsoft.com/en-us/library/windows/desktop/bb943991%28v=vs.85%29.aspx.
        // DX10 additions and deprecated features are not supported.
        byte[] magic = reader.ReadBytes(4);
        if (!fourCCEquals(magic, "DDS "))
          throw new IOException("Invalid DDS file");

        reader.ReadInt32();

        int flags = reader.ReadInt32();
        int height = reader.ReadInt32();
        int width = reader.ReadInt32();

        reader.ReadInt32();
        reader.ReadInt32();

        int nMipmaps = reader.ReadInt32();
        if ((flags & DDSD_MIPMAPCOUNT_BIT) == 0)
          nMipmaps = 1;

        reader.BaseStream.Seek(80, SeekOrigin.Begin);

        int pixelFlags = reader.ReadInt32();
        byte[] formatFourCC = reader.ReadBytes(4);
        int pixelSize = reader.ReadInt32() / 8;

        reader.BaseStream.Seek(128, SeekOrigin.Begin);

        TextureFormat format;
        bool isCompressed = false;
        bool isNormalMap = Path.GetFileNameWithoutExtension(path).EndsWith("NRM");

        if ((pixelFlags & DDPF_FOURCC) != 0)
        {
          isCompressed = true;

          if (fourCCEquals(formatFourCC, "DXT1"))
            format = TextureFormat.DXT1;
          else if (fourCCEquals(formatFourCC, "DXT5"))
            format = TextureFormat.DXT5;
          else
            throw new IOException("Unsupported DDS compression");
        }
        else if ((pixelFlags & DDPF_RGB) != 0)
        {
          format = (pixelFlags & DDPF_ALPHAPIXELS) != 0 ? TextureFormat.RGBA32 :
                                                          TextureFormat.RGB24;
        }
        else
        {
          throw new IOException("Invalid DDS pixelformat");
        }

        byte[] data = reader.ReadBytes((int) (reader.BaseStream.Length - 128));

        // Swap red and blue.
        if (!isCompressed)
        {
          int mipmapWidth = width;
          int mipmapHeight = height;
          int lineStart = 0;

          for (int i = 0; i < nMipmaps; ++i)
          {
            int lineSize = mipmapWidth * pixelSize;

            for (int y = 0; y < mipmapHeight; ++y, lineStart += lineSize)
            {
              int pos = lineStart;

              for (int x = 0; x < mipmapWidth; ++x, pos += pixelSize)
              {
                byte b = data[pos + 0];
                byte r = data[pos + 2];

                data[pos + 0] = r;
                data[pos + 2] = b;
              }
            }

            mipmapWidth = Math.Max(1, mipmapWidth / 2);
            mipmapHeight = Math.Max(1, mipmapHeight / 2);
          }
        }

        Texture2D texture = new Texture2D(width, height, format, nMipmaps > 1);
        texture.LoadRawTextureData(data);
        texture.Apply(false, true);

        return new GameDatabase.TextureInfo(texture, isNormalMap, false, isCompressed);
      }
      catch (IOException e)
      {
        Util.log("{0}: {1}", e.Message, path);
      }
      catch (Exception e)
      {
        Util.log("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
      }

      return null;
    }

    public override IEnumerator Load(UrlDir.UrlFile urlFile, FileInfo file)
    {
      obj = loadDDS(file.FullName);
      successful = obj != null;

      yield return null;
    }
  }
}

#endif
