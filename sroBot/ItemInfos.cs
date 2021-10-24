using sroBot.SROData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    public enum SOX_TYPE
    {
        NONE = 0,
        SoNOVA,
        SoSTAR,
        SoMOON,
        SoSUN
    }

    public class ItemInfo
    {

        public enum WEAPON_TYPE
        {
            SWORD_N_BLADE,
            SHIELD = 1,
            BOW = 6,
            SPEAR_N_GLAVIE,

            TWOHAND_SWORD=8,
            ONEHAND_SWORD=7,
            STAFF=11,
            XBOW=12,
            AXE=9,
            DAGGER=13,
            HARP=14,
            CLERICROD=15,
            WARLOCKROD=10,
            EUSHIELD = 2,

            LIGHTARMOR,
            DEVILSPIRIT,

            UNKNOWN = 255
        }

        //ID,Type,Name,Level,Stack,Durability
        public uint Model;
        public String Type { get; set; }
        public String Name { get; set; }
        public int Level { get; set; }
        public int StackSize;
        public int Durability;

        public Int32 TypeId1;
        public Int32 TypeId2;
        public Int32 TypeId3;
        public Int32 TypeId4;
        public Int32 TypeIdGroup;

        public String Icon { get; set; }

        public bool IsWeapon { get; private set; }

        public bool IsShield { get; private set; }

        public bool IsArmor { get; private set; }

        public bool IsAccessory { get; private set; }

        public bool IsHead { get; private set; }

        public bool IsChest { get; private set; }

        public bool IsLegs { get; private set; }

        public bool IsNecklace { get; private set; }

        public bool IsEarring { get; private set; }

        public bool IsRing { get; private set; }

        public bool IsChinese { get; private set; }

        public bool IsEuropean { get; private set; }

        public bool IsDrop { get; private set; }

        public bool IsAlchemy { get; private set; }

        public bool IsSOX { get; private set; }

        public SOX_TYPE SOX { get; private set; }

        public WEAPON_TYPE ItemType { get; set; }

        public int Degree { get; private set; }

        public byte Plus { get; set; }

        public ItemInfo(uint model, String type, byte plus = 0)
        {
            Model = model;
            Type = type;
            Plus = plus;

            IsChinese = (Type != null && Type.StartsWith("ITEM_CH"));
            IsEuropean = (Type != null && Type.StartsWith("ITEM_EU"));
            IsDrop = (IsEuropean || IsChinese) && Type != null && !Type.Contains("_FRPVP_") && !Type.Contains("_TRADE_");
            IsSOX = (Type != null && Type.Contains("_RARE"));
            IsAlchemy = (Type != null && Type.StartsWith("ITEM_ETC_ARCHEMY_"));

            IsWeapon = (Type != null &&
                (Type.StartsWith("ITEM_CH_SWORD") ||
                Type.StartsWith("ITEM_CH_BLADE") ||
                Type.StartsWith("ITEM_CH_SPEAR") ||
                Type.StartsWith("ITEM_CH_TBLADE") ||
                Type.StartsWith("ITEM_CH_BOW") ||
                Type.StartsWith("ITEM_EU_DAGGER") ||
                Type.StartsWith("ITEM_EU_SWORD") ||
                Type.StartsWith("ITEM_EU_TSWORD") ||
                Type.StartsWith("ITEM_EU_AXE") ||
                Type.StartsWith("ITEM_EU_CROSSBOW") ||
                Type.StartsWith("ITEM_EU_DARKSTAFF") ||
                Type.StartsWith("ITEM_EU_TSTAFF") ||
                Type.StartsWith("ITEM_EU_HARP") ||
                Type.StartsWith("ITEM_EU_STAFF")));

            IsArmor = (Type != null &&
                    (Type.StartsWith("ITEM_CH_M_HEAVY") ||
                    Type.StartsWith("ITEM_CH_M_LIGHT") ||
                    Type.StartsWith("ITEM_CH_M_CLOTHES") ||
                    Type.StartsWith("ITEM_CH_W_HEAVY") ||
                    Type.StartsWith("ITEM_CH_W_LIGHT") ||
                    Type.StartsWith("ITEM_CH_W_CLOTHES") ||
                    Type.StartsWith("ITEM_EU_M_HEAVY") ||
                    Type.StartsWith("ITEM_EU_M_LIGHT") ||
                    Type.StartsWith("ITEM_EU_M_CLOTHES") ||
                    Type.StartsWith("ITEM_EU_W_HEAVY") ||
                    Type.StartsWith("ITEM_EU_W_LIGHT") ||
                    Type.StartsWith("ITEM_EU_W_CLOTHES")));

            IsShield = (Type != null &&
                (Type.StartsWith("ITEM_CH_SHIELD") ||
                Type.StartsWith("ITEM_EU_SHIELD")));

            IsAccessory = (Type != null &&
                    (Type.StartsWith("ITEM_EU_RING") ||
                    Type.StartsWith("ITEM_EU_EARRING") ||
                    Type.StartsWith("ITEM_EU_NECKLACE") ||
                    Type.StartsWith("ITEM_CH_RING") ||
                    Type.StartsWith("ITEM_CH_EARRING") ||
                    Type.StartsWith("ITEM_CH_NECKLACE")));

            IsHead = IsArmor && (Type.Contains("_HA_") || Type.Contains("_CA_"));
            IsChest = IsArmor && Type.Contains("_BA_"); /* richtig ?? */
            IsLegs = IsArmor && Type.Contains("_LA_");

            IsNecklace = IsAccessory && Type.Contains("_NECKLACE_");
            IsEarring = IsAccessory && Type.Contains("_EARRING_");
            IsRing = IsAccessory && Type.Contains("_RING_");

            ItemType = WEAPON_TYPE.UNKNOWN;
            if (IsDrop && Type != null)
            {
                if (IsChinese)
                {
                    if (Type.StartsWith("ITEM_CH_SWORD") || type.StartsWith("ITEM_CH_BLADE"))
                    {
                        ItemType = WEAPON_TYPE.SWORD_N_BLADE;
                    }
                    else if (type.StartsWith("ITEM_CH_SPEAR") || type.StartsWith("ITEM_CH_TBLADE"))
                    {
                        ItemType = WEAPON_TYPE.SPEAR_N_GLAVIE;
                    }
                    else if (type.StartsWith("ITEM_CH_BOW"))
                    {
                        ItemType = WEAPON_TYPE.BOW;
                    }
                    else if (type.StartsWith("ITEM_CH_SHIELD"))
                    {
                        ItemType = WEAPON_TYPE.SHIELD;
                    }
                }
                else if (IsEuropean)
                {
                    if (Type.StartsWith("ITEM_EU_DAGGER"))
                    {
                        ItemType = WEAPON_TYPE.DAGGER;
                    }
                    else if (type.StartsWith("ITEM_EU_SWORD"))
                    {
                        ItemType = WEAPON_TYPE.ONEHAND_SWORD;
                    }
                    else if (type.StartsWith("ITEM_EU_TSWORD"))
                    {
                        ItemType = WEAPON_TYPE.TWOHAND_SWORD;
                    }
                    else if (type.StartsWith("ITEM_EU_AXE"))
                    {
                        ItemType = WEAPON_TYPE.AXE;
                    }
                    else if (type.StartsWith("ITEM_EU_CROSSBOW"))
                    {
                        ItemType = WEAPON_TYPE.XBOW;
                    }
                    else if (type.StartsWith("ITEM_EU_DARKSTAFF"))
                    {
                        ItemType = WEAPON_TYPE.WARLOCKROD;
                    }
                    else if (type.StartsWith("ITEM_EU_TSTAFF"))
                    {
                        ItemType = WEAPON_TYPE.STAFF;
                    }
                    else if (type.StartsWith("ITEM_EU_HARP"))
                    {
                        ItemType = WEAPON_TYPE.HARP;
                    }
                    else if (type.StartsWith("ITEM_EU_STAFF"))
                    {
                        ItemType = WEAPON_TYPE.CLERICROD;
                    }
                    else if (type.StartsWith("ITEM_EU_M_LIGHT") || type.StartsWith("ITEM_EU_W_LIGHT"))
                    {
                        ItemType = WEAPON_TYPE.LIGHTARMOR;
                    }
                }
            }

            if (Type.EndsWith("A_RARE") && Type.Contains("_11_"))
            {
                SOX = SOX_TYPE.SoNOVA;
            }
            else if (Type.EndsWith("A_RARE"))
            {
                SOX = SOX_TYPE.SoSTAR;
            }
            else if (Type.EndsWith("B_RARE"))
            {
                SOX = SOX_TYPE.SoMOON;
            }
            else if (Type.EndsWith("C_RARE"))
            {
                SOX = SOX_TYPE.SoSUN;
            }
            else SOX = SOX_TYPE.NONE;

            if (!IsDrop) Degree = 0;
            else
            {
                try
                {
                    if (IsArmor)
                    {
                        Degree = int.Parse(Type.Split('_')[4]);
                    }
                    else
                    {
                        Degree = int.Parse(Type.Split('_')[3]);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine(Type);
                    Degree = 0;
                }
            }
        }

        public bool CanGetStone(String moType)
        {
            return CanGetStone(MagicOptions.Current.FirstOrDefault(mo => mo.Type.Equals(moType) && mo.Degree == Degree));
        }

        public bool CanGetStone(MagicOption mo)
        {
            if (mo == null) return false;

            return (IsWeapon && mo.ForWeapon) ||
                   (IsShield && mo.ForShield) ||
                   (IsArmor && mo.ForArmor) ||
                   (IsAccessory && mo.ForAccessory) ||
                   (IsHead && mo.ForHead) ||
                   (IsChest && mo.ForChest) ||
                   (IsLegs && mo.ForLegs) ||
                   (IsNecklace && mo.ForNecklace) ||
                   (IsEarring && mo.ForEarring) ||
                   (IsRing && mo.ForRing);
        }
        
        public int GetVirtualPlus()
        {
            var virtualPlus = Plus;

            if (IsSOX)
            {
                switch (SOX)
                {
                    case SOX_TYPE.SoNOVA:
                        virtualPlus += 5;
                        break;

                    case SOX_TYPE.SoSTAR:
                        virtualPlus += 5;
                        break;

                    case SOX_TYPE.SoMOON:
                        virtualPlus += 10;
                        break;

                    case SOX_TYPE.SoSUN:
                        virtualPlus += 16;
                        break;
                }
            }

            return virtualPlus;
        }

        /// <summary>
        /// Check if THIS item is better than the other one.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsBetterThan(ItemInfo item)
        {
            if ((this.Level + GetVirtualPlus()) > (item.Level + item.GetVirtualPlus()))
            {
                return true;
            }

            // maybe check white stats .. !?

            return false;
        }

        public string GetWeaponType()
        {
            return Type.Split('_')[2];
        }
    }

    public class ItemInfos
    {
        public static List<ItemInfo> ItemList = new List<ItemInfo>();

        public static void Load()
        {
            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_items.txt");
            if (!File.Exists(f)) return;

            using (var sr = new StreamReader(f))
            {
                var line = sr.ReadLine(); // skip first line
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    var splitted = line.Split(',');
                    try
                    {
                        ItemList.Add(new ItemInfo(uint.Parse(splitted[0]), splitted[1])
                        {
                            Name = splitted[2],
                            Level = int.Parse(splitted[3]),
                            StackSize = int.Parse(splitted[4]),
                            Durability = int.Parse(splitted[5]),
                            TypeId1 = int.Parse(splitted[6]),
                            TypeId2 = int.Parse(splitted[7]),
                            TypeId3 = int.Parse(splitted[8]),
                            TypeId4 = int.Parse(splitted[9]),
                            TypeIdGroup = int.Parse(splitted[10]),
                            Icon = splitted[11]
                        });
                    }
                    catch
                    {
                        //Console.WriteLine(line);
                    }
                }
            }
        }

        public static int GetIndexById(uint id)
        {
            var item = ItemList.FirstOrDefault(i => i.Model == id);
            return item == null ? -1 : ItemList.IndexOf(item);
        }

        public static ItemInfo GetById(uint id)
        {
            return ItemList.FirstOrDefault(i => i.Model == id)?.Copy();
        }

        public static ItemInfo GetByType(string id)
        {
            return ItemList.FirstOrDefault(i => i.Type == id)?.Copy();
        }
    }
}
