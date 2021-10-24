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
    public class Shops : ISpawnCollection<Mob>
    {
        private ObservableCollection<Mob> shops = new ObservableCollection<Mob>();
        private object shopsLock = new object();

        public Shops()
        {
            BindingOperations.EnableCollectionSynchronization(shops, shopsLock);
        }

        public Mob GetByName(String name)
        {
            return shops.FirstOrDefault(n => n.Mobinfo.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public Mob GetByType(String type)
        {
            return shops.FirstOrDefault(n => n.Mobinfo.Type.IndexOf(type, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        public Mob Get(uint id)
        {
            return shops.FirstOrDefault(n => n.UID == id);
        }

        public void Remove(Mob obj)
        {
            if (obj == null) return;

            lock (shopsLock)
            {
                if (shops.Contains(obj))
                {
                    shops.Remove(obj);
                }
            }
        }

        public void Remove(uint id)
        {
            var shop = Get(id);
            Remove(shop);
        }

        public void Add(Mob obj)
        {
            if (obj == null) return;

            lock (shopsLock)
            {
                if (!shops.Contains(obj))
                {
                    shops.Add(obj);
                }
            }
        }

        public Mob GetClosest(IEnumerable<MobTypePreference> mobPreferences, Func<Mob, bool> check = null)
        {
            throw new NotImplementedException();
        }

        public void RecalculateDistances(Point pos)
        {
            throw new NotImplementedException();
        }

        public Mob[] GetAll()
        {
            return shops.ToArray();
        }

        public ObservableCollection<Mob> GetList()
        {
            return shops;
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            lock (shopsLock)
                shops.Clear();
        }
    }
}
