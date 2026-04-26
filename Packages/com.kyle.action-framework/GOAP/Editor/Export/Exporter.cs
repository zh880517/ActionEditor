namespace GOAP.EditorView
{
    // 将 ConfigAsset 转换为运行时数据对象
    // 序列化方式由调用方自行决定
    public static class Exporter
    {
        public static GOAPRuntimeData Export(ConfigAsset asset)
        {
            var runtimeData = new GOAPRuntimeData
            {
                Name = asset.name
            };

            foreach (var actionData in asset.Actions)
            {
                runtimeData.Actions.Add(actionData.Export());
            }

            foreach (var goalData in asset.Goals)
            {
                runtimeData.Goals.Add(goalData.Export());
            }

            return runtimeData;
        }
    }
}
