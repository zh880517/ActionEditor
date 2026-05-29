using System.Collections.Generic;
namespace VECS
{
    public class ViewContext
    {
        protected readonly List<ulong> groups = new List<ulong>();
        private readonly IViewComponentCollector[] collectors;
        private readonly IViewStaticComponent[] staticComponents;
        private readonly List<ViewEntityInternal> entities = new List<ViewEntityInternal>();
        private readonly int componentCount;
        private int unUsedEntityCount = 0;
        private ulong version = 1;
        private bool versionModify;

        public ViewContext(int componentCount, int uniqueCount, int staticComponentCount)
        {
            this.componentCount = componentCount;
            collectors = new IViewComponentCollector[componentCount + uniqueCount];
            staticComponents = new IViewStaticComponent[staticComponentCount];
        }

        public void InitComponentCollector<T>() where T : class, IViewComponent, new()
        {
            if (ViewComponentIdentity<T>.Id == -1)
            {
                throw new System.Exception($"Component类型未初始化 => {typeof(T).FullName}");
            }
            if (collectors[ViewComponentIdentity<T>.Id] != null)
            {
                throw new System.Exception($"ComponentId 重复或重复注册 => {typeof(T).FullName}");
            }
            collectors[ViewComponentIdentity<T>.Id] = new ViewComponentCollector<T>();
        }
        public void InitFlagComponentCollector<T>() where T : class, IViewComponent, new()
        {
            if (ViewComponentIdentity<T>.Id == -1)
            {
                throw new System.Exception($"Component类型未初始化 => {typeof(T).FullName}");
            }
            if (collectors[ViewComponentIdentity<T>.Id] != null)
            {
                throw new System.Exception($"ComponentId 重复或重复注册 => {typeof(T).FullName}");
            }
            collectors[ViewComponentIdentity<T>.Id] = new ViewFlagComponentCollector<T>();
        }

        public void InitUniqueComponentCollector<T>() where T : class, IViewUniqueComponent, new()
        {
            if (ViewComponentIdentity<T>.Id == -1)
            {
                throw new System.Exception($"Component类型未初始化 => {typeof(T).FullName}");
            }
            if (collectors[ViewComponentIdentity<T>.Id] != null)
            {
                throw new System.Exception($"ComponentId 重复或重复注册 => {typeof(T).FullName}");
            }
            collectors[ViewComponentIdentity<T>.Id] = new ViewUniqueComponentCollector<T>();
        }

        public ViewEntity CreateEntity()
        {
            if (unUsedEntityCount > 0)
            {
                for (int i = 0; i < entities.Count; ++i)
                {
                    var entity = entities[i];
                    if (entity.State == ViewEntityInternal.EntityState.None)
                    {
                        entity.State = ViewEntityInternal.EntityState.Active;
                        unUsedEntityCount--;
                        return entity.ToEntity();
                    }
                }
            }
            var newEntity = new ViewEntityInternal
            {
                State = ViewEntityInternal.EntityState.Active,
                Owner = this,
                Index = entities.Count,
                Version = 1,
                ComponentFlag = new System.Collections.BitArray(componentCount),
            };
            entities.Add(newEntity);
            return newEntity.ToEntity();
        }

        public ViewEntity Find(int index, int version)
        {
            var e = entities[index];
            if (e.Version != version || e.State == ViewEntityInternal.EntityState.None)
                return default;

            return e.ToEntity();
        }

        public void SetGameObject(ViewEntity entity, UnityEngine.GameObject obj)
        {
            if (!entity.Valid)
                return;
            entity.entity.Object = obj;
            ModifyAll(entity);
        }

        public void DestroyEntity(ViewEntity entity)
        {
            if (!entity.Valid)
                return;

            for (int i = 0; i < collectors.Length; ++i)
            {
                collectors[i].Remove(entity.entity);
            }
            entity.entity.Clear();
            unUsedEntityCount++;
        }

        internal T AddComponent<T>(ViewEntityInternal entity, bool forceModify) where T : class, IViewComponent, new()
        {
            return collectors[ViewComponentIdentity<T>.Id].Add(entity, GetVersion(), forceModify) as T;
        }

        public T AddStaticComponent<T>() where T : class, IViewStaticComponent, new()
        {
            int id = ViewStaticComponentIdentity<T>.Id;
            var component = staticComponents[id] as T;
            if (component == null)
            {
                component = new T();
                staticComponents[id] = component;
            }
            return component;
        }

