using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot.Spawn
{
    interface ISpawnCollection<T>
    {
        T Get(uint id);
        void Remove(T obj);
        void Remove(uint id);
        void Add(T obj);
        T GetClosest(IEnumerable<MobTypePreference> mobPrefenreces, Func<T, bool> check = null);
        void RecalculateDistances(Point pos);
        T[] GetAll();
        ObservableCollection<T> GetList();
        void UpdatePosition(uint id, int x, int y);
        void Clear();
    }
}
