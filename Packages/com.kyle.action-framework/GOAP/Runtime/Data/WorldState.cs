using System;
using System.Collections.Generic;

namespace GOAP
{
    public class WorldState
    {
        public const int MaxKeys = 32;

        private readonly int[] _keys = new int[MaxKeys];
        private readonly int[] _values = new int[MaxKeys];
        private readonly CompareOp[] _ops = new CompareOp[MaxKeys];
        private readonly EffectMode[] _modes = new EffectMode[MaxKeys];
        private int _count;

        public int Count => _count;

        public (int key, int value) GetEntry(int index) => (_keys[index], _values[index]);

        public (int key, int value, CompareOp op, EffectMode mode) GetFullEntry(int index)
            => (_keys[index], _values[index], _ops[index], _modes[index]);

        public void Set(int key, int value,
            CompareOp op = CompareOp.Equal, EffectMode mode = EffectMode.Assign)
        {
            int idx = BinarySearch(key);
            if (idx >= 0)
            {
                _values[idx] = value;
                _ops[idx] = op;
                _modes[idx] = mode;
                return;
            }
            int insertAt = ~idx;
            if (_count >= MaxKeys)
                throw new InvalidOperationException($"WorldState capacity exceeded ({MaxKeys})");
            if (insertAt < _count)
            {
                Array.Copy(_keys, insertAt, _keys, insertAt + 1, _count - insertAt);
                Array.Copy(_values, insertAt, _values, insertAt + 1, _count - insertAt);
                Array.Copy(_ops, insertAt, _ops, insertAt + 1, _count - insertAt);
                Array.Copy(_modes, insertAt, _modes, insertAt + 1, _count - insertAt);
            }
            _keys[insertAt] = key;
            _values[insertAt] = value;
            _ops[insertAt] = op;
            _modes[insertAt] = mode;
            _count++;
        }

        public bool TryGet(int key, out int value)
        {
            int idx = BinarySearch(key);
            if (idx >= 0)
            {
                value = _values[idx];
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetFull(int key, out int value, out CompareOp op, out EffectMode mode)
        {
            int idx = BinarySearch(key);
            if (idx >= 0)
            {
                value = _values[idx];
                op = _ops[idx];
                mode = _modes[idx];
                return true;
            }
            value = default;
            op = default;
            mode = default;
            return false;
        }

        public void SetBool(int enumValue, bool value)
            => Set(WorldStateKey.Encode(WorldStateValueType.Bool, enumValue), value ? 1 : 0);

        public void SetInt(int enumValue, int value)
            => Set(WorldStateKey.Encode(WorldStateValueType.Int, enumValue), value);

        public bool GetBool(int enumValue)
        {
            TryGet(WorldStateKey.Encode(WorldStateValueType.Bool, enumValue), out int v);
            return v != 0;
        }

        public int GetInt(int enumValue)
        {
            TryGet(WorldStateKey.Encode(WorldStateValueType.Int, enumValue), out int v);
            return v;
        }

        public bool Contains(int key) => BinarySearch(key) >= 0;

        public void Remove(int key)
        {
            int idx = BinarySearch(key);
            if (idx < 0) return;
            _count--;
            if (idx < _count)
            {
                Array.Copy(_keys, idx + 1, _keys, idx, _count - idx);
                Array.Copy(_values, idx + 1, _values, idx, _count - idx);
                Array.Copy(_ops, idx + 1, _ops, idx, _count - idx);
                Array.Copy(_modes, idx + 1, _modes, idx, _count - idx);
            }
        }

        public void Clear() => _count = 0;

        public WorldState Clone()
        {
            var clone = new WorldState();
            Array.Copy(_keys, clone._keys, _count);
            Array.Copy(_values, clone._values, _count);
            Array.Copy(_ops, clone._ops, _count);
            Array.Copy(_modes, clone._modes, _count);
            clone._count = _count;
            return clone;
        }

        public void CopyFrom(WorldState other)
        {
            Array.Copy(other._keys, _keys, other._count);
            Array.Copy(other._values, _values, other._count);
            Array.Copy(other._ops, _ops, other._count);
            Array.Copy(other._modes, _modes, other._count);
            _count = other._count;
        }

        public bool Satisfies(WorldState subset)
        {
            for (int i = 0; i < subset._count; i++)
            {
                int idx = BinarySearch(subset._keys[i]);
                if (idx < 0) return false;
                if (!CompareValues(_values[idx], subset._ops[i], subset._values[i]))
                    return false;
            }
            return true;
        }

        public void Apply(WorldState effects)
        {
            for (int i = 0; i < effects._count; i++)
            {
                if (effects._modes[i] == EffectMode.Add)
                {
                    TryGet(effects._keys[i], out int existing);
                    Set(effects._keys[i], existing + effects._values[i]);
                }
                else
                {
                    Set(effects._keys[i], effects._values[i]);
                }
            }
        }

        public bool ExactEquals(WorldState other)
        {
            if (_count != other._count) return false;
            for (int i = 0; i < _count; i++)
            {
                if (_keys[i] != other._keys[i] || _values[i] != other._values[i]
                    || _ops[i] != other._ops[i] || _modes[i] != other._modes[i])
                    return false;
            }
            return true;
        }

        public int ComputeHash()
        {
            int hash = 0;
            for (int i = 0; i < _count; i++)
            {
                unchecked
                {
                    hash ^= _keys[i] * 1000003 ^ _values[i] * 999983
                           ^ (int)_ops[i] * 997 ^ (int)_modes[i] * 991;
                }
            }
            return hash;
        }

        public static WorldState FromEntries(IEnumerable<WorldStateEntry> entries)
        {
            var ws = new WorldState();
            if (entries == null) return ws;
            foreach (var e in entries)
                ws.Set(e.RuntimeKey, e.Value, e.Operator, e.Mode);
            return ws;
        }

        public static bool CompareValues(int lhs, CompareOp op, int rhs)
        {
            switch (op)
            {
                case CompareOp.Equal:          return lhs == rhs;
                case CompareOp.NotEqual:       return lhs != rhs;
                case CompareOp.Greater:        return lhs > rhs;
                case CompareOp.Less:           return lhs < rhs;
                case CompareOp.GreaterOrEqual: return lhs >= rhs;
                case CompareOp.LessOrEqual:    return lhs <= rhs;
                default:                       return lhs == rhs;
            }
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _count; i++)
                sb.Append($"{_keys[i]}={_values[i]} ");
            return sb.ToString().TrimEnd();
        }

        private int BinarySearch(int key) => Array.BinarySearch(_keys, 0, _count, key);
    }
}
