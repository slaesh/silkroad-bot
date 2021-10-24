using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot.Spawn
{
    public class Gates : ISpawnCollection<SROData.Portal>
    {
        private ObservableCollection<SROData.Portal> gates = new ObservableCollection<SROData.Portal>();
        private object gatesLock = new object();

        public void Add(SROData.Portal obj)
        {
            if (gates.Contains(obj)) return;
            lock (gatesLock)
            {
                gates.Add(obj);
            }
        }

        public void Clear()
        {
            lock (gatesLock)
                gates.Clear();
        }

        public SROData.Portal Get(uint id)
        {
            return gates.FirstOrDefault(g => g.Id == id);
        }

        public SROData.Portal[] GetAll()
        {
            return gates.ToArray();
        }

        public SROData.Portal GetClosest(IEnumerable<MobTypePreference> mobPreferences, Func<SROData.Portal, bool> check = null)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<SROData.Portal> GetList()
        {
            return gates;
        }

        public void RecalculateDistances(Point pos)
        {
            throw new NotImplementedException();
        }

        public void Remove(uint id)
        {
            var gate = Get(id);
            if (gate == null) return;
            Remove(gate);
        }

        public void Remove(SROData.Portal obj)
        {
            if (!gates.Contains(obj)) return;
            lock (gatesLock)
            {
                gates.Remove(obj);
            }
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            throw new NotImplementedException();
        }
    }
}
