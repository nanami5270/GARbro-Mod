//! \file       ArcLAC.cs
//! \date       Wed Aug 17 15:59:38 2016
//! \brief      Leaf resource archive.
//
// Copyright (C) 2016 by morkt
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using GameRes.Compression;

namespace GameRes.Formats.Leaf
{
    [Export(typeof(ArchiveFormat))]
    public class LacOpener : ArchiveFormat
    {
        public override string         Tag { get { return "LAC"; } }
        public override string Description { get { return "Leaf resource archive"; } }
        public override uint     Signature { get { return 0x43414C; } } // 'LAC'
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public override ArcFile TryOpen (ArcView file)
        {
            int count = file.View.ReadInt32 (4);
            if (!IsSaneCount (count))
                return null;

            uint index_offset = 8;
            var dir = new List<Entry> (count);
            for (int i = 0; i < count; ++i)
            {
                var name = file.View.ReadString (index_offset, 0x3E);
                if (string.IsNullOrEmpty (name))
                    return null;
                index_offset += 0x3E;
                var entry = FormatCatalog.Instance.Create<PackedEntry> (name);
                entry.IsPacked = 0 != file.View.ReadByte (index_offset);
                index_offset += 0xE;
                entry.Size = file.View.ReadUInt32 (index_offset);
                entry.UnpackedSize = file.View.ReadUInt32 (index_offset+4);
                entry.Offset = file.View.ReadInt64 (index_offset+0xC);
                index_offset += 0x2C;
                if (!entry.CheckPlacement (file.MaxOffset))
                    return null;
                dir.Add (entry);
            }
            return new ArcFile (file, this, dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var input = base.OpenEntry (arc, entry);
            var pent = entry as PackedEntry;
            if (null == pent || !pent.IsPacked)
                return input;
            var lzs = new LzssStream (input);
            lzs.Config.FrameFill = 0x20;
            return lzs;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public class PakOpener : ArchiveFormat
    {
        public override string         Tag { get { return "PAK/LAC"; } }
        public override string Description { get { return "Leaf resource archive"; } }
        public override uint     Signature { get { return 0x43414C; } } // 'LAC'
        public override bool  IsHierarchic { get { return false; } }
        public override bool      CanWrite { get { return true; } }

        public PakOpener ()
        {
            Extensions = new string[] { "pak" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            int count = file.View.ReadInt32 (4);
            if (!IsSaneCount (count))
                return null;

            uint index_offset = 8;
            var dir = new List<Entry> (count);
            var name_buf = new byte[0x20];
            for (int i = 0; i < count; ++i)
            {
                file.View.Read (index_offset, name_buf, 0, 0x20);
                index_offset += 0x20;
                int l;
                for (l = 0; l < 0x1F && name_buf[l] != 0; ++l)
                {
                    name_buf[l] ^= 0xFF;
                }
                if (0 == l)
                    return null;
                var name = Encodings.cp932.GetString (name_buf, 0, l);
                var entry = FormatCatalog.Instance.Create<PackedEntry> (name);
                entry.IsPacked = 0 != name_buf[0x1F];
                entry.Size = file.View.ReadUInt32 (index_offset);
                entry.Offset = file.View.ReadUInt32 (index_offset+4);
                index_offset += 8;
                if (!entry.CheckPlacement (file.MaxOffset))
                    return null;
                dir.Add (entry);
            }
            return new ArcFile (file, this, dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var pent = entry as PackedEntry;
            if (null == pent || !pent.IsPacked || entry.Size <= 4)
                return base.OpenEntry (arc, entry);
            if (0 == pent.UnpackedSize)
            {
                uint size = arc.File.View.ReadUInt32 (entry.Offset);
                if (size == pent.Size)
                {
                    pent.Offset += 4;
                    pent.Size -= 4;
                }
                pent.UnpackedSize = arc.File.View.ReadUInt32 (entry.Offset);
                pent.Offset += 4;
                pent.Size -= 4;
                if (0 == pent.UnpackedSize)
                    ++pent.UnpackedSize;
            }
            Stream input = arc.File.CreateStream (entry.Offset, entry.Size);
            return new LzssStream (input);
        }

        // --- REPACK IMPLEMENTATION ---
        // Note: This implementation was made for the Leaf game "Kimi ga Yobu, Megiddo no Oka de".
        // Other games may or may not be supported.

        public override void Create (Stream output, IEnumerable<Entry> entries, ResourceOptions options, EntryCallback callback)
        {
            // Order entries using the same order as the original PAK/LAC archive.
            entries = entries.OrderBy (x => x, LacEntryComparer.Instance).ToList();

            int count = entries.Count();
            using (var writer = new BinaryWriter (output, Encoding.ASCII, true))
            {
                // Write Header (LAC)
                writer.Write (Signature);
                writer.Write (count);

                long table_offset = 8;
                long data_offset = table_offset + (count * 0x28);

                writer.BaseStream.Position = data_offset;

                var entryInfos = new List<EntryInfo>();
                int i = 0;

                foreach (var entry in entries)
                {
                    if (callback != null)
                        callback (i + 1, entry, null);

                    long currentOffset = writer.BaseStream.Position;

                    using (var input = File.OpenRead(entry.Name))
                    {
                        byte[] raw_data = new byte[input.Length];
                        input.Read (raw_data, 0, (int)input.Length);

                        // Compress
                        byte[] compressedData = LeafLzss.Compress (raw_data);

                        uint totalSize;
                        bool isPacked;

                        // Write Data
                        if (raw_data.Length > 32 && compressedData.Length < raw_data.Length)
                        {
                            isPacked = true;
                            totalSize = (uint)(compressedData.Length + 4);
                            writer.Write ((uint)raw_data.Length);
                            writer.Write (compressedData);
                        }
                        else
                        {
                            isPacked = false;
                            totalSize = (uint)raw_data.Length;
                            writer.Write (raw_data);
                        }

                        entryInfos.Add (new EntryInfo
                        {
                            Name = Path.GetFileName (entry.Name),
                            Offset = (uint)currentOffset,
                            Size = totalSize,
                            IsPacked = isPacked
                        });
                    }
                    i++;
                }

                // Write File Table
                writer.BaseStream.Position = table_offset;
                foreach (var info in entryInfos)
                {
                    byte[] name_buf = new byte[0x20];

                    byte[] name_bytes = Encodings.cp932.GetBytes (info.Name);
                    int len = Math.Min (name_bytes.Length, 0x1F);
                    for (int j = 0; j < len; ++j)
                        name_buf[j] = (byte)(name_bytes[j] ^ 0xFF);
                    if (info.IsPacked)
                        name_buf[0x1F] = 1;

                    writer.Write (name_buf);
                    writer.Write (info.Size);
                    writer.Write (info.Offset);
                }
            }
        }

        struct EntryInfo
        {
            public string Name;
            public uint Offset;
            public uint Size;
            public bool IsPacked;
        }

        internal sealed class LacEntryComparer : IComparer<Entry>
        {
            public static readonly LacEntryComparer Instance = new LacEntryComparer();

            public int Compare (Entry x, Entry y)
            {
                string x_name = Path.GetFileName (x.Name);
                string y_name = Path.GetFileName (y.Name);

                // LAC names use '_' as a separator. The archive's order puts
                // that separator after digits and before letters, which may not be
                // the order produced by the current-culture string comparer.
                int result = CompareNames (x_name, y_name);
                if (result != 0)
                    return result;

                // Keep the ordering deterministic for names that differ only
                // by case or by a path component.
                return StringComparer.Ordinal.Compare (x.Name, y.Name);
            }

            private static int CompareNames (string x, string y)
            {
                int length = Math.Min (x.Length, y.Length);
                for (int i = 0; i < length; ++i)
                {
                    char xc = char.ToUpperInvariant (x[i]);
                    char yc = char.ToUpperInvariant (y[i]);
                    if (xc == yc)
                        continue;

                    if (xc == '_')
                        return char.IsLetter (yc) ? -1 : 1;
                    if (yc == '_')
                        return char.IsLetter (xc) ? 1 : -1;

                    return xc < yc ? -1 : 1;
                }

                return x.Length.CompareTo (y.Length);
            }
        }
    }
}
