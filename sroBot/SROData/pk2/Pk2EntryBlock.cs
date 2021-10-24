namespace sroBot.SROData.pk2
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size=0xa00)]
    public struct Pk2EntryBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=20)]
        public pk2Entry[] entries;
    }
}

