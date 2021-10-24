namespace sroBot.SROData.pk2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class Textdata
    {
        private class TidGroup
        {
            public int Id;
            public int Tid1;
            public int Tid2;
            public int Tid3;
            public int Tid4;
        }
        private List<TidGroup> tidGroupMapping = new List<TidGroup>();
        private Dictionary<string, string> dTextDataName = new Dictionary<string, string>();

        public Textdata(byte[] file)
        {
            StreamReader reader = new StreamReader(new MemoryStream(file));
            while (!reader.EndOfStream)
            {
                string name = reader.ReadLine();
                this.ParseTextData(MainWindow.reader.getFile(name));
            }
        }

        public void Dispose()
        {
            this.dTextDataName = null;
        }

        internal void GetItems(byte[] p)
        {
            ParseItemGroupMapping(MainWindow.reader.getFile("fmntidgroupmapdata.txt"));

            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string name = reader.ReadLine();
                this.ParseItemData(MainWindow.reader.getFile(name));
            }
        }

        internal void GetMobs(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string name = reader.ReadLine();
                this.ParseMobData(MainWindow.reader.getFile(name));
            }
        }

        internal void GetSkills(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string name = reader.ReadLine();
                this.ParseSkillData(name, MainWindow.reader.getFile(name));
            }
        }

        internal void GetPortals(byte[] p)
        {
            ParsePortalData(p);
            ParsePortalLinks(MainWindow.reader.getFile("teleportlink.txt"));
        }

        internal void GetMagicOptions(byte[] p)
        {
            ParseMagicOptionData(p);
        }

        internal void GetShops()
        {
            //var tabs = ParseShopTabs(MainWindow.reader.getFile("shoptabdata.txt"));
            //ParseShopGoods(tabs, MainWindow.reader.getFile("refshopgoods.txt"));
            //ParseShops(tabs, MainWindow.reader.getFile("shopdata.txt"));

            var tabs = ParseRefShopTabs(MainWindow.reader.getFile("refshoptab.txt"));
            var pkgItems = PareRefScrapOfPackageItems(MainWindow.reader.getFile("refscrapofpackageitem.txt"));
            var prices = ParseRefPricePolicyOfItem(MainWindow.reader.getFile("refpricepolicyofitem.txt"));
            ParseRefShopGoods(tabs, pkgItems, prices, MainWindow.reader.getFile("refshopgoods.txt"));
            ParseRefShop(tabs, MainWindow.reader.getFile("refshop.txt"), MainWindow.reader.getFile("shopdata.txt"));

        }

        internal void GetEpData()
        {
            ParseEp(MainWindow.reader.getFile("leveldata.txt"));
        }

        private string GetNameFromPk2Name(string Pk2Name)
        {
            if (this.dTextDataName.ContainsKey(Pk2Name))
            {
                if (Pk2Name.EndsWith("A_RARE") && Pk2Name.Contains("_11_"))
                {
                    return (this.dTextDataName[Pk2Name] + " (Seal of Nova)");
                }
                if (Pk2Name.EndsWith("A_RARE"))
                {
                    //return (this.dTextDataName[Pk2Name] + " (Seal of Star)");
                    return (this.dTextDataName[Pk2Name] + " (Seal of Nova)");
                }
                if (Pk2Name.EndsWith("B_RARE"))
                {
                    return (this.dTextDataName[Pk2Name] + " (Seal of Moon)");
                }
                if (Pk2Name.EndsWith("C_RARE"))
                {
                    return (this.dTextDataName[Pk2Name] + " (Seal of Sun)");
                }
                return this.dTextDataName[Pk2Name];
            }
            return null;
        }

        public void IP(byte[] p)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(p));
            byte num = reader.ReadByte();
            byte num2 = reader.ReadByte();
            for (int i = 0; i < num2; i++)
            {
                var len = reader.ReadInt32();
                var div = new string(reader.ReadChars(len));
                reader.ReadByte();
                reader.ReadByte();
                len = reader.ReadInt32();
                var ip = new string(reader.ReadChars(len));
                reader.ReadByte();

                Console.WriteLine(ip);

                break; // take first division !!
            }
            //var _len = reader.ReadInt32();
            //string str2 = new string(reader.ReadChars(_len));
            //Config.server_ip = str2;
        }

        private List<NPCTab> ParseRefShopTabs(byte[] p)
        {
            var tabs = new List<NPCTab>();
            var reader = new StreamReader(new MemoryStream(p));

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });

                try
                {
                    var id = uint.Parse(strArray[2]);
                    var type = strArray[3];

                    tabs.Add(new NPCTab()
                    {
                        TabType = type,
                        ShopType = string.Join("_", type.Replace("_EU_TAB", "_TAB").Split('_').Reverse().Skip(1).Reverse())
                    });
                }
                catch { }
            }

            return tabs;
        }

        private Dictionary<string, Tuple<uint, byte>> PareRefScrapOfPackageItems(byte[] p)
        {
            var pkgItems = new Dictionary<string, Tuple<uint, byte>>();
            var reader = new StreamReader(new MemoryStream(p));

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });

                try
                {
                    var pkgType = strArray[2];
                    var itemType = strArray[3];
                    var plus = byte.Parse(strArray[4]);

                    pkgItems[pkgType] = new Tuple<uint, byte>(ItemInfos.GetByType(itemType)?.Model ?? 0, plus);
                }
                catch { }
            }

            return pkgItems;
        }

        private Dictionary<string, UInt64> ParseRefPricePolicyOfItem(byte[] p)
        {
            var prices = new Dictionary<string, UInt64>();
            var reader = new StreamReader(new MemoryStream(p));

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });

                try
                {
                    var pkgType = strArray[2];
                    var price = UInt64.Parse(strArray[5]);

                    prices[pkgType] = price;
                }
                catch { }
            }

            return prices;
        }

        private void ParseRefShopGoods(List<NPCTab> tabs, Dictionary<string, Tuple<uint, byte>> pkgItems, Dictionary<string, UInt64> prices, byte[] p)
        {
            var reader = new StreamReader(new MemoryStream(p));

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });

                try
                {
                    var tabType = strArray[2];
                    var packageType = strArray[3];
                    var idx = byte.Parse(strArray[4]);

                    var tab = tabs.FirstOrDefault(t => tabType == t.TabType);
                    if (tab == null)
                    {
                        Console.WriteLine($"konnte tab nicht finden.. {tabType}");
                        continue;
                    }
                    if (!pkgItems.ContainsKey(packageType))
                    {
                        Console.WriteLine($"konnte kein passendes item-package finden.. {packageType}");
                        continue;
                    }

                    tab.ItemModels.Add(new NpcItem()
                    {
                        Model = pkgItems[packageType].Item1,
                        Plus = pkgItems[packageType].Item2,
                        IndexOfTab = idx,
                        Price = prices.ContainsKey(packageType) ? prices[packageType] : 0
                    });
                }
                catch { }
            }
        }

        private void ParseRefShop(List<NPCTab> tabs, byte[] pRefShopData, byte[] pShopData)
        {
            var reader = new StreamReader(new MemoryStream(pShopData));
            var shopModels = new Dictionary<string, uint>();

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });

                try
                {
                    var shopType = strArray[2];
                    var sShopModel = strArray[5];
                    uint shopModel = 0;

                    if (!sShopModel.StartsWith("-"))
                    {
                        shopModel = uint.Parse(sShopModel);
                    }

                    shopModels[shopType] = shopModel;
                }
                catch { }
            }

            reader = new StreamReader(new MemoryStream(pRefShopData));

            NPCs.Current.Clear();

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });

                try
                {
                    var id = uint.Parse(strArray[2]);
                    var type = strArray[3];

                    if (!shopModels.ContainsKey(type))
                    {
                        Console.WriteLine($"konte kein passendes model finden.. {type}");
                        continue;
                    }

                    var npc = new SROData.NPC()
                    {
                        Model = shopModels[type]
                    };

                    var npcTabs = new List<NPCTab>();
                    npcTabs.AddRange(tabs.Where(t => t.ShopType == type).Where(t => t.TabType.Contains("_EU_TAB")));
                    npcTabs.AddRange(tabs.Where(t => t.ShopType == type).Where(t => !t.TabType.Contains("_EU_TAB")));
                    //npc.Tabs = tabs.Where(t => t.ShopType == type).ToArray();
                    npc.Tabs = npcTabs.ToArray();

                    SROData.NPCs.Current.Add(npc);
                }
                catch { }
            }
        }

        private void ParseItemData(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                try
                {
                    if (strArray.Length > 1 && strArray[1] != "")
                    {
                        uint key = Convert.ToUInt32(strArray[1]);
                        if (!Media.Items.ContainsKey(key))
                        {
                            if (key == 39142)
                            {
                                Console.WriteLine("!!");
                            }

                            var tmpLvl = strArray[33].Replace("-1", "0").Split(' ')[0];

                            var TypeID1 = Convert.ToInt32(strArray[9]);
                            var TypeID2 = Convert.ToInt32(strArray[10]);
                            var TypeID3 = Convert.ToInt32(strArray[11]);
                            var TypeID4 = Convert.ToInt32(strArray[12]);
                            var typeIdGroup = tidGroupMapping.FirstOrDefault(m => m.Tid1 == TypeID1 && m.Tid2 == TypeID2 && m.Tid3 == TypeID3 && m.Tid4 == TypeID4)?.Id ?? 0;
                            var Race = Convert.ToByte(strArray[14]);
                            var SOX = Convert.ToByte(strArray[15]);
                            var SoulBound = Convert.ToByte(strArray[18]);
                            var Shop_price = Convert.ToInt32(strArray[26]);
                            var Storage_price = Convert.ToInt32(strArray[30]);
                            var Sell_Price = Convert.ToInt32(strArray[31]);
                            //var Level = Convert.ToByte(strArray[33]); // siehe oben !
                            var Max_Stack = Convert.ToUInt32(strArray[57]);
                            var Gender = Convert.ToByte(strArray[58]);
                            var Degree = Convert.ToByte(strArray[61]);

                            Media.Items.Add(key, new Media.Item(key,
                                                                strArray[2],
                                                                GetNameFromPk2Name(strArray[5]),
                                                                TypeID1,
                                                                TypeID2,
                                                                TypeID3,
                                                                TypeID4,
                                                                typeIdGroup,
                                                                Race,
                                                                Convert.ToByte(tmpLvl),
                                                                Max_Stack,
                                                                (uint)Convert.ToSingle(strArray[63]),
                                                                strArray[54]
                                                                ));
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("could not parse item..");
                }
            }
        }

        private void ParseItemGroupMapping(byte[] p)
        {
            var reader = new StreamReader(new MemoryStream(p));

            tidGroupMapping.Clear();

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split('\t');

                tidGroupMapping.Add(new TidGroup
                {
                    Id = int.Parse(strArray[1]),
                    Tid1 = int.Parse(strArray[2]),
                    Tid2 = int.Parse(strArray[3]),
                    Tid3 = int.Parse(strArray[4]),
                    Tid4 = int.Parse(strArray[5])
                });
            }
        }

        private void ParseMobData(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    uint key = Convert.ToUInt32(strArray[1]);
                    var tId1 = byte.Parse(strArray[9]);
                    var tId2 = byte.Parse(strArray[10]);
                    var tId3 = byte.Parse(strArray[11]);
                    var tId4 = byte.Parse(strArray[12]);

                    if (!Media.Mobs.ContainsKey(key))
                    {
                        Media.Mobs.Add(key, new Media.Monster(
                                                        key,
                                                        strArray[2],
                                                        this.GetNameFromPk2Name(strArray[5]),
                                                        Convert.ToByte(strArray[57]),
                                                        Convert.ToUInt32(strArray[59]),
                                                        tId1,
                                                        tId2,
                                                        tId3,
                                                        tId4
                                                        ));
                    }
                }
            }
        }

        public static byte[] decryptData(byte[] data)
        {
            var Hash_Table_1 = new byte[]
            {
                0x07, 0x83, 0xBC, 0xEE, 0x4B, 0x79, 0x19, 0xB6, 0x2A, 0x53, 0x4F, 0x3A, 0xCF, 0x71, 0xE5, 0x3C,
                0x2D, 0x18, 0x14, 0xCB, 0xB6, 0xBC, 0xAA, 0x9A, 0x31, 0x42, 0x3A, 0x13, 0x42, 0xC9, 0x63, 0xFC,
                0x54, 0x1D, 0xF2, 0xC1, 0x8A, 0xDD, 0x1C, 0xB3, 0x52, 0xEA, 0x9B, 0xD7, 0xC4, 0xBA, 0xF8, 0x12,
                0x74, 0x92, 0x30, 0xC9, 0xD6, 0x56, 0x15, 0x52, 0x53, 0x60, 0x11, 0x33, 0xC5, 0x9D, 0x30, 0x9A,
                0xE5, 0xD2, 0x93, 0x99, 0xEB, 0xCF, 0xAA, 0x79, 0xE3, 0x78, 0x6A, 0xB9, 0x02, 0xE0, 0xCE, 0x8E,
                0xF3, 0x63, 0x5A, 0x73, 0x74, 0xF3, 0x72, 0xAA, 0x2C, 0x9F, 0xBB, 0x33, 0x91, 0xDE, 0x5F, 0x91,
                0x66, 0x48, 0xD1, 0x7A, 0xFD, 0x3F, 0x91, 0x3E, 0x5D, 0x22, 0xEC, 0xEF, 0x7C, 0xA5, 0x43, 0xC0,
                0x1D, 0x4F, 0x60, 0x7F, 0x0B, 0x4A, 0x4B, 0x2A, 0x43, 0x06, 0x46, 0x14, 0x45, 0xD0, 0xC5, 0x83,
                0x92, 0xE4, 0x16, 0xD0, 0xA3, 0xA1, 0x13, 0xDA, 0xD1, 0x51, 0x07, 0xEB, 0x7D, 0xCE, 0xA5, 0xDB,
                0x78, 0xE0, 0xC1, 0x0B, 0xE5, 0x8E, 0x1C, 0x7C, 0xB4, 0xDF, 0xED, 0xB8, 0x53, 0xBA, 0x2C, 0xB5,
                0xBB, 0x56, 0xFB, 0x68, 0x95, 0x6E, 0x65, 0x00, 0x60, 0xBA, 0xE3, 0x00, 0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x9C, 0xB5, 0xD5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x3F, 0x41, 0x56,
                0x43, 0x45, 0x53, 0x63, 0x72, 0x69, 0x70, 0x74, 0x40, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x64, 0xBB, 0xE3, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            var Hash_Table_2 = new byte[]
            {
                0x0D, 0x05, 0x90, 0x41, 0xF9, 0xD0, 0x65, 0xBF, 0xF9, 0x0B, 0x15, 0x93, 0x80, 0xFB, 0x01, 0x02,
                0xB6, 0x08, 0xC4, 0x3C, 0xC1, 0x49, 0x94, 0x4D, 0xCE, 0x1D, 0xFD, 0x69, 0xEA, 0x19, 0xC9, 0x57,
                0x9C, 0x4D, 0x84, 0x62, 0xE3, 0x67, 0xF9, 0x87, 0xF4, 0xF9, 0x93, 0xDA, 0xE5, 0x15, 0xF1, 0x4C,
                0xA4, 0xEC, 0xBC, 0xCF, 0xDD, 0xB3, 0x6F, 0x04, 0x3D, 0x70, 0x1C, 0x74, 0x21, 0x6B, 0x00, 0x71,
                0x31, 0x7F, 0x54, 0xB3, 0x72, 0x6C, 0xAA, 0x42, 0xC1, 0x78, 0x61, 0x3E, 0xD5, 0xF2, 0xE1, 0x27,
                0x36, 0x71, 0x3A, 0x25, 0x36, 0x57, 0xD1, 0xF8, 0x70, 0x86, 0xBD, 0x0E, 0x58, 0xB3, 0x76, 0x6D,
                0xC3, 0x50, 0xF6, 0x6C, 0xA0, 0x10, 0x06, 0x64, 0xA2, 0xD6, 0x2C, 0xD4, 0x27, 0x30, 0xA5, 0x36,
                0x1C, 0x1E, 0x3E, 0x58, 0x9D, 0x59, 0x76, 0x9D, 0xA7, 0x42, 0x5A, 0xF0, 0x00, 0xBC, 0x69, 0x31,
                0x40, 0x1E, 0xFA, 0x09, 0x1D, 0xE7, 0xEE, 0xE4, 0x54, 0x89, 0x36, 0x7C, 0x67, 0xC8, 0x65, 0x22,
                0x7E, 0xA3, 0x60, 0x44, 0x1E, 0xBC, 0x68, 0x6F, 0x15, 0x2A, 0xFD, 0x9D, 0x3F, 0x36, 0x6B, 0x28,
                0x06, 0x67, 0xFE, 0xC6, 0x49, 0x6B, 0x9B, 0x3F, 0x80, 0x2A, 0xD2, 0xD4, 0xD3, 0x20, 0x1B, 0x96,
                0xF4, 0xD2, 0xCA, 0x8C, 0x74, 0xEE, 0x0B, 0x6A, 0xE1, 0xE9, 0xC6, 0xD2, 0x6E, 0x33, 0x63, 0xC0,
                0xE9, 0xD0, 0x37, 0xA9, 0x3C, 0xF7, 0x18, 0xF2, 0x4A, 0x74, 0xEC, 0x41, 0x61, 0x7A, 0x19, 0x47,
                0x8F, 0xA0, 0xBB, 0x94, 0x8F, 0x3D, 0x11, 0x11, 0x26, 0xCF, 0x69, 0x18, 0x1B, 0x2C, 0x87, 0x6D,
                0xB3, 0x22, 0x6C, 0x78, 0x41, 0xCC, 0xC2, 0x84, 0xC5, 0xCB, 0x01, 0x6A, 0x37, 0x00, 0x01, 0x65,
                0x4F, 0xA7, 0x85, 0x85, 0x15, 0x59, 0x05, 0x67, 0xF2, 0x4F, 0xAB, 0xB7, 0x88, 0xFA, 0x69, 0x24,
                0x9E, 0xC6, 0x7B, 0x3F, 0xD5, 0x0E, 0x4D, 0x7B, 0xFB, 0xB1, 0x21, 0x3C, 0xB0, 0xC0, 0xCB, 0x2C,
                0xAA, 0x26, 0x8D, 0xCC, 0xDD, 0xDA, 0xC1, 0xF8, 0xCA, 0x7F, 0x6A, 0x3F, 0x2A, 0x61, 0xE7, 0x60,
                0x5C, 0xCE, 0xD3, 0x4C, 0xAC, 0x45, 0x40, 0x62, 0xEA, 0x51, 0xF1, 0x66, 0x5D, 0x2C, 0x45, 0xD6,
                0x8B, 0x7D, 0xCE, 0x9C, 0xF5, 0xBB, 0xF7, 0x52, 0x24, 0x1A, 0x13, 0x02, 0x2B, 0x00, 0xBB, 0xA1,
                0x8F, 0x6E, 0x7A, 0x33, 0xAD, 0x5F, 0xF4, 0x4A, 0x82, 0x76, 0xAB, 0xDE, 0x80, 0x98, 0x8B, 0x26,
                0x4F, 0x33, 0xD8, 0x68, 0x1E, 0xD9, 0xAE, 0x06, 0x6B, 0x7E, 0xA9, 0x95, 0x67, 0x60, 0xEB, 0xE8,
                0xD0, 0x7D, 0x07, 0x4B, 0xF1, 0xAA, 0x9A, 0xC5, 0x29, 0x93, 0x9D, 0x5C, 0x92, 0x3F, 0x15, 0xDE,
                0x48, 0xF1, 0xCA, 0xEA, 0xC9, 0x78, 0x3C, 0x28, 0x7E, 0xB0, 0x46, 0xD3, 0x71, 0x6C, 0xD7, 0xBD,
                0x2C, 0xF7, 0x25, 0x2F, 0xC7, 0xDD, 0xB4, 0x6D, 0x35, 0xBB, 0xA7, 0xDA, 0x3E, 0x3D, 0xA7, 0xCA,
                0xBD, 0x87, 0xDD, 0x9F, 0x22, 0x3D, 0x50, 0xD2, 0x30, 0xD5, 0x14, 0x5B, 0x8F, 0xF4, 0xAF, 0xAA,
                0xA0, 0xFC, 0x17, 0x3D, 0x33, 0x10, 0x99, 0xDC, 0x76, 0xA9, 0x40, 0x1B, 0x64, 0x14, 0xDF, 0x35,
                0x68, 0x66, 0x5B, 0x49, 0x05, 0x33, 0x68, 0x26, 0xC8, 0xBA, 0xD1, 0x8D, 0x39, 0x2B, 0xFB, 0x3E,
                0x24, 0x52, 0x2F, 0x9A, 0x69, 0xBC, 0xF2, 0xB2, 0xAC, 0xB8, 0xEF, 0xA1, 0x17, 0x29, 0x2D, 0xEE,
                0xF5, 0x23, 0x21, 0xEC, 0x81, 0xC7, 0x5B, 0xC0, 0x82, 0xCC, 0xD2, 0x91, 0x9D, 0x29, 0x93, 0x0C,
                0x9D, 0x5D, 0x57, 0xAD, 0xD4, 0xC6, 0x40, 0x93, 0x8D, 0xE9, 0xD3, 0x35, 0x9D, 0xC6, 0xD3, 0x00,
            };

            int key = 0x8c1f;
            var encrypted = 0;

            if (data[0] == 0xe2 && data[1] == 0xb0)
                encrypted = 1;

            for (long i = 0; i < data.LongLength; i++)
            {
                var buff = (byte)(Hash_Table_1[key % 0xa7] - Hash_Table_2[key % 0x1ef]);
                ++key;
                if (encrypted == 1)
                    data[i] += buff;
                else
                    data[i] -= buff;
            }

            return data;
        }

        public static string ASCIIIntToString(int skillInfo)
        {
            byte[] name;
            if (skillInfo <= 0xFF)
            {
                name = new byte[1];
                name[0] = (byte)(skillInfo);
            }
            else if (skillInfo <= 0xFFFF)
            {
                name = new byte[2];
                name[0] = (byte)(skillInfo >> 8);
                name[1] = (byte)(skillInfo);
            }
            else if (skillInfo <= 0xFFFFFF)
            {
                name = new byte[3];
                name[0] = (byte)(skillInfo >> 16);
                name[1] = (byte)(skillInfo >> 8);
                name[2] = (byte)(skillInfo);
            }
            else
            {
                name = new byte[4];
                name[0] = (byte)(skillInfo >> 24);
                name[1] = (byte)(skillInfo >> 16);
                name[2] = (byte)(skillInfo >> 8);
                name[3] = (byte)(skillInfo);
            }
            //Yeah but do you understand any of this what a skillinfo ? its the skill id ok i understand so show me here where i find the string
            return System.Text.Encoding.ASCII.GetString(name);
        }

        private void ParseSkillData(String fileName, byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(decryptData(p)));

            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    if (!strArray[3].StartsWith("SKILL")) continue;

                    uint key = Convert.ToUInt32(strArray[1]);
                    if (!Media.Skills.ContainsKey(key))
                    {
                        var skillName = GetNameFromPk2Name("SN_" + strArray[5]);
                        if (skillName == null && !strArray[4].Contains("?"))
                        {
                            skillName = strArray[4];
                        }
                        else if (skillName != null && skillName.StartsWith("SKILL_"))
                        {
                            skillName = skillName.Remove(skillName.Length - 2);
                        }

                        if (skillName != null && skillName.Equals("Ghost Walk - Shadow"))
                        {
                            Console.WriteLine(String.Join(" ; ", strArray));
                        }

                        var skillGroup = Convert.ToUInt16(strArray[59]);
                        //if (skillGroup >= 255) continue; !! NEED THEM, DO NOT EXCLUDE !!
                        var skillGroupIndex = Convert.ToUInt16(strArray[60]);

                        var coolDownId = Convert.ToInt64(strArray[18]);
                        if (coolDownId < 0)
                        {
                            Console.WriteLine("SKIP: {0} / {1}", strArray[1], strArray[3]);
                            continue;
                        }

                        var skillIcon = strArray[61] == "xxx" ? "" : strArray[61];

                        var skillAttributes = new Dictionary<String, int>();
                        var requiredWeapons = new List<ItemInfo.WEAPON_TYPE>();
                        var isAttackSkill = false;

                        {
                            int propIndex = 69;
                            bool effectEnd = false;
                            int skillInfo;

                            //Define missed information
                            //Imbue

                            //if (sd.Name.Contains("_GIGONGTA_"))
                            //    sd.Definedname = s_data.Definedtype.Imbue;

                            #region skill options

                            try
                            {
                                while ((skillInfo = Convert.ToInt32(strArray[propIndex])) != 0 && !effectEnd)
                                {
                                    propIndex++;

                                    string nameString = ASCIIIntToString(skillInfo);

                                    switch (nameString)
                                    {
                                        // get value - only to client
                                        case "getv":
                                        case "MAAT":
                                        // warrior
                                        case "E2AH":
                                        case "E2AA":
                                        case "E1SA":
                                        case "E2SA":
                                        // rogue
                                        case "CBAT":
                                        case "CBRA":
                                        case "DGAT":
                                        case "DGHR":
                                        case "DGAA":
                                        case "STDU":
                                        case "STSP":
                                        case "RPDU":
                                        case "RPTU":
                                        case "RPBU":
                                        // wizard
                                        case "WIMD":
                                        case "WIRU":
                                        case "EAAT":
                                        case "COAT":
                                        case "FIAT":
                                        case "LIAT":
                                        // warlock
                                        case "DTAT":
                                        case "DTDR":
                                        case "BLAT":
                                        case "TRAA":
                                        case "BSHP":
                                        case "SAAA":
                                        // bard
                                        case "MUAT":
                                        case "BDMD":
                                        case "MUER":
                                        case "MUCR":
                                        case "DSER":
                                        case "DSCR":
                                        // cleric
                                        case "HLAT":
                                        case "HLRU":
                                        case "HLMD":
                                        case "HLFS":
                                        case "HLMI":
                                        case "HLBP":
                                        case "HLSM":
                                        // attribute only - no effect
                                        case "nmh": // Healing stone (socket stone)
                                        case "nmf": // Movement stone (socket stone)
                                        case "eshp":
                                        case "reqn":
                                        case "fitp":
                                        case "ao":   // fortress ??
                                        case "rpkt": // fortress repair kit
                                        case "hitm": // Taunt the enemy into attacking only you
                                        case "efta": // bard tambour
                                        case "lks2": // warlock damage buff
                                        case "hntp": // tag point
                                        case "trap": // csapda??
                                        case "cbuf": //itembuff
                                        case "nbuf": // ??(ticketnél volt) nem látszika  buff másnak?
                                        case "bbuf": //debuff
                                        case null:
                                            break;

                                        case "setv":  //set value
                                            string setvType = ASCIIIntToString(Convert.ToInt32(strArray[propIndex]));
                                            propIndex++;

                                            switch (setvType)
                                            {   // warrior
                                                case "E1SA": // phy attack % //done
                                                case "E2SA": // phy attack % //done
                                                case "E2AA":
                                                case "E2AH": // hit rate inc //done
                                                             // rogue
                                                case "CBAT":
                                                case "CBRA":
                                                case "DGAT":
                                                case "DGHR":
                                                case "DGAA":
                                                case "STSP": // speed %
                                                case "STDU": // set stealth duration
                                                case "RPDU": // phy attack %
                                                case "RPBU": // poison duration 
                                                             // wizard
                                                case "WIMD": // wizard mana decrease %
                                                case "WIRU": // Increase the range of magic attacks
                                                case "EAAT": // Magical Attack Power %Increase earth
                                                case "COAT": // Magical Attack Power %Increase cold
                                                case "FIAT": // Magical Attack Power %Increase fire
                                                case "LIAT": // Magical Attack Power %Increase fire
                                                             // warlock
                                                case "DTAT": // Magical Attack Power %Increase
                                                case "DTDR": // Increase the abnormal status duration inflicted by Dark magic
                                                case "BLAT": // Magical Attack Power %Increase Blood Line row 
                                                case "TRAA": // Increases a Warlocks trap damage 
                                                case "BSHP": // HP draining skill attack power increase
                                                             // bard
                                                case "MUAT": // Magical Attack Power % Increase
                                                case "MUER": // Increase the range of harp magic
                                                case "BDMD": // Lowers the MP consumption of skills
                                                case "MUCR": // Resistance Ratio % Increase.
                                                case "DSER": // Increase the range for dance skill.
                                                case "DSCR": // Increase resistance ratio. You don't stop dancing even under attack 
                                                             // cleric
                                                case "HLAT": // Increase the damage of cleric magic. %
                                                case "HLRU": // HP recovery % Inrease
                                                    //sd.Properties1.Add(setvType, Convert.ToInt32(strArray[propIndex]));
                                                    skillAttributes[setvType] = Convert.ToInt32(strArray[propIndex]);
                                                    propIndex++;
                                                    break;

                                                // cleric
                                                case "HLFS": // charity
                                                case "HLMI": // charity
                                                case "HLBP": // charity
                                                case "HLSM": // charity
                                                    //sd.Properties1.Add(setvType, Convert.ToInt32(strArray[propIndex]));
                                                    skillAttributes[setvType] = Convert.ToInt32(strArray[propIndex]);
                                                    propIndex++;

                                                    //sd.Properties2.Add(setvType, Convert.ToInt32(strArray[propIndex]));
                                                    skillAttributes[setvType] = Convert.ToInt32(strArray[propIndex]);
                                                    propIndex++;

                                                    break;
                                            }
                                            break;
                                        // 1 properties
                                        case "tant":
                                        case "rcur": // randomly cure number of bad statuses
                                        case "ck":
                                        case "ovl2":
                                        case "mwhs":
                                        case "scls":
                                        case "mwmh":
                                        case "mwhh":
                                        case "rmut":
                                        case "abnb":
                                        case "mscc":
                                        case "bcnt": // cos bag count [slot]
                                        case "chpi": // cos hp increase [%]
                                        case "chst": // cos speed increase [%]
                                        case "csum": // cos summon [coslevel]
                                        case "jobs": // job skill [charlevel]
                                        case "hwit": // ITEM_ETC_SOCKET_STONE_HWAN ?? duno what is it
                                        case "spi": // Gives additional skill points when hunting monsters. [%inc]
                                        case "dura": // skill duration
                                        case "msid": // mod def ignore prob%
                                        case "hwir": // honor buff new zerk %
                                        case "hst3": // honor buff speed %inc
                                        case "hst2": // rogue haste speed %inc
                                        case "lkdd": // Share % damage taken from a selected person. (link damage)
                                        case "gdr":  // gold drop rate %inc
                                        case "chcr": // target loses % HP
                                        case "cmcr": // target loses % MP
                                        case "dcmp": // MP Cost % Decrease
                                        case "mwdt": // Weapon Magical Attack Power %Reflect
                                        case "pdmg": // Absolute Damage
                                        case "lfst": // life steal Absorb HP
                                        case "puls": // pulsing skill frequenzy
                                        case "pwtt": // Weapon Physical Attack Power reflect.
                                        case "pdm2": // ghost killer
                                        case "luck": // lucky %inc
                                        case "alcu": // alchemy lucky %inc
                                        case "terd": // parry reduce
                                        case "thrd": // Attack ratio reduce
                                        case "ru": // range incrase
                                        case "hste": // speed %inc
                                        case "da": // downattack %inc
                                        case "reqc": // required status?
                                        case "dgmp": // damage mana absorb
                                        case "dcri": // critical parry inc
                                            //sd.Properties1.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        // 2 properties
                                        case "mc":
                                        case "atca":
                                        case "reat":
                                        case "defr":
                                        case "msr": // Triggered at a certain chance, the next spell cast does not cost MP. [%chance to trigger|%mana reduce]
                                        case "kb": // knockback
                                        case "ko":  // knockdown
                                        case "zb": // zombie
                                        case "fz":  // frozen
                                        case "fb":  // frostbite
                                        case "spda": // Shield physical defence % reduce. Physical attack power increase.
                                        case "expi": // sp|exp %inc PET?
                                        case "stri": // str increase
                                        case "inti": // int increase
                                        case "rhru": // Increase the amount of HP healed. %
                                        case "dmgt": // Absorption %? 
                                        case "dtnt": // Aggro Reduce
                                        case "mcap": // monster mask lvl cap
                                        case "apau": // attack power inc [phy|mag]
                                        case "lkag": // Share aggro
                                        case "dttp": // detect stealth [|maxstealvl]
                                        case "tnt2": // taunt inc | aggro %increase
                                        case "expu": // exp|sp %inc
                                        case "msch": // monster transform
                                        case "dtt": // detect invis [ | maxinvilvl]
                                        case "hpi": // hp incrase [inc|%inc]
                                        case "mpi": // mp incrase [inc|%inc]
                                        case "odar": // damage absorbation
                                        case "resu": // resurrection [lvl|expback%]
                                        case "er": // evasion | parry %inc 
                                        case "hr": // hit rating inc | attack rating inc 
                                        case "tele": // light teleport [sec|meter*10]
                                        case "tel2": // wizard teleport [sec|meter*10]
                                        case "tel3": // warrior sprint teleport [sec|m]
                                        case "onff": // mana consume per second
                                        case "br":  // blocking ratio [|%inc]
                                        case "cr":  // critical inc
                                        case "dru": // damage %inc [phy|mag]
                                        case "irgc": // reincarnation [hp|mp]
                                        case "pola": // Preemptive attack prevention [hp|mp]
                                        case "curt": // negative status effect reduce target player
                                            //sd.Properties1.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties2.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        // 3 properties
                                        case "curl": //anti effect: cure [|effect cure amount|effect level]
                                        case "real":
                                        case "skc":
                                        case "bldr": // Reflects damage upon successful block.
                                        case "ca": // confusion
                                        case "rt":  // restraint (wizard) << restraint i guess it should restrain the target or put to ground as in same spot like chains on feet :) 
                                        case "fe": // fear 
                                        case "sl": // dull
                                        case "st": // stun
                                        case "se": // sleep
                                        case "es":  // lightening
                                        case "bu":  // burn
                                        case "ps":  // poison
                                        case "lkdh": // link Damage % MP Absorption (Mana Switch)
                                        case "stns": // Petrified status
                                        case "hide": // stealth hide
                                        case "lkdr": // Share damage
                                        case "defp": // defensepower inc [phy|mag]
                                        case "bgra": // negative status effect reduce
                                        case "cnsm": // consume item
                                            //sd.Properties1.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties2.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties3.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        // 4 properties
                                        case "csit": // division
                                        case "tb": // hidden
                                        case "my": // short sight
                                        case "ds": // disease
                                        case "csmd":  // weaken
                                        case "cspd":  // decay
                                        case "cssr": // impotent
                                        case "dn": // darkness
                                        case "mom": // duration | Berserk mode Attack damage/Defence/Hit rate/Parry rate will increase % | on every X mins
                                        case "pmdp": // maximum physical defence strength decrease %
                                        case "pmhp": // hp reduce
                                        case "dmgr": // damage return [prob%|return%||]
                                        case "lnks": // Connection between players
                                        case "pmdg": // damage reduce [dura|phy%|mag%|?]
                                        case "qest": // some quest related skill?
                                        case "heal": // heal [hp|hp%|mp|mp%]
                                        case "pw": // player wall
                                        case "summ": // summon bird
                                            //sd.Properties1.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties2.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties3.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties4.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        // 5 properties
                                        case "bl": // bleed
                                            //sd.Properties1.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties2.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties3.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties4.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties5.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        // 6 properties
                                        case "cshp": // panic
                                        case "csmp": // combustion
                                            //sd.Properties1.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties2.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties3.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties4.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties5.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            //sd.Properties6.Add(nameString, Convert.ToInt32(strArray[propIndex]));
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        case "reqi": // required item
                                            int weapType1 = Convert.ToInt32(strArray[propIndex]);
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            int weapType2 = Convert.ToInt32(strArray[propIndex]);
                                            skillAttributes[nameString] = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;

                                            if (weapType1 == 4 && weapType2 == 1)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.SHIELD);

                                            else if (weapType1 == 4 && weapType2 == 2)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.EUSHIELD);

                                            else if (weapType1 == 6 && weapType2 == 6)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.BOW);

                                            else if (weapType1 == 6 && weapType2 == 7)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.ONEHAND_SWORD);

                                            else if (weapType1 == 6 && weapType2 == 8)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.TWOHAND_SWORD);

                                            else if (weapType1 == 6 && weapType2 == 9)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.AXE);

                                            else if (weapType1 == 6 && weapType2 == 10)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.WARLOCKROD);

                                            else if (weapType1 == 6 && weapType2 == 11)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.STAFF);

                                            else if (weapType1 == 6 && weapType2 == 12)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.XBOW);

                                            else if (weapType1 == 6 && weapType2 == 13)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.DAGGER);

                                            else if (weapType1 == 6 && weapType2 == 14)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.HARP);

                                            else if (weapType1 == 6 && weapType2 == 15)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.CLERICROD);

                                            else if (weapType1 == 10 && weapType2 == 0)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.LIGHTARMOR);

                                            else if (weapType1 == 14 && weapType2 == 1)
                                                requiredWeapons.Add(ItemInfo.WEAPON_TYPE.DEVILSPIRIT);

                                            break;

                                        case "ssou": // summon monster
                                            //s_data.summon_data summon;
                                            while (Convert.ToInt32(strArray[propIndex]) != 0)
                                            {
                                                //    summon = new s_data.summon_data();
                                                //    summon.ID = Convert.ToInt32(strArray[propIndex]);
                                                propIndex++;
                                                //    summon.Type = Convert.ToByte(strArray[propIndex]);
                                                propIndex++;
                                                //    summon.MinSummon = Convert.ToByte(strArray[propIndex]);
                                                propIndex++;
                                                //    summon.MaxSummon = Convert.ToByte(strArray[propIndex]);
                                                propIndex++;

                                                //    sd.SummonList.Add(summon);
                                            }
                                            break;

                                        case "att": // if attack skill
                                            isAttackSkill = true;
                                            ////sd.Time = Convert.ToInt32(strArray[propIndex]); war schon auskommentiert !
                                            propIndex++;
                                            //sd.MagPer = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            //sd.MinAttack = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            //sd.MaxAttack = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            //sd.PhyPer = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        case "efr":
                                            //sd.efrUnk1 = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            //int type2 = Convert.ToInt32(strArray[propIndex]);
                                            //if (type2 == 6)
                                            //    sd.RadiusType = s_data.RadiusTypes.TRANSFERRANGE;
                                            //else if (type2 == 2)
                                            //    sd.RadiusType = s_data.RadiusTypes.FRONTRANGERADIUS;
                                            //else if (type2 == 7)
                                            //    sd.RadiusType = s_data.RadiusTypes.MULTIPLETARGET;
                                            //else if (type2 == 4)
                                            //    sd.RadiusType = s_data.RadiusTypes.PENETRATION;
                                            //else if (type2 == 3)
                                            //    sd.RadiusType = s_data.RadiusTypes.PENETRATIONRANGED;
                                            //else if (type2 == 1)
                                            //    sd.RadiusType = s_data.RadiusTypes.SURROUNDRANGERADIUS;
                                            propIndex++;

                                            //sd.Distance = Convert.ToInt32(strArray[propIndex]); // in decimeters
                                            propIndex++;
                                            //sd.SimultAttack = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            //int unk2 = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            //int unk3 = Convert.ToInt32(strArray[propIndex]);
                                            propIndex++;
                                            break;

                                        default:
                                            //Console.WriteLine(" {0}  {1}  {2}", propIndex, nameString, sd.Name);
                                            effectEnd = true;
                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            #endregion

                            if (strArray[3] == "SKILL_CH_WATER_REBIRTH_GROUP_A_01")
                            {
                                Console.WriteLine("HERE!!");
                            }

                            //if (!sd.isAttackSkill)
                            //{
                            //    if (sd.Properties1.ContainsKey("heal") || // heal ;)
                            //        sd.Properties1.ContainsKey("curl"))   // bad status removal
                            //    {
                            //        sd.TargetType = s_data.TargetTypes.PLAYER;
                            //        sd.needPVPstate = false;
                            //    }
                            //    if (sd.Properties1.ContainsKey("resu")) // resurrection
                            //    {
                            //        sd.TargetType = s_data.TargetTypes.PLAYER;
                            //        sd.canSelfTargeted = false;
                            //        sd.needPVPstate = false;
                            //    }
                            //    if (sd.Properties1.ContainsKey("terd") ||  // parry reduce
                            //        sd.Properties1.ContainsKey("thrd") ||  // Attack ratio reduce
                            //        sd.Properties1.ContainsKey("cspd") ||  // decay
                            //        sd.Properties1.ContainsKey("csmd") ||  // weaken
                            //        sd.Properties1.ContainsKey("cssr") ||  // impotent
                            //        sd.Properties1.ContainsKey("st") ||  // stun
                            //        sd.Properties1.ContainsKey("bu") ||  // burn
                            //        sd.Properties1.ContainsKey("fb"))      // frostbite

                            //    {
                            //        sd.TargetType = s_data.TargetTypes.PLAYER | s_data.TargetTypes.MOB;
                            //        sd.canSelfTargeted = false;
                            //    }
                            //}
                        }

                        var weaponToUse = ItemInfo.WEAPON_TYPE.UNKNOWN;
                        var weaponToUseNumber = Convert.ToInt32(strArray[50]);

                        var mastery = Convert.ToUInt32(strArray[34]); // required mastery1
                        var reqMastery2 = Convert.ToUInt32(strArray[35]); // required mastery2
                        var reqMastery1Lvl = Convert.ToByte(strArray[36]); // required mastery1 level
                        var reqMastery2Lvl = Convert.ToByte(strArray[37]); // required mastery2 level
                        var reqStr = Convert.ToUInt32(strArray[38]); // required str
                        var reqInt = Convert.ToUInt32(strArray[39]); // required int
                        var reqSkill1 = Convert.ToUInt32(strArray[40]); // required skill1
                        var reqSkill2 = Convert.ToUInt32(strArray[41]); // required skill2
                        var reqSkill3 = Convert.ToUInt32(strArray[42]); // required skill3
                        var reqSkill1Lvl = Convert.ToByte(strArray[43]); // required skill1 level
                        var reqSkill2Lvl = Convert.ToByte(strArray[44]); // required skill2 level
                        var reqSkill3Lvl = Convert.ToByte(strArray[45]); // required skill3 level

                        if (strArray[3].StartsWith("SKILL_EU"))
                        {
                            if (Enum.IsDefined(typeof(ItemInfo.WEAPON_TYPE), weaponToUseNumber))
                            {
                                weaponToUse = (ItemInfo.WEAPON_TYPE)weaponToUseNumber;
                            }
                            else
                            {
                                Console.WriteLine("unknown type: {0} -> {1}/{2}", strArray[50], strArray[3], GetNameFromPk2Name("SN_" + strArray[5]));
                            }
                        }
                        else if (strArray[3].StartsWith("SKILL_CH"))
                        {
                            switch (mastery)
                            {
                                case Mastery.CH_BICHEON:
                                    weaponToUse = ItemInfo.WEAPON_TYPE.SWORD_N_BLADE;
                                    break;

                                case Mastery.CH_HEUKSAL:
                                    weaponToUse = ItemInfo.WEAPON_TYPE.SPEAR_N_GLAVIE;
                                    break;

                                case Mastery.CH_PACHEON:
                                    weaponToUse = ItemInfo.WEAPON_TYPE.BOW;
                                    break;
                            }
                        }
                        else
                        {
                            //Console.WriteLine("?? {0}", strArray[3]);
                        }

                        var weaponType1 = Convert.ToByte(strArray[50]);
                        var weaponType2 = Convert.ToByte(strArray[51]);

                        Media.Skills.Add(key, new Media.Skill(key,
                                                              uint.Parse(strArray[2]),
                                                              strArray[3],
                                                              skillName,
                                                              Convert.ToByte(strArray[36]),
                                                              Convert.ToUInt16(strArray[53]),
                                                              Convert.ToUInt64(strArray[13]),
                                                              Convert.ToUInt64(strArray[14]),
                                                              Convert.ToUInt64(strArray[70]),
                                                              mastery,
                                                              skillGroup,
                                                              skillIcon,
                                                              Convert.ToUInt32(strArray[46]),
                                                              coolDownId,
                                                              isAttackSkill,
                                                              weaponToUse,
                                                              skillAttributes,
                                                              requiredWeapons,
                                                              weaponType1,
                                                              weaponType2,
                                                              reqMastery2,
                                                              reqMastery2Lvl,
                                                              reqStr,
                                                              reqInt,
                                                              reqSkill1,
                                                              reqSkill2,
                                                              reqSkill3,
                                                              reqSkill1Lvl,
                                                              reqSkill2Lvl,
                                                              reqSkill3Lvl,
                                                              skillGroupIndex
                                                              ));
                    }
                }
            }
        }

        private void ParsePortalData(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    uint key = Convert.ToUInt32(strArray[1]);
                    if (!Media.Portals.ContainsKey(key))
                    {
                        Media.Portals.Add(key, new Media.Portal(key, strArray[2], GetNameFromPk2Name(strArray[2]), Convert.ToUInt32(strArray[3])));
                    }
                }
            }
        }

        private void ParsePortalLinks(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    uint key = Convert.ToUInt32(strArray[1]);
                    if (Media.Portals.ContainsKey(key))
                    {
                        var linkList = new List<uint>(Media.Portals[key].Links);
                        uint teleportTo = Convert.ToUInt32(strArray[2]);

                        if (linkList.Contains(teleportTo)) continue;

                        linkList.Add(teleportTo);
                        Media.Portals[key].Links = linkList.ToArray();
                    }
                }
            }
        }

        private void ParseMagicOptionData(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string[] strArray = line.Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    uint key = Convert.ToUInt32(strArray[1]);
                    if (!Media.MagicOptions.ContainsKey(key))
                    {
                        var dg = Convert.ToByte(strArray[4]);

                        if (strArray[2] == "MATTR_STR" || strArray[2] == "MATTR_HP")
                        {
                            var MinValue = 0;
                            var MaxValue = 0;

                            if (Convert.ToInt32(strArray[10]) != 0)
                            {
                                if (ConvertBlueValue(Convert.ToInt32(strArray[10])) != 0)
                                {
                                    MaxValue = ConvertBlueValue(Convert.ToInt32(strArray[10]));
                                }
                                else
                                {
                                    MaxValue = Convert.ToInt32(strArray[10]);
                                }
                            }
                            else
                            {
                                if (ConvertBlueValue(Convert.ToInt32(strArray[9])) != 0)
                                {
                                    MinValue = ConvertBlueValue(Convert.ToInt32(strArray[8]));
                                    MaxValue = ConvertBlueValue(Convert.ToInt32(strArray[9]));
                                }
                            }

                            Console.WriteLine(strArray[2] + " {0:00}: {1}/{2}", dg, MinValue, MaxValue);
                        }

                        //Media.Skills.Add(key, new Media.Skill(key, strArray[3], this.GetNameFromPk2Name("SN_" + strArray[5]), Convert.ToByte(strArray[57]), 0, 0));
                        Media.MagicOptions.Add(key, new Media.MagicOption(key, strArray[2], GetNameFromPk2Name(strArray[2]), dg,
                                strArray.Any(s => s.StartsWith("weapon")),
                                strArray.Any(s => s.StartsWith("shield")),
                                strArray.Any(s => s.StartsWith("armor")),
                                strArray.Any(s => s.StartsWith("accessory")),
                                strArray.Any(s => s.StartsWith("helm")),
                                strArray.Any(s => s.StartsWith("mail")),
                                strArray.Any(s => s.StartsWith("pants")),
                                strArray.Any(s => s.StartsWith("necklace")),
                                strArray.Any(s => s.StartsWith("earring")),
                                strArray.Any(s => s.StartsWith("ring"))
                            ));
                    }
                }
            }
        }

        public static int ConvertBlueValue(int num)
        {
            int number = num >>= 16;
            return number;
        }

        private void ParseTextData(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                string[] strArray = line.Split(new char[] { '\t' });
                try
                {
                    if (strArray.Length > 7)
                    {
                        string key = strArray[1];
                        if (
                            (
                            ((key.Contains("SN_SKILL") || key.Contains("SN_MOB")) || (key.Contains("SN_ITEM") || key.Contains("SN_NPC"))) ||
                            ((key.Contains("SN_STORE") || key.Contains("SN_INS")) || ((key.Contains("SN_COS") || key.Contains("SN_MOB")) || key.Contains("SN_STRUCTURE")))) &&
                            (strArray.Length > 9))
                        {
                            string str3 = strArray[9]; // war idx = 8
                            if ((str3.Length != 0))
                            {
                                this.dTextDataName[key] = str3;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private void ParseShops(SROData.NPCTab[] tabs, byte[] p)
        {
            SROData.NPCs.Current.Clear();

            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    var key = Convert.ToUInt32(strArray[1]);

                    if (strArray[5].StartsWith("-")) continue;

                    var model = Convert.ToUInt32(strArray[5]);
                    if (!SROData.NPCs.Current.Any(n => n.Model == model))
                    {
                        var npc = new SROData.NPC() { Model = model };

                        //var tabIds = strArray.Skip(6).Take(6).Select(ti => Convert.ToUInt32(ti));
                        //var npcTabs = tabs.Where(t => tabIds.Contains(t.Id)).OrderBy(t => t.ShopType);
                        //npc.Tabs = npcTabs.ToArray();
                        //SROData.NPCs.Current.Add(npc);
                    }
                }
            }
        }

        private SROData.NPCTab[] ParseShopTabs(byte[] p)
        {
            var tabs = new List<SROData.NPCTab>();

            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    var key = Convert.ToUInt32(strArray[1]);
                    //var tab = new SROData.NPCTab(key) { ShopType = strArray[2], TabType = strArray[4] };
                    //tabs.Add(tab);
                }
            }

            return tabs.ToArray();
        }

        private void ParseShopGoods(SROData.NPCTab[] tabs, byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    var tabType = strArray[2];

                    var itemType = strArray[3].Replace("PACKAGE_", "");
                    var item = ItemInfos.ItemList.FirstOrDefault(i => i.Type == itemType);
                    if (item == null) continue;

                    var tab = tabs.FirstOrDefault(t => t.ShopType == tabType);
                    if (tab == null) continue;

                    //tab.ItemModels[item.Model] = Convert.ToByte(strArray[4]);
                }
            }
        }

        private void ParseEp(byte[] p)
        {
            ExpPoints.AtLevel.Clear();

            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[] { '\t' });
                if (strArray.Length > 1)
                {
                    try
                    {
                        var lvl = Convert.ToByte(strArray[0]);
                        var ep = Convert.ToUInt64(strArray[1]);
                        var sp = Convert.ToUInt32(strArray[2]);
                        ExpPoints.AtLevel[lvl] = ep;
                        Mastery.SpAtLevel[lvl] = sp;
                    }
                    catch { }
                }
            }
        }

        public void Port(byte[] p)
        {
            StreamReader reader = new StreamReader(new MemoryStream(p));
            while (!reader.EndOfStream)
            {
                string[] strArray = reader.ReadLine().Split(new char[1]);
                //Config.GatewayThread = Convert.ToInt32(strArray[0]);
                //Config.RedirectIPPort = Convert.ToInt32(strArray[0]);
                Console.WriteLine(Convert.ToInt32(strArray[0]));
            }
        }
    }
}

