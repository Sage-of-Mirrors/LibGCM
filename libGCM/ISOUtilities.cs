using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WArchiveTools.FileSystem;
using GameFormatReader.Common;
using System.IO;
using libGCM.ISOs;

namespace libGCM
{
    public static class ISOUtilities
    {
        /// <summary>
        /// Returns the root of the given ISO file in the form of a VirtualFilesystemDirectory.
        /// </summary>
        /// <param name="filePath">Path to the ISO file</param>
        /// <returns></returns>
        public static VirtualFilesystemDirectory LoadISO(string filePath)
        {
            VirtualFilesystemDirectory rootDir;

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);
                ISO iso = new ISO();
                rootDir = iso.LoadISO(reader);
            }

            return rootDir;
        }

        /// <summary>
        /// Dumps an ISO's contents from the specified root to the provided output path.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="outputPath"></param>
        public static void DumpISOContents(VirtualFilesystemDirectory root, string outputPath)
        {
            ISO iso = new ISO();
            iso.DumpToDisk(root, outputPath);
        }

        /// <summary>
        /// Dumps an ISO's contents from the specified ISO file to the provided output path.
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        public static void DumpISOContents(string inputPath, string outputPath)
        {
            ISO iso = new ISO();

            using (FileStream stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);
                iso.DumpToDisk(iso.LoadISO(reader), outputPath);
            }
        }

        /// <summary>
        /// Outputs an ISO file containing the specified root to the provided output path.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="outputPath"></param>
        public static void DumpToISO(VirtualFilesystemDirectory root, string outputPath)
        {
            ISO iso = new ISO();
            iso.WriteISO(root, outputPath);
        }
    }
}
