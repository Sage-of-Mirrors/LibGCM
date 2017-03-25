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
        /// Returns the directory at the provided path, if it exists. If it doesn't, it will return null.
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static VirtualFilesystemDirectory FindDirectory(VirtualFilesystemDirectory root, string dirPath)
        {
            VirtualFilesystemDirectory result = root;
            VirtualFilesystemDirectory currentDir = root;
            List<string> dividedPath = new List<string>(dirPath.ToLower().Split('\\'));
            // This will remember where we are in dividePath above
            int pathIndex = 0;

            // We're going to run through the current root's children and compare them to dividedPath[pathIndex].
            // If their names are the same and the child is a directory, we will move that directory to result
            // and restart the loop, moving on to the next element in dividedPath.
            while (pathIndex < dividedPath.Count)
            {
                for (int i = 0; i < result.Children.Count; i++)
                {
                    if (currentDir.Children[i].Name.ToLower() == dividedPath[pathIndex] && currentDir.Children[i].Type == NodeType.Directory)
                    {
                        currentDir = currentDir.Children[i] as VirtualFilesystemDirectory;
                        break;
                    }
                }

                // If the current dir is the same as the result, that means we didn't find the next dir in the list.
                // In turn, that means the dir we're looking for doesn't exist.
                if (currentDir == result)
                    return null;
                // Otherwise it does exist, so we set result to the current dir to set up the next check.
                else
                    result = currentDir;

                pathIndex++;
            }

            return result;
        }

        /// <summary>
        /// Returns the file at the given path, if it exists. If the file does not exist, it will return null.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
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
