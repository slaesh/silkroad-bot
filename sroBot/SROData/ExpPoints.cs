using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROData
{
    class ExpPoints
    {
        public static Dictionary<ushort, ulong> AtLevel = new Dictionary<ushort, ulong>();

        public static void Load()
        {
            AtLevel.Clear();

            var f = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parse_epsp.txt");
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
                        var lvl = Convert.ToByte(splitted[0]);
                        var ep = Convert.ToUInt64(splitted[1]);
                        var sp = Convert.ToUInt32(splitted[2]);
                        Mastery.SpAtLevel[lvl] = sp;
                        AtLevel[lvl] = ep;
                    }
                    catch
                    {
                        //Console.WriteLine(line);
                    }
                }
            }
        }
    }
}
