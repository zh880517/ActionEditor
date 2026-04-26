using System;
using System.Collections.Generic;

namespace GOAP
{
    internal class MinHeap<T> where T : class
    {
        private readonly List<T> _data = new List<T>();
        private readonly Comparison<T> _compare;

        public int Count => _data.Count;

        public MinHeap(Comparison<T> compare)
        {
            _compare = compare;
        }

        public void Push(T item)
        {
            _data.Add(item);
            SiftUp(_data.Count - 1);
        }

        public T Pop()
        {
            var top = _data[0];
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            if (_data.Count > 0)
                SiftDown(0);
            return top;
        }

        public void Clear() => _data.Clear();

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_compare(_data[i], _data[parent]) >= 0) break;
                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            int count = _data.Count;
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= count) break;
                int right = left + 1;
                int smallest = (right < count && _compare(_data[right], _data[left]) < 0) ? right : left;
                if (_compare(_data[i], _data[smallest]) <= 0) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = _data[a];
            _data[a] = _data[b];
            _data[b] = tmp;
        }
    }
}
