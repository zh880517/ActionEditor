using System.IO;
using UnityEditor;
using UnityEngine;

namespace GOAP.EditorView
{
    // 将 ConfigAsset 导出为运行时 JSON 数据文件
    // 导出文件与 .asset 同目录，命名为 <ConfigName>_runtime.json
    public static class Exporter
    {
        public static void Export(ConfigAsset asset)
        {
            var runtimeData = new RuntimeData
            {
                Name = asset.name
            };

            // 转换 Action 数据
            foreach (var actionData in asset.Actions)
            {
                runtimeData.Actions.Add(new SerializedActionData
                {
                    Id = actionData.Id,
                    DisplayName = actionData.DisplayName,
                    Cost = actionData.Cost,
                    Preconditions = new System.Collections.Generic.List<WorldStateEntry>(actionData.Preconditions),
                    Effects = new System.Collections.Generic.List<WorldStateEntry>(actionData.Effects)
                });
            }

            // 转换 Goal 数据
            foreach (var goalData in asset.Goals)
            {
                runtimeData.Goals.Add(new SerializedGoalData
                {
                    Id = goalData.Id,
                    DisplayName = goalData.DisplayName,
                    BasePriority = goalData.BasePriority,
                    DesiredState = new System.Collections.Generic.List<WorldStateEntry>(goalData.DesiredState)
                });
            }

            // 序列化为 JSON 并写入文件
            string json = JsonUtility.ToJson(runtimeData, prettyPrint: true);
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string dirPath = Path.GetDirectoryName(assetPath);
            string outputPath = Path.Combine(dirPath, $"{asset.name}_runtime.json");

            File.WriteAllText(outputPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"[GOAP] 导出成功: {outputPath}");
        }
    }
}
