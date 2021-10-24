using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    public class Item
    {
        public uint UID;
        public ItemInfo Iteminfo { get; set; }

        public int Distance { get; set; }

        public int X;
        public int Y;

        public bool Pickable = false;

        public Item(ItemInfo iteminfo, uint uid)
        {
            UID = uid;
            Iteminfo = iteminfo;
            Distance = X = Y = 0;
        }
    }
}
