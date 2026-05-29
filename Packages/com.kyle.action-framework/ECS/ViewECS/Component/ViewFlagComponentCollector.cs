using System.Collections.Generic;
namespace VECS
{
    //减少Flag类型Component多余的New
    internal class ViewFlagComponentCollector<T> : IComponentCollectorT<T> where T : class, IViewComponent, new()
    {
        private ulong mVersion;
        private readonly List<FlagComponentEntity<T>> mUnits = new List<FlagComponentEntity<T>>();
        private readonly Stack<FlagComponentEntity<T>> mUnUsedUnits = new Stack<FlagComponentEntity<T>>();
        private readonly Dictionary<int, int> mIdIdxMap = new Dictionary<int, int>();//EntityId => 数组索引
        private T Component = new T();
        public int Count => mIdIdxMap.Count;

        private FlagComponentEntity<T> Create()
        {
            if (mUnUsedUnits.Count > 0)
            {
                return mUnUsedUnits.Pop();
            }
            return new FlagComponentEntity<T>();
        }

        private void RemoveAt(int idx, int entityIndex)
        {
            var unit = mUnits[idx];
            mIdIdxMap.Remove(entityIndex);

            int lastIdx = mUnits.Count - 1;
            if (idx != lastIdx)
            {
                var last = mUnits[lastIdx];
                mUnits[idx] = last;
                last.Index = idx;
                mIdIdxMap[last.Owner.Index] = idx;
            }

            mUnits.RemoveAt(lastIdx);
            unit.Reset();
            mUnUsedUnits.Push(unit);
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
            unit.Index = mUnits.Count;
            mUnits.Add(unit);
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
                RemoveAt(idx, entity.Index);
            }
        }
        public void RemoveAll()
        {
            for (int i = 0; i < mUnits.Count; ++i)
            {
                var unit = mUnits[i];
                unit.Reset();
                mUnUsedUnits.Push(unit);
            }
            mUnits.Clear();
            mIdIdxMap.Clear();
        }
        public EntityFindResult<T> Find(int startIndex, ulong version, bool includeDisable)
        {
            if (mVersion > version)
            {
                for (int i = startIndex; i < mUnits.Count; ++i)
                {
                    var unit = mUnits[i];
                    if (unit.Owner != null
                        && (includeDisable || unit.Owner.State == ViewEntityInternal.EntityState.Loaded)
                        && unit.Version > version)
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
                    if (unit.Owner != null
                        && (includeDisable || unit.Owner.State == ViewEntityInternal.EntityState.Loaded)
                        && unit.Version > version
                        && matcher.Match(unit.Owner.ToEntity(), Component))
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
