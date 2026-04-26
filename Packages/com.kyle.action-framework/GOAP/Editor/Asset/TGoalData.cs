using System.Collections.Generic;
using UnityEditor;

namespace GOAP
{
    // 快速扩展 GoalData 额外字段的泛型模板（类比 TActionData<T>）
    // T 是 struct，包含该 Goal 类型的特有字段（实现 IGoalData）
    // 使用示例：
    //   public struct SurviveConfig : IGoalData { ... }
    //   public class SurviveGoalData : TGoalData<SurviveConfig> { }
    public abstract class TGoalData<T> : GoalData where T : struct, IGoalData
    {
        public T Config;

        public override string Id => Config.Id;

        public override GoalRuntimeData Export()
        {
            var exportData = new TGoalRuntimeData<T>();
            exportData.Id = Config.Id;
            exportData.Name = name;
            exportData.BasePriority = BasePriority;
            exportData.DesiredState = new List<WorldStateEntry>(DesiredState);
            exportData.Data = Config;
            return exportData;
        }
    }
}
