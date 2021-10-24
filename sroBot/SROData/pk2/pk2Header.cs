namespace sroBot.SROData.pk2
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size=0x100)]
    public struct pk2Header
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=30)]
        public string Name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        public byte[] version;
        [MarshalAs(UnmanagedType.I1)]
        public byte encryption;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=0x10)]
        public byte[] verify;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=0xcd)]
        public byte[] reserved;
    }
}

