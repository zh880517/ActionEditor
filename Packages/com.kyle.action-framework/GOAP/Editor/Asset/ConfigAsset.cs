using System.Collections.Generic;
using UnityEngine;

namespace GOAP.EditorView
{
    // 配置资产基类，存储一个 NPC 类型的所有 Action 和 Goal 数据
    // 通过子类化并标注 [GOAPTag(...)] 声明可用 Action 分组，子类自行添加 [CreateAssetMenu]
    public class ConfigAsset : ScriptableObject
    {
        public string ConfigName = "NewConfig";

        // 此配置中定义的所有 Action
        public List<ActionData> Actions = new List<ActionData>();

        // 此配置中定义的所有 Goal
        public List<GoalData> Goals = new List<GoalData>();
    }
}
