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

    // WorldState 中单个键值对的可序列化表示
    [Serializable]
    public struct WorldStateEntry
    {
        // 键名
        public string Key;
        // 值的类型名：bool / int / float / string
        public string ValueType;
        // 值内容（JSON 字符串）
        public string ValueJson;
    }

    // WorldStateEntry 与运行时 WorldState 之间的转换工具
    public static class WorldStateEntryConverter
    {
        // 将 List<WorldStateEntry> 反序列化为 WorldState
        public static WorldState ToWorldState(List<WorldStateEntry> entries)
        {
            var ws = new WorldState();
            foreach (var entry in entries)
            {
                var value = ParseValue(entry.ValueType, entry.ValueJson);
                if (value != null)
                    ws.Set(entry.Key, value);
            }
            return ws;
        }

        // 将 WorldState 序列化为 List<WorldStateEntry>（仅支持 bool/int/float/string）
        public static List<WorldStateEntry> FromWorldState(WorldState ws)
        {
            var entries = new List<WorldStateEntry>();
            foreach (var kvp in ws.State)
            {
                var (typeName, json) = SerializeValue(kvp.Value);
                if (typeName != null)
                    entries.Add(new WorldStateEntry { Key = kvp.Key, ValueType = typeName, ValueJson = json });
            }
            return entries;
        }

        private static object ParseValue(string valueType, string json)
        {
            switch (valueType)
            {
                case "bool":   return bool.Parse(json);
                case "int":    return int.Parse(json);
                case "float":  return float.Parse(json, System.Globalization.CultureInfo.InvariantCulture);
                case "string": return json;
                default:       return null;
            }
        }

        private static (string typeName, string json) SerializeValue(object value)
        {
            switch (value)
            {
                case bool b:   return ("bool", b.ToString().ToLower());
                case int i:    return ("int", i.ToString());
                case float f:  return ("float", f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                case string s: return ("string", s);
                default:       return (null, null);
            }
        }
    }
}
