using UnityEngine;

namespace ActionLine.EditorView
{
    /// <summary>
    /// 场景资源管理，Unity编辑器资源重载的时候记录已经实例化的资源，避免重复加载。
    /// </summary>
    public class PreviewResourceContext : ScriptableObject
    {
    }
}
