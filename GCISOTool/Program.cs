using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libGCM;
using WArchiveTools.FileSystem;
using System.IO;
using GameFormatReader.Common;

namespace GCISOTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string testInPath = @"D:\Games\GameCube\The Legend of Zelda - The Wind Waker (GZLE01).iso";
            string testOutPath = @"D:\SZS Tools\GCISOTool Tests\GLME01";
            string testISOPath = @"D:\SZS Tools\GCISOTool Tests\GLME01_test.iso";

            using (FileStream stm = new FileStream(testInPath, FileMode.Open, FileAccess.Read))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stm, Endian.Big);
                VirtualFilesystemDirectory iso = ISOUtilities.LoadISO(testInPath);

                //ISOUtilities.DumpISOContents(iso, testOutPath);
                //ISOUtilities.DumpISOContents(testInPath, testOutPath);
                //ISOUtilities.DumpToISO(iso, testISOPath);
                VirtualFilesystemDirectory dir = ISOUtilities.FindDirectory(iso, @"res\Stage\Pjavd");
                VirtualFilesystemFile file = ISOUtilities.FindFile(iso, @"res\Stage\A_Mori\room0.arc");
            }
        }
    }
}
