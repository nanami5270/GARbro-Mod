//! \file       ImageIPG.cs
//! \brief      SALA ONE IPG image format.
//

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;

namespace GameRes.Formats.Artemis
{
    [Export(typeof(ImageFormat))]
    public class IpgFormat : ImageFormat
    {
        public override string         Tag { get { return "IPG"; } }
        public override string Description { get { return "SALA ONE PSP IPG image format"; } }
        public override uint     Signature { get { return 0x30475049; } } // 'IPG0'

        public IpgFormat ()
        {
            Extensions = new string[] { "ipg" };
        }

        public override ImageMetaData ReadMetaData (IBinaryStream file)
        {
            file.Position = 4;
            uint width  = file.ReadUInt32();
            uint height = file.ReadUInt32();
            if (0 == width || 0 == height || width > 0x10000 || height > 0x10000)
                return null;
            if (file.Length < 12 + (long)width*height*4)
                return null;
            return new ImageMetaData { Width = width, Height = height, BPP = 32 };
        }

        public override ImageData Read (IBinaryStream file, ImageMetaData info)
        {
            var meta = (ImageMetaData)info;
            int pixels_size = (int)meta.Width * (int)meta.Height * 4;
            file.Position = 12;
            var pixels = new byte[pixels_size];
            int total = 0;
            while (total < pixels_size)
            {
                int read = file.Read (pixels, total, pixels_size - total);
                if (0 == read)
                    throw new EndOfStreamException();
                total += read;
            }
            // source is RGBA, convert to BGRA in place
            for (int i = 0; i < pixels_size; i += 4)
            {
                byte r = pixels[i];
                pixels[i] = pixels[i+2];
                pixels[i+2] = r;
            }
            return ImageData.Create (meta, PixelFormats.Bgra32, null, pixels);
        }

        public override void Write (Stream file, ImageData image)
        {
            throw new NotImplementedException ("IpgFormat.Write not implemented");
        }
    }
}
