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

        /// <summary>
        /// Returns the directory at the provided path, if it exists.
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static VirtualFilesystemDirectory FindDirectory(VirtualFilesystemDirectory root, string dirPath)
        {
            VirtualFilesystemDirectory result = root;
            List<string> dividedPath = new List<string>(dirPath.ToLower().Split('\\'));
            int pathIndex = 0;

            while (pathIndex < dividedPath.Count)
            {
                for (int i = 0; i < result.Children.Count; i++)
                {
                    if (result.Children[i].Name.ToLower() == dividedPath[pathIndex] && result.Children[i].Type == NodeType.Directory)
                    {
                        result = result.Children[i] as VirtualFilesystemDirectory;
                        break;
                    }
                }

                pathIndex++;
            }

            return result;
        }

        public static VirtualFilesystemFile FindFile(VirtualFilesystemDirectory root, string filePath)
        {
            VirtualFilesystemFile result = null;
            VirtualFilesystemDirectory curDir = root;
            List<string> dividedPath = new List<string>(filePath.ToLower().Split('\\'));
            int pathIndex = 0;

            while (pathIndex < dividedPath.Count)
            {
                for (int i = 0; i < curDir.Children.Count; i++)
                {
                    if (curDir.Children[i].Type == NodeType.File)
                    {
                        VirtualFilesystemFile cand = curDir.Children[i] as VirtualFilesystemFile;

                        if (cand.Name.ToLower() + cand.Extension.ToLower() == dividedPath[pathIndex])
                        {
                            result = cand;
                            break;
                        }
                    }

                    if (curDir.Children[i].Name.ToLower() == dividedPath[pathIndex])
                    {
                        if (curDir.Children[i].Type == NodeType.Directory)
                            curDir = curDir.Children[i] as VirtualFilesystemDirectory;
                        break;
                    }
                }

                pathIndex++;
            }

            return result;
        }
    }
}
