using UnityEditor;


internal class ScriptObjectAssetMonitor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,string[] movedAssets, string[] movedFromAssetPaths)
    {
        if(!ScriptObjectCollector.HasInstance)
            return;

    }
}
