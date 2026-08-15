//! \file       ImageRCG.cs
//! \date       Sat Aug 15 2026
//! \brief      Rave RCG image format.
//
// Copyright (C) 2015 by morkt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//

using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Compression;

namespace GameRes.Formats
{
    [Export(typeof(ImageFormat))]
    public class ImageRCG : ImageFormat
    {
        public override string         Tag { get { return "RCG"; } }
        public override string Description { get { return "Rave RCG image format"; } }
        public override uint     Signature { get { return 0x31520018; } }
        public override bool      CanWrite { get { return false; } }

        public override ImageMetaData ReadMetaData (IBinaryStream stream)
        {
            stream.Position = 4;
            var meta = new ImageMetaData ();
            meta.BPP = 24;
            meta.Width = stream.ReadUInt16 ();
            meta.Height = stream.ReadUInt16 ();
            meta.OffsetX = 0;
            meta.OffsetY = 0;
            // Skip 8 bytes unknown fields
            stream.Position += 8;
            var compressed_size = stream.ReadUInt32 ();
            return compressed_size != stream.Length-24 ? null : meta;
        }

        public override ImageData Read (IBinaryStream stream, ImageMetaData info)
        {
            stream.Position = 20;
            var uncompressed_size = stream.ReadUInt32 ();
            var data = new byte[uncompressed_size];
            using (var zstream = new ZLibStream (stream.AsStream, CompressionMode.Decompress, true))
            {
                return uncompressed_size != zstream.Read (data, 0, (int)uncompressed_size)
                    ? null
                    : ImageData.Create (info, PixelFormats.Rgb24, null, data);
            }
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new System.NotImplementedException ("RCGFormat.Write not implemented");
        }
    }
}