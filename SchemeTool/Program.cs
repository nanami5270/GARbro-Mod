using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;

namespace SchemeTool
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load database
            using (Stream stream = File.OpenRead(".\\GameData\\Formats.dat"))
            {
                GameRes.FormatCatalog.Instance.DeserializeScheme(stream);
            }

            GameRes.Formats.KiriKiri.Xp3Opener format = GameRes.FormatCatalog.Instance.ArcFormats
                .FirstOrDefault(a => a is GameRes.Formats.KiriKiri.Xp3Opener) as GameRes.Formats.KiriKiri.Xp3Opener;

            if (format != null)
            {
                GameRes.Formats.KiriKiri.Xp3Scheme scheme = format.Scheme as GameRes.Formats.KiriKiri.Xp3Scheme;

                // Add scheme information here

                #if true
                byte[] cb = File.ReadAllBytes(@"C:\Users\MLChinoo\Desktop\limelight_lj\control_block.bin");
                var cb2 = MemoryMarshal.Cast<byte, uint>(cb);
                for (int i = 0; i < cb2.Length; i++)
                    cb2[i] = ~cb2[i];
                var cs = new GameRes.Formats.KiriKiri.CxScheme
                {
                    Mask = 0x2e2,
                    Offset = 0x283,
                    PrologOrder = new byte[] { 1, 0, 2 },
                    OddBranchOrder = new byte[] { 1, 2, 4, 3, 0, 5 },
                    EvenBranchOrder = new byte[] { 2, 5, 0, 7, 6, 1, 3, 4 },
                    ControlBlock = cb2.ToArray()
                };
                var crypt = new GameRes.Formats.KiriKiri.HxCrypt(cs);
                crypt.RandomType = 0;
                crypt.FilterKey = 0xb5a900b497d99000;
                crypt.NamesFile = "HxNames.lst";
                var key1 = SoapHexBinary.Parse("90731d0f07858de0c5553c10035d174003d400e7d7ccaa76ba4ca078bcc6cc78").Value;
                var key2 = SoapHexBinary.Parse("092063cd25d03538a83e10e9ef5d1609").Value;
                crypt.IndexKeyDict = new Dictionary<string, GameRes.Formats.KiriKiri.HxIndexKey>()
                {
                    { "data.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "bgimage.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "bgm.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "evimage.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "evimage2.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "fgimage.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "image.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "patch.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "scn.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "uipsd.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "video.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "voice.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } },
                    { "voice2.xp3", new GameRes.Formats.KiriKiri.HxIndexKey { Key1 = key1, Key2 = key2 } }
                };
                #else
                GameRes.Formats.KiriKiri.ICrypt crypt = new GameRes.Formats.KiriKiri.XorCrypt(0x00);
                #endif

                    scheme.KnownSchemes.Add("Limelight Lemonade Jam", crypt);
            }

            var gameMap = typeof(GameRes.FormatCatalog).GetField("m_game_map", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(GameRes.FormatCatalog.Instance) as Dictionary<string, string>;

            if (gameMap != null)
            {
                // Add file name here
                 // gameMap.Add("SabbatOfTheWitch.exe", "Sabbat of the Witch [Steam]");
            }

            // Save database
            using (Stream stream = File.Create(".\\GameData\\Formats.dat"))
            {
                GameRes.FormatCatalog.Instance.SerializeScheme(stream);
            }
        }
    }
}
