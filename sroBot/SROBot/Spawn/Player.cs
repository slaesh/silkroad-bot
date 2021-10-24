using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot.Spawn
{
    public class Player : MVVM.ViewModelBase
    {
        public uint UID = 0;

        public String Name
        {
            get { return GetValue(() => Name); }
            set { SetValue(() => Name, value); }
        }
        public String Guild
        {
            get { return GetValue(() => Guild); }
            set { SetValue(() => Guild, value); }
        }
        public String Nick
        {
            get { return GetValue(() => Nick); }
            set { SetValue(() => Nick, value); }
        }
        public bool Job
        {
            get { return GetValue(() => Job); }
            set { SetValue(() => Job, value); }
        }
        public uint Hp
        {
            get { return GetValue(() => Hp); }
            set { SetValue(() => Hp, value); }
        }
        public InventoryItem[] Items
        {
            get { return GetValue(() => Items); }
            set { SetValue(() => Items, value); }
        }
        public int Distance
        {
            get { return GetValue(() => Distance); }
            set { SetValue(() => Distance, value); }
        }
        public bool IsAlive
        {
            get { return GetValue(() => IsAlive); }
            set { SetValue(() => IsAlive, value); }
        }
        public bool IsStalling
        {
            get { return GetValue(() => IsStalling); }
            set { SetValue(() => IsStalling, value); }
        }
        public String StallTitle
        {
            get { return GetValue(() => StallTitle); }
            set { SetValue(() => StallTitle, value); }
        }

        public int X
        {
            get { return GetValue(() => X); }
            set { SetValue(() => X, value); }
        }
        public int Y
        {
            get { return GetValue(() => Y); }
            set { SetValue(() => Y, value); }
        }

        public Player(uint uid, String name)
        {
            UID = uid;
            Name = name;

            Guild = "";
            Nick = "";
            Job = false;
            Hp = 0;
            Items = new InventoryItem[0];
            Distance = 0;
            IsAlive = true;
            IsStalling = false;
            StallTitle = "";
            X = 0;
            Y = 0;
        }
    }
}
