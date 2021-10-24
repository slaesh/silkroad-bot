namespace sroBot.SROData.pk2
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size=0x80)]
    public struct pk2Entry
    {
        [MarshalAs(UnmanagedType.I1)]
        public byte type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=0x51)]
        public string name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
        public byte[] accessTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
        public byte[] createTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
        public byte[] modifyTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
        public byte[] position;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        public byte[] size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
        public byte[] nextChain;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
        public byte[] padding;
        public long nChain
        {
            get
            {
                return BitConverter.ToInt64(this.nextChain, 0);
            }
        }
        public long Position
        {
            get
            {
                return BitConverter.ToInt64(this.position, 0);
            }
        }
        public uint Size
        {
            get
            {
                return BitConverter.ToUInt32(this.size, 0);
            }
        }
    }
}

