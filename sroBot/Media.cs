namespace sroBot
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    internal class Media
    {
        public static Dictionary<int, string> Areas = new Dictionary<int, string>();
        public static Dictionary<uint, Item> Items = new Dictionary<uint, Item>();
        public static Dictionary<uint, Monster> Mobs = new Dictionary<uint, Monster>();
        public static Dictionary<uint, Object> Objects = new Dictionary<uint, Object>();
        public static Dictionary<uint, Shop> Shops = new Dictionary<uint, Shop>();
        public static Dictionary<uint, Skill> Skills = new Dictionary<uint, Skill>();
        public static Dictionary<uint, MagicOption> MagicOptions = new Dictionary<uint, MagicOption>();
        public static Dictionary<uint, Portal> Portals = new Dictionary<uint, Portal>();

        public static shopdata GetShopDataByItemId(uint Pk2Id, uint ItemId)
        {
            Media.shopdata shopdata = new Media.shopdata();
            for (byte i = 0; i < Shops[Pk2Id].Items.Count; i = (byte) (i + 1))
            {
                for (byte j = 0; j < Shops[Pk2Id].Items[i].Count; j = (byte) (j + 1))
                {
                    if (Shops[Pk2Id].Items[i][j] == ItemId)
                    {
                        shopdata.Tab = i;
                        shopdata.ItemPosition = j;
                    }
                }
            }
            return shopdata;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Monster
        {
            public uint Pk2Id;
            public string Pk2Name;
            public string Name;
            public byte Level;
            public uint HP;
            public byte TypeId1;
            public byte TypeId2;
            public byte TypeId3;
            public byte TypeId4;

            public Monster(uint pk2id, string pk2name, string name, byte level, uint hp, byte tId1, byte tId2, byte tId3, byte tId4)
            {
                Pk2Id = pk2id;
                Pk2Name = pk2name;
                Name = name;
                Level = level;
                HP = hp;
                TypeId1 = tId1;
                TypeId2 = tId2;
                TypeId3 = tId3;
                TypeId4 = tId4;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Item
        {
            public uint Pk2Id;
            public string Pk2Name;
            public string Name;
            public Int32 TypeId1;
            public Int32 TypeId2;
            public Int32 TypeId3;
            public Int32 TypeId4;
            public Int32 TypeIdGroup;
            public byte Race;
            public byte Level;
            public uint StackSize;
            public uint Durability;
            public String Icon;

            public Item(uint pk2Id, string pk2Name, string name, Int32 typeId1, Int32 typeId2, Int32 typeId3, Int32 typeId4, Int32 typeIdGroup, byte race, byte level, uint stackSize, uint durability, String icon)
            {
                Pk2Id = pk2Id;
                Pk2Name = pk2Name;
                Name = name;
                TypeId1 = typeId2;
                TypeId2 = typeId2;
                TypeId3 = typeId3;
                TypeId4 = typeId4;
                TypeIdGroup = typeIdGroup;
                Race = race;
                Level = level;
                StackSize = stackSize;
                Durability = durability;
                Icon = icon;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Object
        {
            public uint Pk2Id;
            public string Pk2Name;
            public string Name;
            public byte Level;
            public uint MaximumHitpoints;
            public Object(uint Pk2Id, string Pk2Name, string Name, byte Level, uint MaximumHitpoints)
            {
                this.Pk2Id = Pk2Id;
                this.Pk2Name = Pk2Name;
                this.Name = Name;
                this.Level = Level;
                this.MaximumHitpoints = MaximumHitpoints;
            }
        }

        public class Shop
        {
            public List<List<uint>> Items = new List<List<uint>>();
            public uint Pk2Id;

            public Shop(uint Pk2Id, List<List<uint>> Items)
            {
                this.Pk2Id = Pk2Id;
                this.Items = Items;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct shopdata
        {
            public byte Tab;
            public byte ItemPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Skill
        {
            public uint Pk2Id;
            public uint SkillId;
            public string Pk2Name;
            public string Name;
            public byte RequiredMastery1Level;
            public ushort MP;
            public ulong CastTime;
            public ulong Cooldown;
            public ulong Duration;
            public uint RequiredMastery1;
            public ushort SkillGroup;
            public String Icon;
            public uint SPNeeded;
            public long CooldownId;
            public bool NeedsTarget;
            public Dictionary<String, int> Attributes;
            public IEnumerable<ItemInfo.WEAPON_TYPE> RequiredItems;
            public ItemInfo.WEAPON_TYPE WeaponToUse;
            public byte WeaponType1;
            public byte WeaponType2;

            public uint RequiredMastery2;
            public byte RequiredMastery2Level;
            public uint RequiredStr;
            public uint RequiredInt;
            public uint RequiredSkill1;
            public uint RequiredSkill2;
            public uint RequiredSkill3;
            public byte RequiredSkill1Level;
            public byte RequiredSkill2Level;
            public byte RequiredSkill3Level;

            public ushort SkillGroupIndex;

            public Skill(
                uint Pk2Id,
                uint skillId,
                string Pk2Name,
                string Name,
                byte Level,
                ushort reqMp,
                ulong CastTime, 
                ulong cooldown, 
                ulong duration, 
                uint mastery, 
                ushort skillgroup, 
                String icon, 
                uint spneeded, 
                long cooldownId, 
                bool needsTarget, 
                ItemInfo.WEAPON_TYPE weaponToUse, 
                Dictionary<String, int> skillAttributes, 
                IEnumerable<ItemInfo.WEAPON_TYPE> requiredItems, 
                byte weaponType1, 
                byte weaponType2,
                uint reqMastery2,
                byte reqMaster2Lvl,
                uint reqStr,
                uint reqInt,
                uint reqSkill1,
                uint reqSkill2,
                uint reqSkill3,
                byte reqSkill1Lvl,
                byte reqSkill2Lvl,
                byte reqSkill3Lvl,
                ushort skillGroupIndex
                )
            {
                this.Pk2Id = Pk2Id;
                this.SkillId = skillId;
                this.Pk2Name = Pk2Name;
                this.Name = Name;
                this.RequiredMastery1Level = Level;
                this.MP = reqMp;
                this.CastTime = CastTime;
                this.Cooldown = cooldown;
                this.Duration = duration;
                RequiredMastery1 = mastery;
                SkillGroup = skillgroup;
                this.Icon = icon;
                SPNeeded = spneeded;
                CooldownId = cooldownId;
                NeedsTarget = needsTarget;
                Attributes = skillAttributes;
                RequiredItems = requiredItems;
                WeaponToUse = weaponToUse;
                WeaponType1 = weaponType1;
                WeaponType2 = weaponType2;
                RequiredMastery2 = reqMastery2;
                RequiredMastery2Level = reqMaster2Lvl;
                RequiredStr = reqStr;
                RequiredInt = reqInt;
                RequiredSkill1 = reqSkill1;
                RequiredSkill2 = reqSkill2;
                RequiredSkill3 = reqSkill3;
                RequiredSkill1Level = reqSkill1Lvl;
                RequiredSkill2Level = reqSkill2Lvl;
                RequiredSkill3Level = reqSkill3Lvl;
                SkillGroupIndex = skillGroupIndex;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MagicOption
        {
            public uint Pk2Id;
            public String Pk2Name;
            public String Name;
            public byte Degree;

            public bool ForWeapon;
            public bool ForShield;
            public bool ForArmor;
            public bool ForAccessory;
            public bool ForHead;
            public bool ForChest;
            public bool ForLegs;
            public bool ForNecklace;
            public bool ForEarring;
            public bool ForRing;

            public MagicOption(uint Pk2Id, String pk2name, String name, byte dg, bool forWeapon, bool forShield, bool forArmor, bool forAccessory, bool forHead, bool forChest, bool forLegs, bool forNecklace, bool forEarring, bool forRing)
            {
                this.Pk2Id = Pk2Id;
                Pk2Name = pk2name;
                Name = name;
                Degree = dg;

                ForWeapon = forWeapon;
                ForShield = forShield;
                ForArmor = forArmor;
                ForAccessory = forAccessory;
                ForHead = forHead;
                ForChest = forChest;
                ForLegs = forLegs;
                ForNecklace = forNecklace;
                ForEarring = forEarring;
                ForRing = forRing;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class Portal
        {
            public uint Pk2Id;
            public String Pk2Name;
            public String Name;
            public uint Pk2Model;
            public uint[] Links;

            public Portal(uint Pk2Id, String pk2name, String name, uint pk2model)
            {
                this.Pk2Id = Pk2Id;
                Pk2Name = pk2name;
                Name = name;
                Pk2Model = pk2model;
                Links = new uint[0];
            }
        }
    }
}

