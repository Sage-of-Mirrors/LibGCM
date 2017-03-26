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
            if (args.Length == 0)
                ShowHelpMessage();
            else if (args.Length == 1)
            {
                string fileDirTest = Path.GetExtension(args[0]);
                if (fileDirTest == ".iso")
                {
                    string outName = $"{Path.GetDirectoryName(args[0])}\\{Path.GetFileNameWithoutExtension(args[0])}";
                    DumpISO(args[0], outName);
                }
                else if (fileDirTest == "")
                {
                    string outName = $"{Path.GetDirectoryName(args[0])}\\{Path.GetFileNameWithoutExtension(args[0])}.iso";
                    BuildISO(args[0], outName);
                }
            }
            else
            {
                string inputPath = "";
                string outputPath = "";

                if (args.Length <= 2)
                    inputPath = args[1];
                if (args.Length <= 3)
                {
                    inputPath = args[1];
                    outputPath = args[2];
                }

                if (args[0] == "-dump")
                {
                    if (outputPath == "")
                    {
                        outputPath = inputPath;
                    }

                    DumpISO(inputPath, outputPath);
                }
                else if (args[0] == "-build")
                {
                    if (outputPath == "")
                        outputPath = $"{inputPath}\\{Path.GetFileName(inputPath)}.iso";

                    BuildISO(inputPath, outputPath);
                }
            }
        }

        static void ShowHelpMessage()
        {
            Console.WriteLine("GCISOTool is a commandline tool for working with GameCube ISOs.");
            Console.WriteLine("Thanks to LordNed for inspiration and all who documented GC ISOs.");
            Console.WriteLine("For any issues, contact @SageOfMirrors on Twitter or on GitHub.");
            Console.WriteLine();
            Console.WriteLine("Usage: GCISOTool [-option] [args]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("-dump  input_iso_path [output_directory_path]               Dumps the contents of the ISO.");
            Console.WriteLine("-build input_directory      [output_iso_path]               Builds an ISO from the specified directory.");
        }

        static void DumpISO(string inputPath, string outputPath)
        {
            ISOUtilities.DumpISOContents(inputPath, outputPath);
        }

        static void BuildISO(string inputPath, string outputPath)
        {
            VirtualFilesystemDirectory rootDir = new VirtualFilesystemDirectory("root");
            GetDirectoriesRecursive(rootDir, inputPath);
            ISOUtilities.DumpToISO(rootDir, outputPath);
        }

        static void GetDirectoriesRecursive(VirtualFilesystemDirectory root, string rootString)
        {
            List<string> files = new List<string>(Directory.GetFiles(rootString));

            // The entries in this subdir need to be in a particular order.
            // We'll fill them in with null so that we can replace them later.
            if (Path.GetFileName(rootString) == "&&systemdata")
            {
                for (int i = 0; i < 4; i++)
                    root.Children.Add(null);
            }

            foreach (string str in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(str);
                string fileExt = Path.GetExtension(str);
                VirtualFileContents cont = new VirtualFileContents(File.ReadAllBytes(str));

                VirtualFilesystemFile newFile = new VirtualFilesystemFile(fileName, fileExt, cont);

                // These will replace the nulls we put in &&systemdata's child list earlier.
                if (fileName.ToLower() + fileExt.ToLower() == "iso.hdr")
                    root.Children[0] = newFile;
                else if (fileName.ToLower() + fileExt.ToLower() == "apploader.ldr")
                    root.Children[2] = newFile;
                else if (fileName.ToLower() + fileExt.ToLower() == "start.dol")
                    root.Children[1] = newFile;
                else if (fileName.ToLower() + fileExt.ToLower() == "game.toc")
                    root.Children[3] = newFile;
                else
                    root.Children.Add(newFile);
            }

            List<string> dirs = new List<string>(Directory.GetDirectories(rootString));

            foreach (string str in dirs)
            {
                string dirName = Path.GetFileName(str);
                VirtualFilesystemDirectory dir = new VirtualFilesystemDirectory(dirName);

                GetDirectoriesRecursive(dir, str);

                if (dirName == "&&systemdata")
                    root.Children.Insert(0, dir);
                else
                    root.Children.Add(dir);
            }
        }
    }
}