        internal T ModifyComponent<T>(ViewEntityInternal entity) where T : class, IViewComponent, new()
        {
            return collectors[ViewComponentIdentity<T>.Id].Modify(entity, GetVersion()) as T;
        }

        public void ModifyAll(ViewEntity entity)
        {
            if (!entity.Valid)
                return;
            foreach(var collector in collectors)
            {
                collector.Modify(entity.entity, GetVersion());
            }
        }

        internal T GetComponent<T>(ViewEntityInternal entity) where T : class, IViewComponent, new()
        {
            return collectors[ViewComponentIdentity<T>.Id].Get(entity) as T;
        }

        public T GetUniqueComponent<T>(out ViewEntity entity) where T : class, IViewUniqueComponent, new()
        {
            if (!ViewComponentIdentity<T>.Unique)
            {
                throw new System.Exception($"{typeof(T).Name} not a UniqueComponent");
            }
            int id = ViewComponentIdentity<T>.Id;
            var collector = collectors[id] as ViewUniqueComponentCollector<T>;
            return collector.TryGet(out entity);
        }

        public T GetStaticComponent<T>() where T : class, IViewStaticComponent, new()
        {
            int id = ViewStaticComponentIdentity<T>.Id;
            return staticComponents[id] as T;
        }

        internal void RemoveComponent<T>(ViewEntityInternal entity) where T : class, IViewComponent, new()
        {
            collectors[ViewComponentIdentity<T>.Id].Remove(entity);
        }

        public void RemoveAll<T>() where T : class, IViewComponent, new()
        {
            collectors[ViewComponentIdentity<T>.Id].RemoveAll();
        }

        public void AddToAll<T>(bool forceModify = false) where T : class, IViewComponent, new()
        {
            for (int i=0; i<entities.Count; ++i)
            {
                var entity = entities[i];
                if (entity.State > ViewEntityInternal.EntityState.None)
                {
                    entity.AddComponent<T>(forceModify);
                }
            }
        }

        public EntityFindResult<T> Find<T>(int startIndex, ulong version, bool includeDisable = false, int groupIndex = -1) where T : class, IViewComponent, new()
        {
            int id = ViewComponentIdentity<T>.Id;
            var collector = collectors[id] as IComponentCollectorT<T>;
            var result = collector.Find(startIndex, version, includeDisable);
            if (groupIndex >= 0)
            {
                if (groups[groupIndex] < result.Version)
                    groups[groupIndex] = result.Version;
            }
            return result;
        }

        public EntityFindResult<T> MatchFind<T, TMatcher>(TMatcher matcher, int startIndex, ulong version, bool includeDisable = false, int groupIndex = -1) where T : class, IViewComponent, new()
            where TMatcher : IViewComponentMatcher<T>
        {
            int id = ViewComponentIdentity<T>.Id;
            var collector = collectors[id] as IComponentCollectorT<T>;
            var result = collector.MatchFind(startIndex, version, includeDisable, matcher);
            if (groupIndex >= 0)
            {
                if (groups[groupIndex] < result.Version)
                    groups[groupIndex] = result.Version;
            }
            return result;
        }

        public Group<TComponent> CreatGroup<TComponent>(bool includeDisable = false) where TComponent : class, IViewComponent, new()
        {
            return new Group<TComponent>(this, includeDisable);
        }

        public MatchGroup<TComponent, TMatcher> CreatMatchGroup<TComponent, TMatcher>(TMatcher matcher, bool includeDisable = false) where TComponent : class, IViewComponent, new() where TMatcher : IViewComponentMatcher<TComponent>
        {
            return new MatchGroup<TComponent, TMatcher>(this, matcher, includeDisable);
        }

        public int RegisterReactiveGroup<T>() where T : class, IViewComponent, new()
        {
            groups.Add(0);
            return groups.Count - 1;
        }
        public ReactiveGroup<T> GetReactiveGroup<T>(int index, bool includeDisable = false) where T : class, IViewComponent, new()
        {
            ulong version = groups[index];
            versionModify = true;
            return new ReactiveGroup<T>(index, version, includeDisable, this);
        }
        private ulong GetVersion()
        {
            if (versionModify)
            {
                ++version;
                versionModify = false;
            }
            return version;
        }
    }
}