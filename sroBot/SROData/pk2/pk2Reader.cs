namespace sroBot.SROData.pk2
{
    using SilkroadSecurityApi;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Linq;

    public class pk2Reader
    {
        private byte[] bKey = new byte[] { 50, 0xce, 0xdd, 0x7c, 0xbc, 0xa8 };
        private Blowfish blowfish = new Blowfish();
        private pFolder currentFolder;
        public List<Pk2EntryBlock> EntryBlocks = new List<Pk2EntryBlock>();
        public List<pFile> Files = new List<pFile>();
        private FileStream fileStream;
        public List<pFolder> Folders = new List<pFolder>();
        private pFolder mainFolder;

        public pk2Reader(string silkroadPath)
        {
            if (!File.Exists(silkroadPath))
            {
                throw new Exception("pk2 not found. Please set the correct Path to your Silkroad directory");
            }
            this.fileStream = new FileStream(silkroadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.blowfish.Initialize(this.bKey);
            BinaryReader reader = new BinaryReader(this.fileStream);
            this.header = (pk2Header) this.BufferToStruct(reader.ReadBytes(0x100), typeof(pk2Header));
            Console.WriteLine(this.header.Name);
            this.currentFolder = new pFolder();
            this.currentFolder.name = silkroadPath;
            this.currentFolder.files = new List<pFile>();
            this.currentFolder.subfolders = new List<pFolder>();
            this.mainFolder = this.currentFolder;
            this.Read(reader.BaseStream.Position);
            Console.WriteLine("Done. Found {0} files.", this.Files.Count);
        }

        private object BufferToStruct(byte[] buffer, Type returnStruct)
        {
            IntPtr destination = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, destination, buffer.Length);
            return Marshal.PtrToStructure(destination, returnStruct);
        }

        public bool FileExists(string name)
        {
            return (this.Files.Find(item => item.name.ToLower() == name.ToLower()).position != 0L);
        }

        public int GetFileCount(string name)
        {
            return this.Files.Count(f => string.Equals(f.name, name, StringComparison.OrdinalIgnoreCase));
        }

        public byte[] getFile(string name)
        {
            Predicate<pFile> match = null;
            if (!this.FileExists(name))
            {
                throw new Exception(string.Format("pk2Reader: File not found: {0}", name));
            }
            BinaryReader reader = new BinaryReader(this.fileStream);
            if (match == null)
            {
                match = item => item.name.ToLower() == name.ToLower();
            }
            pFile file = this.Files.Find(match);
            reader.BaseStream.Position = file.position;
            return reader.ReadBytes((int) file.size);
        }

        public byte[] getFile(pFile file)
        {
            BinaryReader reader = new BinaryReader(this.fileStream);
            reader.BaseStream.Position = file.position;
            return reader.ReadBytes((int)file.size).ToArray();
        }

        public IEnumerable<pFile> getFiles(string name)
        {
            if (!this.FileExists(name))
            {
                throw new Exception(string.Format("pk2Reader: File not found: {0}", name));
            }

            var files = new List<pFile>();

            foreach (var file in Files.Where(f => string.Equals(f.name, name, StringComparison.OrdinalIgnoreCase)))
            {
                files.Add(file);
            }

            return files.ToArray();
        }

        public List<string> GetFileNames()
        {
            List<string> list = new List<string>();
            foreach (pFile file in this.Files)
            {
                list.Add(file.name);
            }
            return list;
        }

        private void Read(long position)
        {
            BinaryReader reader = new BinaryReader(this.fileStream) {
                BaseStream = { Position = position }
            };
            List<pFolder> list = new List<pFolder>();
            Pk2EntryBlock block = (Pk2EntryBlock) this.BufferToStruct(this.blowfish.Decode(reader.ReadBytes(Marshal.SizeOf(typeof(Pk2EntryBlock)))), typeof(Pk2EntryBlock));
            for (int i = 0; i < 20; i++)
            {
                pk2Entry entry = block.entries[i];
                switch (entry.type)
                {
                    case 1:
                        if ((entry.name != ".") && (entry.name != ".."))
                        {
                            pFolder item = new pFolder {
                                name = entry.name,
                                position = BitConverter.ToInt64(entry.position, 0)
                            };
                            list.Add(item);
                            this.Folders.Add(item);
                            this.currentFolder.subfolders.Add(item);
                        }
                        break;

                    case 2:
                    {
                        pFile file = new pFile {
                            position = entry.Position,
                            name = entry.name,
                            size = entry.Size,
                            parentFolder = this.currentFolder
                        };
                        this.Files.Add(file);
                        this.currentFolder.files.Add(file);
                        break;
                    }
                }
            }
            if (block.entries[0x13].nChain != 0L)
            {
                this.Read(block.entries[0x13].nChain);
            }
            foreach (pFolder folder in list)
            {
                this.currentFolder = folder;
                if (folder.files == null)
                {
                    folder.files = new List<pFile>();
                }
                if (folder.subfolders == null)
                {
                    folder.subfolders = new List<pFolder>();
                }
                //if (Config.pk2InfoAnzeigen)
                if (true)
                {
                    Console.WriteLine(folder.name);
                }
                this.Read(folder.position);
            }
        }

        public pk2Header header { get; set; }
    }
}

