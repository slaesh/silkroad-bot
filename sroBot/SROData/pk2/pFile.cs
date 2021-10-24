namespace sroBot.SROData.pk2
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct pFile
    {
        public string name;
        public long position;
        public uint size;
        public pFolder parentFolder;
    }
}

