# ScriptObjectCollector 资产收集器

路径：`Common/Editor/ScriptObjectCollector/`

`ScriptableSingleton`，在编辑器会话中追踪和缓存磁盘上的 `.asset` 文件列表，并通过 `ScriptObjectAssetMonitor`（`AssetPostprocessor`）监听资产的导入、移动和删除事件，实时更新缓存。

---

## 核心 API

### `ScriptObjectCollector`

| 成员 | 说明 |
|------|------|
| `GetAssets(MonoScript script, string rootPath)` | 返回 `rootPath` 目录下所有属于 `script` 类型的 `.asset` 文件路径列表（`IReadOnlyList<string>`）；首次调用时扫描磁盘并缓存，后续由监听器增量维护 |
| `OnAssetChanged` | `static event Action<MonoScript>`；当某类型的 `.asset` 发生变化（新增/删除/移动）时触发，携带对应的 `MonoScript` |
| `HasInstance` | 是否已初始化单例（避免无意创建） |

### `ScriptObjectAssetMonitor`

内部 `AssetPostprocessor`，在 `OnPostprocessAllAssets` 中检测 `.asset` 变动并通知 `ScriptObjectCollector` 更新缓存，同时触发 `OnAssetChanged` 事件。

---

## 典型用法

```csharp
using UnityEditor;

// 获取指定类型在指定目录下的所有资产路径
MonoScript script = MonoScriptUtil.GetMonoScript(typeof(MyConfig));
IReadOnlyList<string> paths = ScriptObjectCollector.instance.GetAssets(script, "Assets/Configs");

foreach (var path in paths)
{
    var asset = AssetDatabase.LoadAssetAtPath<MyConfig>(path);
    // ...
}

// 监听资产变化（如需实时刷新 UI）
ScriptObjectCollector.OnAssetChanged += OnConfigChanged;

void OnConfigChanged(MonoScript changedScript)
{
    if (changedScript == MonoScriptUtil.GetMonoScript(typeof(MyConfig)))
    {
        RefreshUI();
    }
}
```

---

## 与 CollectableScriptableObject 的关系

`CollectableScriptableObject` 是 `ScriptableObject` 的空基类标记，业务 Asset 继承它后可以被统一标识。`ScriptObjectCollector` 不限于此基类，任何 `ScriptableObject` 派生类都可以被收集，只需提供对应的 `MonoScript` 即可。
