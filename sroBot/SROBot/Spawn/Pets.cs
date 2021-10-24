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
    public class Pets : ISpawnCollection<Pet>
    {
        private ObservableCollection<Pet> pets = new ObservableCollection<Pet>();
        private object petsLock = new object();

        public Pets()
        {
            BindingOperations.EnableCollectionSynchronization(pets, petsLock);
        }

        public void Remove(Pet pet)
        {
            if (pet == null) return;
            lock (petsLock)
            {
                if (!pets.Contains(pet)) return;
                pets.Remove(pet);
            }
        }

        public void Remove(uint uid)
        {
            var pet = Get(uid);
            if (pet == null)
            {
                return;
            }
            Remove(pet);
        }

        public Pet Get(uint uid)
        {
            lock (petsLock) return pets.FirstOrDefault(m => m.UID == uid);
        }

        public Pet GetClosest(IEnumerable<MobTypePreference> mobPreferences, Func<Pet, bool> check = null)
        {
            return null;
        }

        public Pet[] GetAll()
        {
            return pets.ToArray();
        }

        public void Add(Pet obj)
        {
            lock (petsLock)
            {
                if (!pets.Contains(obj)) pets.Add(obj);
            }
        }

        public void RecalculateDistances(Point pos)
        {
            lock (petsLock)
            {
                foreach (var pet in pets)
                {
                    pet.Distance = Movement.GetDistance(pet.X, pos.X, pet.Y, pos.Y);
                }
            }
        }

        public ObservableCollection<Pet> GetList()
        {
            return pets;
        }

        public void UpdatePosition(uint id, int x, int y)
        {
            var pet = Get(id);
            if (pet == null) return;
            lock (petsLock)
            {
                pet.X = x;
                pet.Y = y;
            }
        }

        public void Clear()
        {
            lock(petsLock)
                pets.Clear();
        }
    }
}
