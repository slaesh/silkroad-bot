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
    public class Mobs : ISpawnCollection<Mob>
    {
        private ObservableCollection<Mob> mobs = new ObservableCollection<Mob>();
        private object mobsLock = new object();

        public Mobs()
        {
            BindingOperations.EnableCollectionSynchronization(mobs, mobsLock);
        }

        public void Remove(Mob mob)
        {
            if (mob == null) return;
            lock (mobsLock)
            {
                if (!mobs.Contains(mob)) return;
                mobs.Remove(mob);
            }
        }

        public void Remove(uint uid)
        {
            var mob = Get(uid);
            if (mob == null)
            {
                return;
            }
            Remove(mob);
        }

        public Mob Get(uint uid)
        {
            lock (mobsLock) return mobs.FirstOrDefault(m => m.UID == uid);
        }

        public Mob GetClosest(IEnumerable<MobTypePreference> mobPreferences = null, Func<Mob, bool> check = null)
        {
            lock (mobsLock)
            {
                try
                {
                    Mob ret = null;

                    foreach (var mob in mobs.Where(m => m.Ignore > 0))
                    {
                        mob.Ignore--;
                    }

                    var possibleMobs = mobs.Where(m => m.Ignore <= 0 && !m.IsPartyMemberAttacking()).ToArray();
                    possibleMobs = MobTypePreference.Order(possibleMobs, mobPreferences).ToArray();

                    // HACK !!

                    //return possibleMobs.Where(m => check == null || check(m)).OrderBy(m => m.Distance).FirstOrDefault();

                    // HACK !!

                    var attackingMe = possibleMobs.Where(m => m.IsAttackingMe && (check == null || check(m))).OrderBy(m => m.GetScore(mobPreferences));
                    if (attackingMe.Any())
                    {
                        var len = attackingMe.Count() - 1;
                        if (len > 2) len = 2;
                        var idx = Global.Random.Next(0, len);

                        idx = 0; // hack ?! ka was besser ist..
                        return attackingMe.ElementAt(idx);
                    }

                    var orderedMobs = possibleMobs.Where(m => check == null || check(m)).OrderBy(m => m.GetScore(mobPreferences));
                    if (orderedMobs.Any())
                    {
                        var len = orderedMobs.Count() - 1;
                        if (len > 2) len = 2;
                        var idx = Global.Random.Next(0, len);

                        idx = 0; // hack ?! ka was besser ist..
                        return orderedMobs.ElementAt(idx);
                    }
                    
                    return ret;
                }
                catch { return null; }
            }
        }

        public Mob[] GetAll()
        {
            return mobs.ToArray();
        }

        public void Add(Mob obj)
        {
            lock (mobsLock)
            {
                if (!mobs.Contains(obj)) mobs.Add(obj);
            }
        }

        public void RecalculateDistances(Point pos)
        {
            lock (mobsLock)
            {
                foreach (var mob in mobs)
                {
                    mob.Distance = Movement.GetDistance(mob.X, pos.X, mob.Y, pos.Y);
                }
            }
        }

        public ObservableCollection<Mob> GetList()
        {
            return mobs;
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            var mob = Get(id);
            if (mob == null) return;
            lock (mobsLock)
            {
                mob.X = x;
                mob.Y = y;
            }
        }

        public void Clear()
        {
            lock(mobsLock)
                mobs.Clear();
        }
    }
}
