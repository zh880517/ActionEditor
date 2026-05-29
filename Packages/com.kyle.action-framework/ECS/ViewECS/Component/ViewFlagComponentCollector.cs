using System.Collections.Generic;
namespace VECS
{
    //减少Flag类型Component多余的New
    internal class ViewFlagComponentCollector<T> : IComponentCollectorT<T> where T : class, IViewComponent, new()
    {
        private ulong mVersion;
        private readonly List<FlagComponentEntity<T>> mUnits = new List<FlagComponentEntity<T>>();
        private readonly Queue<int> mUnUsedIdxs = new Queue<int>();
        private readonly Dictionary<int, int> mIdIdxMap = new Dictionary<int, int>();//EntityId => 数组索引
        private T Component = new T();
        public int Count => mIdIdxMap.Count;

        private FlagComponentEntity<T> Create()
        {
            if (mUnUsedIdxs.Count > 0)
            {
                var index = mUnUsedIdxs.Dequeue();
                return mUnits[index];
            }
            var unit = new FlagComponentEntity<T>();
            unit.Index = mUnits.Count;
            mUnits.Add(unit);
            return unit;
        }
        public IViewComponent Add(ViewEntityInternal entity, ulong version, bool forceModify)
        {
            if (mIdIdxMap.TryGetValue(entity.Index, out int idx))
            {
                var exist = mUnits[idx];
                if (forceModify)
                {
                    exist.Version = version;
                    mVersion = version;
                }
                return Component;
            }
            var unit = Create();
            mIdIdxMap.Add(entity.Index, unit.Index);
            unit.Owner = entity;
            unit.Version = version;
            mVersion = version;
            return Component;
        }
        public IViewComponent Get(ViewEntityInternal entity)
        {
            if (mIdIdxMap.TryGetValue(entity.Index, out int idx))
            {
                return Component;
            }
            return null;
        }
        public IViewComponent Modify(ViewEntityInternal entity, ulong version)
        {
            if (mIdIdxMap.TryGetValue(entity.Index, out int idx))
            {
                var unit = mUnits[idx];
                unit.Version = version;
                mVersion = version;
                return Component;
            }
            return null;
        }
        public void Remove(ViewEntityInternal entity)
        {
            if (mIdIdxMap.TryGetValue(entity.Index, out int idx))
            {
                var unit = mUnits[idx];
                unit.Reset();
                mUnUsedIdxs.Enqueue(idx);
                mIdIdxMap.Remove(entity.Index);
            }
        }
        public void RemoveAll()
        {
            foreach (var kv in mIdIdxMap)
            {
                var unit = mUnits[kv.Value];
                mUnUsedIdxs.Enqueue(kv.Value);
                unit.Reset();
            }
            mIdIdxMap.Clear();
        }
        public EntityFindResult<T> Find(int startIndex, ulong version, bool includeDisable)
        {
            if (mVersion > version)
            {
                for (int i = startIndex; i < mUnits.Count; ++i)
                {
                    var unit = mUnits[i];
                    if (unit.Owner == null)
                        continue;
                    if (!includeDisable && unit.Owner.State != ViewEntityInternal.EntityState.Loaded)
                        continue;
                    if (unit.Version > version)
                    {
                        return new EntityFindResult<T>()
                        {
                            Entity = unit.Owner.ToEntity(),
                            Index = i + 1,
                            Version = unit.Version,
                            Component = Component
                        };
                    }
                }
            }
            return new EntityFindResult<T>() { Index = mUnits.Count };
        }

        public EntityFindResult<T> MatchFind<TMatcher>(int startIndex, ulong version, bool includeDisable, TMatcher matcher) where TMatcher : IViewComponentMatcher<T>
        {
            if (mVersion > version)
            {
                for (int i = startIndex; i < mUnits.Count; ++i)
                {
                    var unit = mUnits[i];
                    if (unit.Owner == null)
                        continue;
                    if (!includeDisable && unit.Owner.State != ViewEntityInternal.EntityState.Loaded)
                        continue;
                    if (unit.Version > version && matcher.Match(unit.Owner.ToEntity(), Component))
                    {
                        return new EntityFindResult<T>()
                        {
                            Entity = unit.Owner.ToEntity(),
                            Index = i + 1,
                            Version = unit.Version,
                            Component = Component
                        };
                    }
                }
            }
            return new EntityFindResult<T>() { Index = mUnits.Count };
        }
    }
}