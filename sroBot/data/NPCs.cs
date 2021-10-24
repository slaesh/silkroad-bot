using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.data
{
    class NPCTab
    {
        public uint Id;
        public String Type;
        public uint unknown;
        public String TabType;
        public uint[] ItemModels;

        public NPCTab(uint id)
        {
            Id = id;
            Type = "";
            TabType = "";
            ItemModels= new uint[0];
        }
    }

    class NPC
    {
        public uint Model;
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
                            npc = new NPC() { Model = npcModel };
                            Current.Add(npc);
                        }

                        var tabId = Convert.ToUInt32(splitted[1]);
                        if (npc.Tabs.Any(t => t.Id == tabId)) continue;

                        var tab = new NPCTab(tabId);
                        tab.TabType = splitted[2];
                        try
                        {
                            tab.ItemModels = splitted.Skip(3).Select(i => Convert.ToUInt32(i)).ToArray();

                        }
                        catch { Console.WriteLine("could not get goods from npc: {0}?!", npc.Model); }

                        var tabs = new List<NPCTab>(npc.Tabs);
                        tabs.Add(tab);
                        npc.Tabs = tabs.ToArray();
                    }
                    catch (Exception ex) { Console.WriteLine("err parsing NPC: {0} => {1}: {2}", line, ex.Message, ex.StackTrace); }
                }
            }
        }

        public static NPC GetByModel(uint model)
        {
            return Current.FirstOrDefault(n => n.Model == model);
        }
    }
}
