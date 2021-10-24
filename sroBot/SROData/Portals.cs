using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROData
{
    public class Portal
    {
        public uint Id;
        public String Type;
        public String Name;
        public uint Model;
        public uint[] Links = new uint[0];
        public uint IngameId;

        public Portal(uint id, String type, String name, uint model)
        {
            Id = id;
            Type = type;
            Name = name;
            Model = model;
        }
    }

    class Portals : List<Portal>
    {
        private static Portals instance;
        public static Portals Current
        {
            get
            {
                return instance ?? (instance = new Portals());
            }
        }

        public static void Load()
        {
            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_portals.txt");
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
                        var portal = new Portal(uint.Parse(splitted[0]), splitted[1], splitted[2], uint.Parse(splitted[3]));
                        Current.Add(portal);
                        
                        portal.Links = splitted.Skip(4).Select(teleportTo =>
                            {
                                uint dummy = 0;
                                uint.TryParse(teleportTo, out dummy);
                                return dummy;
                            }).ToArray();
                    }
                    catch
                    {
                        //Console.WriteLine(line);
                    }
                }
            }
        }

        public static Portal GetById(uint id)
        {
            return Current.FirstOrDefault(p => p.Id == id);
        }

        public static Portal GetByModel(uint model)
        {
            return Current.FirstOrDefault(p => p.Model == model);
        }
    }
}
