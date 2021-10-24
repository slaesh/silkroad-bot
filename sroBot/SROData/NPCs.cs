using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROData
{
    class NpcItem
    {
        public uint Model;
        public byte IndexOfTab;
        public byte Plus;
        public UInt64 Price;
    }

    class NPCTab
    {
        public String ShopType;
        public uint unknown;
        public String TabType;
        public List<NpcItem> ItemModels;

        public NPCTab()
        {
            ShopType = "";
            TabType = "";
            ItemModels = new List<NpcItem>();
        }
    }

    class NPC
    {
        public uint Model;
        public string Type;
        public NPCTab[] Tabs = new NPCTab[0];
    }

    class NPCs : List<NPC>
    {
        private static NPCs instance;
        public static NPCs Current
        {
            get
            {
                return instance ?? (instance = new NPCs());
            }
        }

        public static void Load()
        {
            NPCs.Current.Clear();
            
            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_goods.txt");
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
                        var npcModel = uint.Parse(splitted[0]);
                        var npc = GetByModel(npcModel);
                        if (npc == null)
                        {
                            npc = new NPC() { Model = npcModel, Type = MobInfos.GetById(npcModel)?.Type ?? "UNKNOWN" };
                            Current.Add(npc);
                        }
                        
                        var tab = new NPCTab();
                        tab.TabType = splitted[1];

                        try
                        {
                            byte itemidx = 0;
                            foreach (var itemdata in splitted.Skip(2).ToArray())
                            {
                                try
                                {
                                    var itemModel = Convert.ToUInt32(itemdata.Split('+')[0]);
                                    var itemPlus = Convert.ToByte(itemdata.Split('+')[1].Split(';')[0]);
                                    var itemPrice = UInt64.Parse(itemdata.Split('+')[1].Split(';')[1]);

                                    tab.ItemModels.Add(new NpcItem()
                                    {
                                        Model = itemModel,
                                        IndexOfTab = itemidx++,
                                        Plus = itemPlus,
                                        Price = itemPrice
                                    });
                                }
                                catch { }
                            }
                        }
                        catch { }

                        var tabs = new List<NPCTab>(npc.Tabs);
                        tabs.Add(tab);
                        npc.Tabs = tabs.ToArray();
                    }
                    catch
                    {
                        //Console.WriteLine(line);
                    }
                }
            }

            //foreach (var armorNpc in Current.Where(n => n.Type.EndsWith("_ARMOR")))
            //{
            //    var tabs = armorNpc.Tabs.ToArray();
            //    var newTabs = new List<NPCTab>();
                
            //    foreach (var curTab in tabs)
            //    {
            //        newTabs.Add(curTab); // MALE !

            //        // .. GENERATE FEMALE

            //        newTabs.Add(new NPCTab(curTab.Id + 10000 /* dont care.. */)
            //        {
            //            TabType = curTab.TabType,
            //            ShopType = curTab.ShopType,
            //            unknown = curTab.unknown,
            //            ItemModels = curTab.ItemModels.ToDictionary(_ => ItemInfos.GetByType(ItemInfos.GetById(_.Key)?.Type.Replace("_M_", "_W_"))?.Model ?? 0, _ => _.Value)
            //        });
            //    }

            //    armorNpc.Tabs = newTabs.ToArray();
            //}
        }

        public static NPC GetByModel(uint model)
        {
            return Current.FirstOrDefault(n => n.Model == model);
        }
    }
}
