using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ThirdPersonController
{
    public class EnemyEntityPool : MonoBehaviour
    {
        [Header("Prefab")]
        public EntityPrefabReference enemyPrefab;

        [Header("Pool")]
        public int prewarmCount = 100;
        public int maxCount = 200;
        public bool allowExpand = true;

        private readonly List<Entity> pooled = new List<Entity>();
        private EntityManager entityManager;
        private Entity prefabEntity = Entity.Null;
        private Entity requestEntity = Entity.Null;
        private bool prewarmed;

        private void Awake()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            RequestPrefabLoad();
        }

        private void Update()
        {
            if (entityManager == null)
            {
                return;
            }

            if (prefabEntity == Entity.Null)
            {
                TryResolvePrefab();
            }

            if (!prewarmed && prefabEntity != Entity.Null)
            {
                Prewarm();
                prewarmed = true;
            }
        }

        public void Prewarm()
        {
            if (entityManager == null || prefabEntity == Entity.Null)
            {
                return;
            }

            int target = Mathf.Clamp(prewarmCount, 0, maxCount);
            for (int i = pooled.Count; i < target; i++)
            {
                Entity entity = entityManager.Instantiate(prefabEntity);
                entityManager.AddComponent<Disabled>(entity);
                pooled.Add(entity);
            }
        }

        public Entity Spawn(float3 position)
        {
            if (entityManager == null)
            {
                return Entity.Null;
            }

            Entity entity = GetPooled();
            if (entity == Entity.Null)
            {
                return Entity.Null;
            }

            if (entityManager.HasComponent<Disabled>(entity))
            {
                entityManager.RemoveComponent<Disabled>(entity);
            }

            if (entityManager.HasComponent<LocalTransform>(entity))
            {
                LocalTransform transform = entityManager.GetComponentData<LocalTransform>(entity);
                transform.Position = position;
                entityManager.SetComponentData(entity, transform);
            }

            return entity;
        }

        public void Despawn(Entity entity)
        {
            if (entityManager == null || entity == Entity.Null || !entityManager.Exists(entity))
            {
                return;
            }

            if (!entityManager.HasComponent<Disabled>(entity))
            {
                entityManager.AddComponent<Disabled>(entity);
            }

            if (!pooled.Contains(entity))
            {
                pooled.Add(entity);
            }
        }

        private Entity GetPooled()
        {
            if (pooled.Count > 0)
            {
                int lastIndex = pooled.Count - 1;
                Entity entity = pooled[lastIndex];
                pooled.RemoveAt(lastIndex);
                return entity;
            }

            if (!allowExpand || pooled.Count >= maxCount || enemyPrefab.Equals(default(EntityPrefabReference)))
            {
                return Entity.Null;
            }

            if (prefabEntity == Entity.Null)
            {
                TryResolvePrefab();
                if (prefabEntity == Entity.Null)
                {
                    return Entity.Null;
                }
            }

            Entity newEntity = entityManager.Instantiate(prefabEntity);
            return newEntity;
        }

        private void RequestPrefabLoad()
        {
            if (entityManager == null || enemyPrefab.Equals(default(EntityPrefabReference)))
            {
                return;
            }

            if (requestEntity == Entity.Null || !entityManager.Exists(requestEntity))
            {
                requestEntity = entityManager.CreateEntity(typeof(RequestEntityPrefabLoaded));
                entityManager.SetComponentData(requestEntity, new RequestEntityPrefabLoaded
                {
                    Prefab = enemyPrefab
                });
            }
        }

        private void TryResolvePrefab()
        {
            if (requestEntity == Entity.Null || !entityManager.Exists(requestEntity))
            {
                RequestPrefabLoad();
                return;
            }

            if (!entityManager.HasComponent<PrefabLoadResult>(requestEntity))
            {
                return;
            }

            PrefabLoadResult result = entityManager.GetComponentData<PrefabLoadResult>(requestEntity);
            if (result.PrefabRoot != Entity.Null)
            {
                prefabEntity = result.PrefabRoot;
            }

            if (entityManager.Exists(requestEntity))
            {
                entityManager.DestroyEntity(requestEntity);
            }
            requestEntity = Entity.Null;
        }
    }
}
