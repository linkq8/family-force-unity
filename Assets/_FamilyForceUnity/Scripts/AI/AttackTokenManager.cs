using System.Collections.Generic;
using UnityEngine;

namespace FamilyForceUnity.AI
{
    public sealed class AttackTokenManager : MonoBehaviour
    {
        [Min(1), SerializeField] private int capacity = 2;
        private readonly HashSet<int> owners = new();

        public int Capacity => capacity;
        public int ActiveCount => owners.Count;

        public bool TryAcquire(Object owner)
        {
            if (owner == null)
                return false;

            int id = owner.GetInstanceID();
            if (owners.Contains(id))
                return true;
            if (owners.Count >= capacity)
                return false;

            owners.Add(id);
            return true;
        }

        public void Release(Object owner)
        {
            if (owner != null)
                owners.Remove(owner.GetInstanceID());
        }

        public void ConfigureCapacity(int value) => capacity = Mathf.Max(1, value);
    }
}

