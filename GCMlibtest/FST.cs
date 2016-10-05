using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCMlibtest
{
    public enum FSTNodeType
    {
        File = 0,
        Directory = 1
    }

    struct FSTEntry
    {
        public FSTNodeType Type;

        public string AbsoluteFileName;

        public string RelativeFileName;

        public int FileOffsetParentDir; //If Type is File, this is the offset of the file's data. If it's Directory, this is the index of its parent.

        public int FileSizeNextDirIndex; //If Type is File, this is the size of the file data. If it's Directory, this is the index of the next dir on the same level as itself.

        public int FileNameOffset;
    }
}
