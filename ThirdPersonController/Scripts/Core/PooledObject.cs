using UnityEngine;

namespace ThirdPersonController
{
    public class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }
        public ObjectPoolManager Owner { get; private set; }

        public void Initialize(GameObject prefab, ObjectPoolManager owner)
        {
            Prefab = prefab;
            Owner = owner;
        }
    }
}
