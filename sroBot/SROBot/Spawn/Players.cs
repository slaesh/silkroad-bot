using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace sroBot.SROBot.Spawn
{
    public class Players : ISpawnCollection<Player>
    {
        private ObservableCollection<Player> players = new ObservableCollection<Player>();
        private object playersLock = new object();

        public Players()
        {
            BindingOperations.EnableCollectionSynchronization(players, playersLock);
        }

        public void Add(Player obj)
        {
            if (players.Contains(obj)) return;

            lock (playersLock)
                players.Add(obj);
        }

        public void Clear()
        {
            lock(playersLock)
                players.Clear();
        }

        public Player Get(uint id)
        {
            return players.FirstOrDefault(p => p.UID == id);
        }

        public Player Get(String name)
        {
            return players.FirstOrDefault(p => p.Name == name);
        }

        public Player[] GetAll()
        {
            return players.ToArray();
        }

        public Player GetClosest(IEnumerable<MobTypePreference> mobPreferences, Func<Player, bool> check = null)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<Player> GetList()
        {
            return players;
        }

        public void RecalculateDistances(Point pos)
        {
            lock (playersLock)
            {
                foreach (var player in players)
                {
                    player.Distance = Movement.GetDistance(player.X, pos.X, player.Y, pos.Y);
                }
            }
        }

        public void Remove(uint id)
        {
            var player = Get(id);
            if (player == null) return;
            Remove(player);
        }

        public void Remove(Player obj)
        {
            if (!players.Contains(obj)) return;
            lock (playersLock)
                players.Remove(obj);
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            var player = Get(id);
            if (player == null) return;
            lock (playersLock)
            {
                player.X = x;
                player.Y = y;
            }
        }
    }
}
