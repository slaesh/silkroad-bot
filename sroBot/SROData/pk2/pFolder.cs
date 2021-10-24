namespace sroBot.SROData.pk2
{
    using System;
    using System.Collections.Generic;

    public class pFolder
    {
        public List<pFile> files;
        public string name;
        public long position;
        public List<pFolder> subfolders;
    }
}

