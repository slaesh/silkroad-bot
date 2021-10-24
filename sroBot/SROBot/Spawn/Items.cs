using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sroBot;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Drawing;

namespace sroBot.SROBot.Spawn
{
    public class Items : ISpawnCollection<Item>
    {
        private ObservableCollection<Item> items = new ObservableCollection<Item>();
        private object itemsLock = new object();

        public Items()
        {
            BindingOperations.EnableCollectionSynchronization(items, itemsLock);
        }

        public void Add(Item item)
        {
            if (item == null) return;
            lock (itemsLock)
            {
                if (items.Contains(item)) return;
                items.Add(item);
            }
        }

        public void Remove(Item item)
        {
            if (item == null) return;
            lock (itemsLock)
            {
                if (!items.Contains(item)) return;
                items.Remove(item);
            }
        }

        public void Remove(uint uid)
        {
            var item = Get(uid);
            if (item == null) return;
            Remove(item);
        }

        public Item Get(uint uid)
        {
            lock (itemsLock) return items.FirstOrDefault(i => i.UID == uid);
        }

        public Item GetClosest(IEnumerable<MobTypePreference> mobPreferences, Func<Item, bool> check = null)
        {
            lock (itemsLock)
            {
                try
                {
                    Item ret = null;
                    foreach (var item in items.Where(i => i.Pickable))
                    {
                        if (check != null && !check(item)) continue;
                        if (ret == null || ret.Distance > item.Distance) ret = item;
                    }
                    return ret;
                }
                catch { return null; }
            }
        }

        public void RecalculateDistances(Point pos)
        {
            lock (itemsLock)
            {
                foreach (var item in items)
                {
                    item.Distance = Movement.GetDistance(item.X, pos.X, item.Y, pos.Y);
                }
            }
        }

        public Item[] GetAll()
        {
            return items.ToArray();
        }

        public ObservableCollection<Item> GetList()
        {
            return items;
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            var item = Get(id);
            if (item == null) return;
            lock (itemsLock)
            {
                item.X = x;
                item.Y = y;
            }
        }

        public void Clear()
        {
            lock (itemsLock)
                items.Clear();
        }
    }
}
