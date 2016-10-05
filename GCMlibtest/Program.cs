using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GameFormatReader.Common;

namespace GCMlibtest
{
    class Program
    {
        static void Main(string[] args)
        {
            List<FSTEntry> FSTTest = new List<FSTEntry>();

            using (FileStream stream = new FileStream(@"C:\Program Files (x86)\SZS Tools\GLME01.iso", FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                int FSTOffset = reader.ReadInt32At(0x424);

                reader.BaseStream.Position = FSTOffset;

                int numEntries = reader.ReadInt32At(reader.BaseStream.Position + 8);

                int stringTableOffset = numEntries * 0xC;

                for (int i = 0; i < numEntries; i++)
                {
                    FSTEntry fst = new FSTEntry();

                    fst.Type = (FSTNodeType)reader.ReadByte();

                    reader.SkipByte();

                    int curPos = (int)reader.BaseStream.Position;

                    ushort stringOffset = (ushort)reader.ReadInt16();

                    reader.BaseStream.Position = stringOffset + stringTableOffset + FSTOffset;

                    string name = reader.ReadStringUntil('\0');

                    fst.RelativeFileName = name;

                    reader.BaseStream.Position = curPos + 2;

                    fst.FileOffsetParentDir = reader.ReadInt32();

                    fst.FileSizeNextDirIndex = reader.ReadInt32();

                    FSTTest.Add(fst);
                }

                VirtualFilesystemDirectory rootDir = new VirtualFilesystemDirectory("root");

                VirtualFilesystemDirectory sysData = new VirtualFilesystemDirectory(VirtualFilesystemDirectory.SysDataName);

                byte[] headerData = reader.ReadBytesAt(0, 0x2440);

                sysData.Children.Add(new VirtualFilesystemFile("ISOHeader", ".hdr", headerData));

                int headerOffset = reader.ReadInt32At(0x420);

                byte[] dolData = reader.ReadBytesAt(headerOffset, FSTOffset - headerOffset);

                sysData.Children.Add(new VirtualFilesystemFile("Start", ".dol", dolData));

                byte[] appLoaderData = reader.ReadBytesAt(0x2440, headerOffset - 0x2440);

                sysData.Children.Add(new VirtualFilesystemFile("AppLoader", ".ldr", appLoaderData));

                byte[] ftsData = reader.ReadBytesAt(FSTOffset, reader.ReadInt32At(0x428));

                sysData.Children.Add(new VirtualFilesystemFile("Game", ".toc", ftsData));

                rootDir.Children.Add(sysData);

                int count = 1;

                while (count < numEntries)
                {
                    if (FSTTest[count].Type == FSTNodeType.Directory)
                    {
                        VirtualFilesystemDirectory dir = new VirtualFilesystemDirectory(FSTTest[count].RelativeFileName);

                        Console.WriteLine("Created directory: " + dir.Name);

                        FSTEntry curEnt = FSTTest[count];

                        while (count < curEnt.FileSizeNextDirIndex - 1)
                        {
                            count = GetDirStructureRecursive(count + 1, FSTTest, FSTTest[count + 1], dir, reader);
                        }

                        rootDir.Children.Add(dir);
                    }

                    else
                    {
                        VirtualFilesystemFile file = GetFileData(FSTTest[count], reader);

                        rootDir.Children.Add(file);
                    }

                    count += 1;
                }

                SetAllFileAbsoluteFilenames(rootDir, "root");

                /* //Test dumping code
                Directory.CreateDirectory(@"C:\Program Files (x86)\SZS Tools\test root");

                foreach (VirtualFilesystemNode vsObj in rootDir.Children)
                {
                    DumpDirsRecursive(vsObj, @"C:\Program Files (x86)\SZS Tools\test root");
                }

                Console.WriteLine("Done!");

                Console.ReadLine();
                 * */

                using (FileStream output = new FileStream(@"C:\Program Files (x86)\SZS Tools\testOut.iso", FileMode.Create))
                {
                    EndianBinaryWriter writer = new EndianBinaryWriter(output, Endian.Big);

                    List<byte> fstNameBank = new List<byte>();

                    List<FSTEntry> outputFST = new List<FSTEntry>();

                    List<VirtualFilesystemFile> fileList = new List<VirtualFilesystemFile>();

                    FSTEntry rootFST = new FSTEntry();

                    int fstListOffset = 0;

                    VirtualFilesystemDirectory sysDir = (VirtualFilesystemDirectory)rootDir.Children[0];

                    VirtualFilesystemFile header = (VirtualFilesystemFile)sysDir.Children[0];

                    writer.Write(header.Data);

                    VirtualFilesystemFile apploader = (VirtualFilesystemFile)sysDir.Children[2];

                    writer.Write(apploader.Data);

                    VirtualFilesystemFile dol = (VirtualFilesystemFile)sysDir.Children[1];

                    writer.Write(dol.Data);

                    fstListOffset = (int)writer.BaseStream.Position;

                    int fstSkipOffsetValue = 12;

                    rootDir.Children.RemoveAt(0);

                    foreach (VirtualFilesystemNode node in rootDir.Children)
                    {
                        fstSkipOffsetValue = GetFSTSkipValue(fstSkipOffsetValue, node);
                    }

                    byte[] dummyFST = new byte[fstSkipOffsetValue];

                    writer.Write(dummyFST);

                    rootFST.Type = FSTNodeType.Directory;

                    outputFST.Add(rootFST); //Placeholder FST entry for the root

                    foreach (VirtualFilesystemNode node in rootDir.Children)
                    {
                        DoOutputPrep(node, outputFST, fstNameBank, writer, 0);
                    }

                    rootFST.FileSizeNextDirIndex = outputFST.Count();

                    outputFST[0] = rootFST; //Add actual root FST entry

                    writer.BaseStream.Position = fstListOffset;

                    foreach (FSTEntry entry in outputFST)
                    {
                        writer.Write((byte)entry.Type);

                        writer.Write((byte)0);

                        writer.Write((ushort)entry.FileNameOffset);

                        writer.Write(entry.FileOffsetParentDir);

                        writer.Write(entry.FileSizeNextDirIndex);
                    }

                    writer.Write(fstNameBank.ToArray());
                }
            }
        }

        private static int GetDirStructureRecursive(int curIndex, List<FSTEntry> FST, FSTEntry parentFST, VirtualFilesystemDirectory parentDir, EndianBinaryReader image)
        {
            FSTEntry curEntry = FST[curIndex];

            if (curEntry.Type == FSTNodeType.Directory)
            {
                VirtualFilesystemDirectory dir = new VirtualFilesystemDirectory(curEntry.RelativeFileName);

                Console.WriteLine("Created directory: " + dir.Name);

                while (curIndex < curEntry.FileSizeNextDirIndex - 1)
                {
                    curIndex = GetDirStructureRecursive(curIndex + 1, FST, curEntry, dir, image);
                }

                parentDir.Children.Add(dir);

                Console.WriteLine("Leaving directory: " + dir.Name);

                return curIndex;
            }

            else
            {
                VirtualFilesystemFile file = GetFileData(curEntry, image);

                parentDir.Children.Add(file);

                /*byte[] data = new byte[curEntry.FileSizeNextDirIndex];

                string[] fileNameAndExtension = curEntry.RelativeFileName.Split('.');

                image.BaseStream.Position = curEntry.FileOffsetParentDir;

                data = image.ReadBytes(curEntry.FileSizeNextDirIndex);

                    if (fileNameAndExtension.Length != 2)
                        fileNameAndExtension = new string[] { fileNameAndExtension[0], "" };

                VirtualFilesystemFile file = new VirtualFilesystemFile(fileNameAndExtension[0], "." + fileNameAndExtension[1], data);

                Console.WriteLine("Created file: " + file.Name);

                parentDir.Children.Add(file);*/

                return curIndex;
            }
        }

        private static void DumpDirsRecursive(VirtualFilesystemNode vfsObj, string root)
        {
            if (vfsObj.Type == NodeType.Directory)
            {
                VirtualFilesystemDirectory dir = (VirtualFilesystemDirectory)vfsObj;

                string testRoot = root + @"\" + dir.Name;

                Directory.CreateDirectory(testRoot);

                foreach (VirtualFilesystemNode child in dir.Children)
                {
                    if (child.Type == NodeType.Directory)
                    {
                        Directory.CreateDirectory(testRoot + @"\" + child.Name);

                        Console.WriteLine("Wrote directory: " + testRoot + @"\" + child.Name);

                        DumpDirsRecursive(child, testRoot);
                    }

                    else
                    {
                        DumpDirsRecursive(child, testRoot);
                    }
                }
            }

            else
            {
                VirtualFilesystemFile file = (VirtualFilesystemFile)vfsObj;

                using (FileStream stream = new FileStream(root + @"\" + file.Name + file.Extension, FileMode.Create))
                {
                    EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

                    writer.Write(file.Data);

                    Console.WriteLine("Wrote file: " + root + @"\" + file.Name + file.Extension);
                }
            }

            /*foreach (VirtualFilesystemNode child in (VirtualFilesystemDirectory)vfsObj.Children)
            {
                if (child.Type == NodeType.Directory)
                {
                    string newRoot = root + @"\" + child.Name;

                    Directory.CreateDirectory(newRoot);

                    Console.WriteLine("Wrote directory: " + newRoot);
                    
                    DumpDirsRecursive((VirtualFilesystemDirectory)child, newRoot);
                }

                else
                {
                    VirtualFilesystemFile file = (VirtualFilesystemFile)child;

                    using(FileStream stream = new FileStream(root + @"\" + file.Name + file.Extension, FileMode.Create))
                    {
                        EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

                        writer.Write(file.Data);

                        Console.WriteLine("Wrote file: " + root + @"\" + file.Name + file.Extension);
                    }
                }
            }*/
        }

        private static VirtualFilesystemFile GetFileData(FSTEntry fstData, EndianBinaryReader image)
        {
            string[] fileNameAndExtension = fstData.RelativeFileName.Split('.');

            image.BaseStream.Position = fstData.FileOffsetParentDir;

            byte[] data = image.ReadBytes((int)fstData.FileSizeNextDirIndex);

            VirtualFilesystemFile file;

            if (fileNameAndExtension.Length != 2)
            {
                file = new VirtualFilesystemFile(fileNameAndExtension[0], "", data);
            }

            else
            {
                file = new VirtualFilesystemFile(fileNameAndExtension[0], "." + fileNameAndExtension[1], data);
            }

            Console.WriteLine("Created file: " + file.Name);

            return file;
        }

        private static void DoOutputPrep(VirtualFilesystemNode vfsNode, List<FSTEntry> outputFST, List<byte> fstNameBank, EndianBinaryWriter writer, int curParentDirIndex)
        {
            FSTEntry fstEnt = new FSTEntry();

            if (vfsNode.Type == NodeType.Directory)
            {
                VirtualFilesystemDirectory dir = (VirtualFilesystemDirectory)vfsNode;

                fstEnt.Type = FSTNodeType.Directory;

                fstEnt.FileNameOffset = fstNameBank.Count();

                fstNameBank.AddRange(Encoding.ASCII.GetBytes(dir.Name.ToCharArray()));

                fstNameBank.Add(0);

                fstEnt.FileOffsetParentDir = curParentDirIndex;

                curParentDirIndex = outputFST.Count();

                int thisDirIndex = curParentDirIndex;

                outputFST.Add(fstEnt); //Placeholder for this dir

                foreach (VirtualFilesystemNode child in dir.Children)
                {
                    DoOutputPrep(child, outputFST, fstNameBank, writer, curParentDirIndex);
                }

                int dirEndIndex = outputFST.Count();

                fstEnt.FileSizeNextDirIndex = (dirEndIndex - thisDirIndex) + thisDirIndex;

                outputFST[thisDirIndex] = fstEnt; //Add the actual entry after giving it the rest of the info
            }

            else
            {
                VirtualFilesystemFile file = (VirtualFilesystemFile)vfsNode;

                fstEnt.Type = FSTNodeType.File;

                fstEnt.FileSizeNextDirIndex = file.Data.Length;

                fstEnt.FileNameOffset = fstNameBank.Count();

                fstNameBank.AddRange(Encoding.ASCII.GetBytes(file.Name.ToCharArray()));

                fstNameBank.AddRange(Encoding.ASCII.GetBytes(file.Extension.ToCharArray()));

                fstNameBank.Add((byte)0);

                writer.BaseStream.Position = (int)writer.BaseStream.Position + (32 - ((int)writer.BaseStream.Position % 32)) % 32;

                fstEnt.FileOffsetParentDir = (int)writer.BaseStream.Position;

                writer.Write(file.Data);

                for (int i = 0; i < ((32 - (file.Data.Length - 32)) % 32); i++)
                {
                    writer.Write((byte)0);
                }

                Console.WriteLine("Wrote file: " + file.Name);

                outputFST.Add(fstEnt);
            }
        }

        private static int GetFSTSkipValue(int curValue, VirtualFilesystemNode node)
        {
            if (node.Type == NodeType.Directory)
            {
                VirtualFilesystemDirectory dir = (VirtualFilesystemDirectory)node;

                curValue += (12 + dir.Name.Length + 1);

                foreach (VirtualFilesystemNode child in dir.Children)
                {
                    curValue = GetFSTSkipValue(curValue, child);
                }
            }

            else
            {
                VirtualFilesystemFile file = (VirtualFilesystemFile)node;

                curValue += (int)(12 + file.Name.Length + file.Extension.Length + 1);
            }

            return curValue;
        }

        private static VirtualFilesystemFile GetFileRecursive(VirtualFilesystemNode vfsObj, string absoluteFilename)
        {
            if (vfsObj.Type == NodeType.Directory)
            {
                VirtualFilesystemDirectory dir = (VirtualFilesystemDirectory)vfsObj;

                foreach (VirtualFilesystemNode child in dir.Children)
                {
                    VirtualFilesystemFile result = GetFileRecursive(child, absoluteFilename);

                    if (result != null)
                        return result;
                }
            }

            else
            {
                VirtualFilesystemFile file = (VirtualFilesystemFile)vfsObj;

                if (file.AbsoluteFilename == absoluteFilename)
                    return file;
                else
                    return null;
            }

            return null;
        }

        private static void SetAllFileAbsoluteFilenames(VirtualFilesystemNode node, string root)
        {
            if (node.Type == NodeType.Directory)
            {
                VirtualFilesystemDirectory dir = (VirtualFilesystemDirectory)node;

                string newRoot = root + @"\" + dir.Name;

                foreach (VirtualFilesystemNode child in dir.Children)
                {
                    SetAllFileAbsoluteFilenames(child, newRoot);
                }
            }

            else
            {
                VirtualFilesystemFile file = (VirtualFilesystemFile)node;

                file.AbsoluteFilename = root + @"\" + file.Name + file.Extension;
            }
        }
    }
}
