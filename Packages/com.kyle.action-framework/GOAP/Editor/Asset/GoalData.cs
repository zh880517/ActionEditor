using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    // GoalData 是 Goal 的编辑器数据基类（ScriptableObject），存为 ConfigAsset 子资产
    // 子类通过继承扩展额外字段，并标注具体 IGoalData struct
    public abstract class GoalData : ScriptableObject
    {
        // 目标唯一标识，由子类通过 Config.Id 提供（只读）
        public abstract string Id { get; }

        // 基础优先级（游戏代码可在运行时动态调整）
        public float BasePriority = 50f;

        // 期望终态键值对列表
        public List<WorldStateEntry> DesiredState = new List<WorldStateEntry>();

        // 编辑器折叠状态（不导出到运行时）
        public bool FoldoutDesiredState = true;

        public abstract GoalRuntimeData Export();
    }
}
