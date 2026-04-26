using System;
using System.Collections.Generic;

namespace GOAP
{
    // 编辑器导出后的运行时数据，用于初始化 Agent 的数据部分
    // 执行行为（IAction.Perform）由游戏代码按 Id 注册，与此数据分离
    [Serializable]
    public class RuntimeData
    {
        public string Name;
        public List<SerializedActionData> Actions = new List<SerializedActionData>();
        public List<SerializedGoalData> Goals = new List<SerializedGoalData>();
    }

    // Action 的可序列化数据（仅数据，不含执行逻辑）
    [Serializable]
    public class SerializedActionData
    {
        public string Id;
        public string DisplayName;
        public float Cost;
        public List<WorldStateEntry> Preconditions = new List<WorldStateEntry>();
        public List<WorldStateEntry> Effects = new List<WorldStateEntry>();
    }

    // Goal 的可序列化数据（仅数据，不含优先级逻辑）
    [Serializable]
    public class SerializedGoalData
    {
        public string Id;
        public string DisplayName;
        public float BasePriority;
        public List<WorldStateEntry> DesiredState = new List<WorldStateEntry>();
    }

    // WorldState 值类型：仅支持 bool 和 int
    public enum WorldStateValueType
    {
        Bool,
        Int
    }

    // WorldState 中单个键值对的可序列化表示
    // Key 以枚举整数值存储，ValueType 区分键来自 bool 组还是 int 组枚举
    // Value 统一使用 int 存储：bool 以 0（false）/ 1（true）表示
    [Serializable]
    public struct WorldStateEntry
    {
        // 值类型（Bool 或 Int），决定使用哪组枚举解析 KeyIndex
        public WorldStateValueType ValueType;
        // 键（对应 ConfigAsset.BoolKeyType 或 IntKeyType 枚举的整数值）
        public int KeyIndex;
        // 值：bool 用 0/1 表示，int 直接存储
        public int Value;
    }

    // WorldStateEntry 与运行时 WorldState 之间的转换工具
    public static class WorldStateEntryConverter
    {
        // 将 List<WorldStateEntry> 转换为 WorldState
        // Key 直接使用 KeyIndex（枚举整数值），Value 直接存储（bool 以 0/1 表示）
        // 注意：BoolKeyType 与 IntKeyType 枚举的整数值不应重叠
        public static WorldState ToWorldState(List<WorldStateEntry> entries)
        {
            var ws = new WorldState();
            foreach (var entry in entries)
                ws.Set(entry.KeyIndex, entry.Value);
            return ws;
        }
    }
}
