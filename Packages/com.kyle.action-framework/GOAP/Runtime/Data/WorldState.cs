using System;
using System.Collections.Generic;

namespace GOAP
{
    // NPC 世界状态，用字典存储任意键值对
    // 规划器通过 Clone() 生成快照后进行搜索，不污染实际状态
    public class WorldState
    {
        private readonly Dictionary<string, object> _state = new Dictionary<string, object>();

        public IReadOnlyDictionary<string, object> State => _state;

        public void Set(string key, object value)
        {
            _state[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_state.TryGetValue(key, out var obj) && obj is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public bool Contains(string key) => _state.ContainsKey(key);

        public void Remove(string key) => _state.Remove(key);

        public void Clear() => _state.Clear();

        // 生成深拷贝快照，规划时使用，避免污染实际状态
        public WorldState Clone()
        {
            var clone = new WorldState();
            foreach (var kvp in _state)
                clone._state[kvp.Key] = kvp.Value;
            return clone;
        }

        // 检查 this 是否满足 subset 中所有键值对（值相等判断用 Equals）
        public bool Satisfies(WorldState subset)
        {
            foreach (var kvp in subset._state)
            {
                if (!_state.TryGetValue(kvp.Key, out var val))
                    return false;
                if (!Equals(val, kvp.Value))
                    return false;
            }
            return true;
        }

        // 将 effects 中的所有键值对写入 this（模拟行动执行后的状态变化）
        public void Apply(WorldState effects)
        {
            foreach (var kvp in effects._state)
                _state[kvp.Key] = kvp.Value;
        }

        // 用于调试，返回所有键值对的字符串表示
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in _state)
                sb.Append($"{kvp.Key}={kvp.Value} ");
            return sb.ToString().TrimEnd();
        }
    }
}
