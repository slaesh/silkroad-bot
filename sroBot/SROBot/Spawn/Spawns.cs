using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot.Spawn
{
    public class Spawns
    {
        public Mobs Mobs = new Mobs();
        public Pets Pets = new Pets();
        public Items Items = new Items();
        public Shops Shops = new Shops();
        public Players Player = new Players();
        public Gates Gates = new Gates();

        private Bot bot;

        public Spawns(Bot bot)
        {
            this.bot = bot;
        }

        public void SimulateSpawnsToClient()
        {
            // simulate all spawns to the client !
        }

        public void RecalculateDistances(Point charPos)
        {
            Mobs.RecalculateDistances(charPos);
            Items.RecalculateDistances(charPos);
            Pets.RecalculateDistances(charPos);
            Player.RecalculateDistances(charPos);
        }

        public void Remove (uint id)
        {
            Mobs.Remove(id);
            Items.Remove(id);
            Shops.Remove(id);
            Pets.Remove(id);
            Player.Remove(id);
            Gates.Remove(id);
        }

        public void UpdatePosition(uint id, Point pos)
        {
            UpdatePosition(id, pos.X, pos.Y);
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            Mobs.UpdatePosition(id, x, y);
            Pets.UpdatePosition(id, x, y);
            Player.UpdatePosition(id, x, y);
        }

        public void Clear()
        {
            Mobs.Clear();
            Pets.Clear();
            Items.Clear();
            Shops.Clear();
            Player.Clear();
            Gates.Clear();
        }
    }
}
