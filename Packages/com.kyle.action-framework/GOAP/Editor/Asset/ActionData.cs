using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    // ActionData 是 Action 的编辑器数据基类（ScriptableObject），存为 ConfigAsset 子资产
    // 子类通过继承扩展额外字段，并标注 [XxxGroup]（ActionGroupAttribute 子类）声明分组
    public abstract class ActionData : ScriptableObject
    {
        // 对应运行时 IAction.Id，用于绑定执行逻辑
        public string Id = "NewAction";

        public string DisplayName = "New Action";

        // 规划代价
        public float Cost = 1f;

        // 前置条件键值对列表
        public List<WorldStateEntry> Preconditions = new List<WorldStateEntry>();

        // 效果键值对列表
        public List<WorldStateEntry> Effects = new List<WorldStateEntry>();

        // 编辑器折叠状态（不导出到运行时）
        public bool FoldoutPreconditions = true;
        public bool FoldoutEffects = true;
    }
}
