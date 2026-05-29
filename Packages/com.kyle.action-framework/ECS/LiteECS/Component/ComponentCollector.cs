using System.Collections.Generic;

namespace ECSLite
{
    internal class ComponentCollector<T> : IComponentCollectorT<T> where T : class, IComponent, new()
    {
        private readonly List<ComponentEntity<T>> mUnits = new List<ComponentEntity<T>>();
        private readonly Queue<int> mUnUsedIdxs = new Queue<int>();
        private readonly Dictionary<int, int> mIdIdxMap = new Dictionary<int, int>();//index => 数组索引
        private int mHeadIdx = -1;
        private int mTailIdx = -1;
        public int Count => mIdIdxMap.Count;

        private ComponentEntity<T> Create()
        {
            if (mUnUsedIdxs.Count > 0)
            {
                var index = mUnUsedIdxs.Dequeue();
                return mUnits[index];
            }
            var unit = new ComponentEntity<T>();
            unit.Index = mUnits.Count;
            mUnits.Add(unit);
            return unit;
        }

        private void LinkLast(ComponentEntity<T> unit)
        {
            unit.PreviousIndex = mTailIdx;
            unit.NextIndex = -1;
            if (mTailIdx >= 0)
            {
                mUnits[mTailIdx].NextIndex = unit.Index;
            }
            else
            {
                mHeadIdx = unit.Index;
            }
            mTailIdx = unit.Index;
        }

        private void Unlink(ComponentEntity<T> unit)
        {
            if (unit.PreviousIndex >= 0)
            {
                mUnits[unit.PreviousIndex].NextIndex = unit.NextIndex;
            }
            else
            {
                mHeadIdx = unit.NextIndex;
            }

            if (unit.NextIndex >= 0)
            {
                mUnits[unit.NextIndex].PreviousIndex = unit.PreviousIndex;
            }
            else
            {
                mTailIdx = unit.PreviousIndex;
            }
        }

        public IComponent Add(int entityIdx)
        {
            if (mIdIdxMap.TryGetValue(entityIdx, out int idx))
            {
                var exist = mUnits[idx];
                return exist.Component;
            }
            var unit = Create();
            mIdIdxMap.Add(entityIdx, unit.Index);
            unit.EntityIdx = entityIdx;
            LinkLast(unit);
            return unit.Component;
        }

        public ComponentFindResult<T> Find(int startIndex)
        {
            int idx = startIndex == 0 ? mHeadIdx : startIndex - 1;
            if (idx >= 0)
            {
                var unit = mUnits[idx];
                return new ComponentFindResult<T>()
                {
                    EntityIndex = unit.EntityIdx,
                    Index = unit.NextIndex >= 0 ? unit.NextIndex + 1 : -1,
                    Component = unit.Component
                };
            }
            return new ComponentFindResult<T>(){ Index = -1 };
        }

        public ComponentFindResult<T> MatchFind<TMatcher>(int startIndex, TMatcher matcher) where TMatcher : IComponentMatcher<T>
        {
            int idx = startIndex == 0 ? mHeadIdx : startIndex - 1;
            while (idx >= 0)
            {
                var unit = mUnits[idx];
                if (matcher.Match(unit.Component))
                {
                    return new ComponentFindResult<T>()
                    {
                        EntityIndex = unit.EntityIdx,
                        Index = unit.NextIndex >= 0 ? unit.NextIndex + 1 : -1,
                        Component = unit.Component
                    };
                }
                idx = unit.NextIndex;
            }
            return new ComponentFindResult<T>() { Index = -1 };
        }

        public IComponent Get(int entityIdx)
        {
            if (mIdIdxMap.TryGetValue(entityIdx, out int idx))
            {
                return mUnits[idx].Component;
            }
            return null;
        }

        public void Remove(int entityIdx)
        {
            if (mIdIdxMap.TryGetValue(entityIdx, out int idx))
            {
                var unit = mUnits[idx];
                Unlink(unit);
                unit.Reset();
                mUnUsedIdxs.Enqueue(idx);
                mIdIdxMap.Remove(entityIdx);
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
            mHeadIdx = -1;
            mTailIdx = -1;
            mIdIdxMap.Clear();
        }

    }
}
