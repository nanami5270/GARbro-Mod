using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Media;
using GameRes.Compression;

namespace GameRes.Formats
{
    [Export(typeof(ImageFormat))]
    public class ImageRCG : ImageFormat
    {
        public override string Tag
        {
            get { return "RCG"; }
        }

        public override string Description
        {
            get { return "Rave RCG image format"; }
        }

        public override uint Signature
        {
            get { return 0x31520018; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override ImageMetaData ReadMetaData(IBinaryStream stream)
        {
            stream.Position = 4;
            var meta = new ImageMetaData();
            meta.BPP = 24;
            meta.Width = stream.ReadUInt16();
            meta.Height = stream.ReadUInt16();
            meta.OffsetX = 0;
            meta.OffsetY = 0;
            // Skip 8 bytes unknown fields
            stream.Position += 8;
            var compressed_size = stream.ReadUInt32();
            return compressed_size != stream.Length - 24 ? null : meta;
        }

        public override ImageData Read(IBinaryStream stream, ImageMetaData info)
        {
            stream.Position = 20;
            var uncompressed_size = stream.ReadUInt32();
            var data = new byte[uncompressed_size];
            using (var zstream = new ZLibStream(stream.AsStream, CompressionMode.Decompress, true))
            {
                return uncompressed_size != zstream.Read(data, 0, (int)uncompressed_size)
                    ? null
                    : ImageData.Create(info, PixelFormats.Rgb24, null, data);
            }
        }

        public override void Write(Stream file, ImageData image)
        {
            throw new System.NotImplementedException("RCGFormat.Write not implemented");
        }
    }
}