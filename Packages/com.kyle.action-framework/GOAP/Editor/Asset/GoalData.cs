using System;
using System.Collections.Generic;
using GOAP;

namespace GOAP.EditorView
{
    // 编辑器中单个 Goal 的数据，存储在 ConfigAsset 中
    [Serializable]
    public class GoalData
    {
        // 对应运行时 IGoal.Id，用于绑定优先级逻辑
        public string Id = "NewGoal";

        public string DisplayName = "New Goal";

        // 基础优先级（游戏代码可在运行时动态调整）
        public float BasePriority = 50f;

        // 期望终态键值对列表
        public List<WorldStateEntry> DesiredState = new List<WorldStateEntry>();

        // 编辑器折叠状态（不导出到运行时）
        public bool FoldoutDesiredState = true;
    }
}
