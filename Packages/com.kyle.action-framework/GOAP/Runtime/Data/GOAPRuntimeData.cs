using System;
using System.Collections.Generic;

namespace GOAP
{
    public class ActionRuntimeData
    {
        public long ID;
        public string Name;
        public float Cost;
        public List<WorldStateEntry> Preconditions = new List<WorldStateEntry>();
        public List<WorldStateEntry> Effects = new List<WorldStateEntry>();
    }

    public class TActionRuntimeData<T> : ActionRuntimeData where T : IActionData
    {
        public T Data;
    }
    // 编辑器导出后的运行时数据，用于初始化 Agent 的数据部分
    // 执行行为（IAction.Perform）由游戏代码按 Id 注册，与此数据分离
    [Serializable]
    public class GOAPRuntimeData
    {
        public string Name;
        public List<ActionRuntimeData> Actions = new List<ActionRuntimeData>();
        public List<GoalRuntimeData> Goals = new List<GoalRuntimeData>();
    }

    // Goal 的可序列化数据（仅数据，不含优先级逻辑）
    [Serializable]
    public class GoalRuntimeData
    {
        public string Id;
        public string Name;
        public float BasePriority;
        public List<WorldStateEntry> DesiredState = new List<WorldStateEntry>();
    }

    // 携带强类型 IGoalData struct 的目标运行时数据（类比 TActionRuntimeData<T>）
    // Data 字段可直接用于创建 Goal<T> 实例
    public class TGoalRuntimeData<T> : GoalRuntimeData where T : IGoalData
    {
        public T Data;
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
}
