using System.Collections.Generic;
namespace ECSLite
{
    public class Context
    {
        private IComponentCollector[] collectors;
        private List<EntityInternal> entities = new List<EntityInternal>();
        private int componentCount;
        private int unUsedEntityCount = 0;

        public Context(int componentCount, int uniqueCount)
        {
            this.componentCount = componentCount;
            collectors = new IComponentCollector[componentCount + uniqueCount];
        }

        public void InitComponentCollector<T>() where T : class, IComponent, new()
        {
            if (ComponentIdentity<T>.Id == -1)
            {
                throw new System.Exception($"Component类型未初始化 => {typeof(T).FullName}");
            }
            if (collectors[ComponentIdentity<T>.Id] != null)
            {
                throw new System.Exception($"ComponentId 重复或重复注册 => {typeof(T).FullName}");
            }
            collectors[ComponentIdentity<T>.Id] = new ComponentCollector<T>();
        }

        public void InitFlagComponentCollector<T>() where T : class, IComponent, new()
        {
            if (ComponentIdentity<T>.Id == -1)
            {
                throw new System.Exception($"Component类型未初始化 => {typeof(T).FullName}");
            }
            if (collectors[ComponentIdentity<T>.Id] != null)
            {
                throw new System.Exception($"ComponentId 重复或重复注册 => {typeof(T).FullName}");
            }
            collectors[ComponentIdentity<T>.Id] = new FlagComponentCollector<T>();
        }

        public void InitUniqueComponentCollector<T>() where T : class, IUniqueComponent, new()
        {
            if (ComponentIdentity<T>.Id == -1)
            {
                throw new System.Exception($"Component类型未初始化 => {typeof(T).FullName}");
            }
            if (collectors[ComponentIdentity<T>.Id] != null)
            {
                throw new System.Exception($"ComponentId 重复或重复注册 => {typeof(T).FullName}");
            }
            collectors[ComponentIdentity<T>.Id] = new UniqueComponentCollector<T>();
        }

        internal EntityInternal CreateEntity()
        {
            if (unUsedEntityCount > 0)
            {
                for (int i=0; i<entities.Count; ++i)
                {
                    var entity = entities[i];
                    if (!entity.Used)
                    {
                        entity.Used = true;
                        unUsedEntityCount--;
                        return entity;
                    }
                }
            }
            var newEntity = new EntityInternal 
            {
                ID = new EntityIdentify { Index = entities.Count, Version = 1 },
                Used = true,
                Owner = this,
                ComponentFlag = new System.Collections.BitArray(componentCount),
            };
            entities.Add(newEntity);
            return newEntity;
        }

        internal EntityInternal FindEntity(EntityIdentify id)
        {
            var entity = entities[id.Index];
            if (entity.ID.Version == id.Version)
            {
                return entity;
            }
            return null;
        }

        internal EntityInternal Get(int index)
        {
            return entities[index];
        }

        public void DestroyEntity(EntityIdentify entityID)
        {
            var entity = entities[entityID.Index];
            if (entity.ID.Version == entityID.Version)
            {
                for (int i = 0; i < collectors.Length; ++i)
                {
                    collectors[i].Remove(entityID.Index);
                }
                entity.Clear();
                unUsedEntityCount++;
            }
        }

        internal T AddComponent<T>(int id) where T : class, IComponent, new()
        {
            return collectors[ComponentIdentity<T>.Id].Add(id) as T;
        }

        internal T GetComponent<T>(int id) where T : class, IComponent, new()
        {
            return collectors[ComponentIdentity<T>.Id].Get(id) as T;
        }

        internal void RemoveComponent<T>(int id) where T : class, IComponent, new()
        {
            collectors[ComponentIdentity<T>.Id].Remove(id);
        }

        internal T GetUniqueComponent<T>(out EntityInternal entity) where T : class, IUniqueComponent, new()
        {
            if (!ComponentIdentity<T>.Unique)
            {
                throw new System.Exception($"{typeof(T).Name} not a UniqueComponent");
            }
            int id = ComponentIdentity<T>.Id;
            var collector = collectors[id] as UniqueComponentCollector<T>;

            var component = collector.TryGet(out int entityId);
            if (entityId < 0)
            {
                entity = null;
                return null;
            }
            entity = entities[entityId];
            return component;
        }

        internal void AddToAll<T>() where T : class, IComponent, new()
        {
            int componentId = ComponentIdentity<T>.Id;
            var collector = collectors[componentId];
            for (int i=0; i<entities.Count; ++i)
            {
                var entity = entities[i];
                var component = collector.Add(i) as T;
                if (component != null && !ComponentIdentity<T>.Unique)
                {
                    entity.ComponentFlag[componentId] = true;
                }
            }
        }

        internal ComponentFindResult<T> FindComponet<T>(int startIndex) where T : class, IComponent, new()
        {
            int id = ComponentIdentity<T>.Id;
            var collector = collectors[id] as IComponentCollectorT<T>;
            return collector.Find(startIndex);
        }

        internal ComponentFindResult<T> MatchFindComponet<T, TMatcher>(int startIndex, TMatcher matcher) where T : class, IComponent, new() where TMatcher : IComponentMatcher<T>
        {
            int id = ComponentIdentity<T>.Id;
            var collector = collectors[id] as IComponentCollectorT<T>;
            return collector.MatchFind(startIndex, matcher);
        }

        internal void RemoveAll<T>() where T : class, IComponent, new()
        {
            collectors[ComponentIdentity<T>.Id].RemoveAll();
        }
    }
}
