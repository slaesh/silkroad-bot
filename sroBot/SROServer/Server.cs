using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sroBot.ConfigHandler;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Controls;

namespace sroBot.SROServer
{
    public partial class Server
    {
        private static String[] DirectoriesToCreate = new String[] { "bots", "pk2" };

        public static ObservableCollection<Server> Available = new ObservableCollection<Server>();

        public String Ip { get; set; } = "";
        public uint Port { get; set; } = 0;
        public uint LocaleVersion { get; set; } = 0;
        public uint ClientVersion { get; set; } = 0;
        public String Captcha { get; set; } = "";

        public String Name { get; set; } = "";
        public ObservableCollection<SROBot.Bot> Bots { get; set; } = new ObservableCollection<SROBot.Bot>();
        
        private Server() { }

        public Server(String ip, uint port, uint locale, uint client)
        {
            Ip = ip;
            Port = port;
            LocaleVersion = locale;
            ClientVersion = client;
        }

        private static Server load(String serverDir)
        {
            return new JsonConfiguration<Server>(Path.Combine(serverDir, "server.json")).Load();
        }

        private void loadBots()
        {
            var serverDir = Path.Combine(App.ExecutingPath, "server", Name, "bots");
            foreach (var botname in Directory.EnumerateDirectories(serverDir).Select(bot => Path.GetFileName(bot)))
            {
                var bot = SROBot.Bot.Load(this, botname);
                if (bot == null) continue;

                Bots.Add(bot);
            }
        }

        public static bool Load()
        {
            var serversDir = Path.Combine(App.ExecutingPath, "server");
            if (!Directory.Exists(serversDir))
            {
                Directory.CreateDirectory(serversDir);
            }

            Available.Clear();

            foreach (var serverDir in Directory.EnumerateDirectories(serversDir))
            {
                var server = load(serverDir);
                if (server == null) continue;
                
                server.Name = Path.GetFileName(serverDir);
                server.loadBots();
                Available.Add(server);
            }

            return true;
        }

        public static bool Create(String name, Server server, bool overWrite = false)
        {
            if (String.IsNullOrEmpty(name) || server == null) return false;

            var serverDir = Path.Combine(App.ExecutingPath, "server", name);
            if (!overWrite && Directory.Exists(serverDir)) return false;

            var dir = Directory.CreateDirectory(serverDir);
            if (!dir.Exists) return false;

            Array.ForEach(DirectoriesToCreate, (d) => { Directory.CreateDirectory(Path.Combine(serverDir, d)); });

            new JsonConfiguration<Server>(Path.Combine(serverDir, "server.json")).Save(server);

            server.Name = Path.GetFileName(serverDir);
            Available.Add(server);

            return true;
        }

        public SROBot.Bot[] GetBots()
        {
            return Bots.ToArray();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
