using System;
using System.Collections.Generic;

namespace GOAP
{
    // 编辑器导出后的运行时数据根容器，由 Exporter 生成，由 AgentFactory 消费
    [Serializable]
    public class GOAPRuntimeData
    {
        public string Name;
        // 规划数据列表；实际元素类型为 TSerializedActionData<T>（含执行 struct）
        public List<SerializedActionData> Actions = new List<SerializedActionData>();
        public List<GoalRuntimeData> Goals = new List<GoalRuntimeData>();
    }

    // Action 规划数据（Id / Cost / Preconditions / Effects）
    // 不含执行逻辑；执行 struct 由子类 TSerializedActionData<T> 携带
    [Serializable]
    public class SerializedActionData
    {
        public string Id;
        public string Name;
        public float Cost;
        public List<WorldStateEntry> Preconditions = new List<WorldStateEntry>();
        public List<WorldStateEntry> Effects = new List<WorldStateEntry>();
    }

    // 携带强类型执行 struct 的行动序列化数据
    // AgentFactory 通过反射检测此泛型子类，构造 RuntimeAction<T>
    public class TSerializedActionData<T> : SerializedActionData where T : struct, IActionData
    {
        public T Data;
    }

    // Goal 规划数据
    [Serializable]
    public class GoalRuntimeData
    {
        public string Id;
        public string Name;
        public float BasePriority;
        public List<WorldStateEntry> DesiredState = new List<WorldStateEntry>();
    }

    // 携带强类型 IGoalData struct 的目标序列化数据
    public class TGoalRuntimeData<T> : GoalRuntimeData where T : IGoalData
    {
        public T Data;
    }

    // WorldState 值类型：仅支持 bool 和 int
    public enum WorldStateValueType
    {
        Bool = 0,
        Int  = 1
    }

    // 运行时键编码工具
    // 高位掩码将 ValueType 编入 key，使 BoolKey 和 IntKey 枚举值允许重叠
    // 布局：bit30 = ValueType（0=Bool, 1=Int），bit0-29 = 原始枚举值
    public static class WorldStateKey
    {
        public const int TypeBit = 1 << 30;

        public static int Encode(WorldStateValueType type, int enumValue)
            => type == WorldStateValueType.Int ? (enumValue | TypeBit) : enumValue;

        public static (WorldStateValueType type, int enumValue) Decode(int key)
        {
            if ((key & TypeBit) != 0)
                return (WorldStateValueType.Int, key & ~TypeBit);
            return (WorldStateValueType.Bool, key);
        }
    }

    // WorldState 中单个键值对的可序列化表示
    // ValueType + KeyIndex 为编辑器 / 序列化用的原始字段
    // 运行时通过 WorldStateKey.Encode 合并为单个 int 键，BoolKey 和 IntKey 枚举值允许重叠
    [Serializable]
    public struct WorldStateEntry
    {
        // 值类型（Bool 或 Int），决定使用哪组枚举解析 KeyIndex
        public WorldStateValueType ValueType;
        // 键（对应 ConfigAsset.BoolKeyType 或 IntKeyType 枚举的整数值）
        public int KeyIndex;
        // 值：bool 用 0/1 表示，int 直接存储
        public int Value;

        // 运行时编码后的键（ValueType 编入高位）
        public int RuntimeKey => WorldStateKey.Encode(ValueType, KeyIndex);
    }
}
