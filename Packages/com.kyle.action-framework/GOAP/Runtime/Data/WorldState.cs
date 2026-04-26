using System.Collections.Generic;

namespace GOAP
{
    // NPC 世界状态，用整数键值对存储状态
    // 键为枚举整数值（由 ConfigAsset.BoolKeyType / IntKeyType 定义），值统一用 int 存储
    // bool 状态以 0（false）/ 非零（true）表示；BoolKey 与 IntKey 枚举值不应重叠
    // 规划器通过 Clone() 生成快照后进行搜索，不污染实际状态
    public class WorldState
    {
        private readonly Dictionary<int, int> _state = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> State => _state;

        public void Set(int key, int value)
        {
            _state[key] = value;
        }

        public bool TryGet(int key, out int value)
        {
            return _state.TryGetValue(key, out value);
        }

        public bool Contains(int key) => _state.ContainsKey(key);

        public void Remove(int key) => _state.Remove(key);

        public void Clear() => _state.Clear();

        // 生成深拷贝快照，规划时使用，避免污染实际状态
        public WorldState Clone()
        {
            var clone = new WorldState();
            foreach (var kvp in _state)
                clone._state[kvp.Key] = kvp.Value;
            return clone;
        }

        // 检查 this 是否满足 subset 中所有键值对（值相等判断）
        public bool Satisfies(WorldState subset)
        {
            foreach (var kvp in subset._state)
            {
                if (!_state.TryGetValue(kvp.Key, out var val))
                    return false;
                if (val != kvp.Value)
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
