using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    public class Pet : Mob
    {
        public ushort HGP = 0;
        public String Name = "";
        public uint OwnerId = 0;
        public String Owner = "";
        public SROBot.Inventory Inventory;

        public Pet(MobInfo petinfo, uint uid) : base(petinfo, uid) { }
    }
}
