using System;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    // 配置资产基类，存储一个 NPC 类型的所有 Action 和 Goal 数据
    // 通过子类化并标注 [GOAPTag(...)] 声明可用 Action 分组，子类自行添加 [CreateAssetMenu]
    // 子类必须实现 BoolKeyType 和 IntKeyType，分别返回用作 bool/int 状态键的枚举类型
    public abstract class ConfigAsset : ScriptableObject
    {
        // 返回用作 bool 状态键的枚举类型（由子类定义）
        public abstract Type BoolKeyType { get; }

        // 返回用作 int 状态键的枚举类型（由子类定义）
        public abstract Type IntKeyType { get; }

        // 此配置中定义的所有 Action
        public List<ActionData> Actions = new List<ActionData>();

        // 此配置中定义的所有 Goal
        public List<GoalData> Goals = new List<GoalData>();
    }
}
