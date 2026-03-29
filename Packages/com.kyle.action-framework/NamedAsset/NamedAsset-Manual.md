# NamedAsset 操作手册

## 概述

NamedAsset 是一个基于名称的资源管理框架，统一了 Editor（`AssetDatabase`）和 Runtime（`AssetBundle`）两种加载模式。核心特性：

- **按名称加载**：资源以 `{PackageName}/{assetname}` 的形式索引（文件名统一转为小写），业务层无需关心物理路径
- **Handle 生命周期**：基于 Version 的句柄机制防止资源重复释放（use-after-free safe）
- **GameObject 对象池**：异步实例化 + 自动回收复用，支持组件状态重置
- **依赖自动分包**：可扩展的策略链处理资源依赖的 Bundle 划分

---

## 目录结构

```
NamedAsset/
├── Runtime/                        # 运行时程序集（全平台）
│   ├── AssetManager.cs             # 静态资源管理器入口
│   ├── AssetRequest.cs             # 资源句柄 struct
│   ├── GameObjectPool.cs           # GameObject 对象池
│   ├── Resetable.cs                # 可重置组件基类
│   ├── PoolableEntity.cs           # 池化实体组件（内部）
│   ├── AsyncFileUtil.cs            # 异步文件加载工具
│   ├── IPathProvider.cs            # 路径提供者接口
│   ├── IAssetProvider.cs           # 资源加载提供者接口
│   ├── Attribute/                  # Inspector 属性标记
│   ├── Manifest/                   # 序列化清单结构
│   ├── AssetDatabase/              # Editor 模式加载器
│   └── AssetBundle/                # Bundle 模式加载器
└── Editor/                         # 编辑器程序集
    ├── Setting/                    # 打包配置
    ├── Builder/                    # 打包构建器 & 策略
    ├── Monitor/                    # 资源导入监听
    ├── Drawer/                     # Inspector 属性绘制器
    └── View/                       # 编辑器窗口 & UI
```

---

## 快速开始

### 1. 配置打包设置

打开菜单 **Window → 打包资源编辑器**，在 Inspector 中添加 `Packages` 条目：

| 字段 | 说明 | 示例 |
|------|------|------|
| `Name` | 包名（资源名前缀） | `UI` |
| `PackType` | 打包模式 | `PackAllInOne` / `PackSingleFile` / `PackDirectory` |
| `Path` | 资源根目录 | `Assets/Art/UI` |
| `SearchPattern` | 文件过滤 | `*.prefab` |
| `SearchOption` | 是否递归子目录 | `AllDirectories` / `TopDirectoryOnly` |

**打包模式说明：**

| 模式 | 行为 | 适用场景 |
|------|------|----------|
| `PackAllInOne` | 目录下所有匹配资源打入单一 Bundle | 小体积常驻资源（通用 UI） |
| `PackSingleFile` | 每个资源独立一个 Bundle | 大型独立资源（场景、角色） |
| `PackDirectory` | 按所在子目录分组打 Bundle | 按功能/场景组织的中等粒度资源 |

### 2. 配置依赖分包策略

在 `DependencePackPolicies` 列表中按优先级添加策略（ScriptableObject）：

- **FolderPackPolicy**：按文件所在目录分 Bundle，可设置 `FolderLimit`（跳过的目录）和 `FileExternLimit`（仅处理的扩展名）
- **MaterialPackByShaderPolicy**：将 `.mat` 文件按 Shader 名称分组
- **自定义策略**：继承 `DependencePackPolicy`，实现 `PackDependence`

策略按列表顺序执行，每个策略返回未处理的文件列表传递给下一个。最终剩余文件归入 `dependence_other` Bundle。

### 3. 构建 AssetBundle

在打包资源编辑器窗口点击 **Build AssetBundle**，构建产物输出到 `PackExportPath`（默认 `StreamingAssets/bundle/`）。

构建流程：
1. 收集所有 Package 下匹配的资源，校验重名
2. 通过 `AssetDatabase.GetDependencies` 收集所有隐式依赖
3. 依次执行依赖分包策略
4. 调用 `BuildPipeline.BuildAssetBundles` 打包
5. 生成 `AssetManifest.json`（记录资源名→Bundle位置的映射）
6. 清理不再使用的旧 Bundle 文件

---

## 运行时 API

### 初始化

```csharp
// 实现 IPathProvider 提供 Manifest 和 Bundle 的文件路径
public class MyPathProvider : IPathProvider
{
    public FileLocaltion GetAssetManifestPath()
    {
        return new FileLocaltion
        {
            Path = Path.Combine(Application.streamingAssetsPath, "bundle/AssetManifest.json"),
            Type = FilePathType.File
        };
    }

    public FileLocaltion GetAssetBundlePath(string bundleName)
    {
        return new FileLocaltion
        {
            Path = Path.Combine(Application.streamingAssetsPath, "bundle/", bundleName),
            Type = FilePathType.File
        };
    }
}

// 初始化（通常在游戏启动时调用一次）
await AssetManager.Initialize(new MyPathProvider());
```

> **Editor 模式**默认使用 `AssetDatabase` 直接加载，无需打包。可在打包资源编辑器中勾选 "编辑器模式使用AssetBundle方式加载" 切换为 Bundle 模式调试。

### 加载资源

```csharp
// 加载资源，名称格式为 "{PackageName}/{assetfilename}"（文件名全小写，不含扩展名）
var request = await AssetManager.LoadAsset<Sprite>("UI/icon_star");

if (request.IsValid)
{
    Sprite sprite = request.Asset;
    // 使用资源...
}

// 使用完毕后释放（重复调用 Release 安全，不会二次释放）
request.Release();
```

**`AssetRequestResult` 错误码：**

| 值 | 含义 |
|----|------|
| `Success` | 加载成功 |
| `BundleNotFound` | 找不到对应 Bundle |
| `BundleLoadFailed` | Bundle 加载失败 |
| `AssetNotFound` | 资源名在 Manifest 中不存在 |
| `AssetLoadFailed` | 资源加载失败（类型不匹配等） |

### GameObject 实例化与对象池

```csharp
// 异步实例化（内部自动池化）
GameObject go = await AssetManager.InstantiateAsync("Character/hero_01", parentTransform);

// 归还到对象池（不销毁，SetActive(false) 等待复用）
AssetManager.ReleaseInstance(go);

// 销毁所有池中闲置实例
AssetManager.ClearPool();
```

### 资源卸载

```csharp
// 卸载所有引用计数归零的资源和 Bundle
AssetManager.ClearUnusedAssets();

// 完全销毁（游戏退出或切换场景时）
AssetManager.Destroy();
```

---

## 对象池与 Resetable 组件

当 GameObject 通过 `ReleaseInstance` 回收时，挂载的所有 `Resetable` 子类会收到 `OnReset()` 回调，用于清理运行时状态。

```csharp
public class EnemyController : Resetable
{
    private int hp;
    private bool isDead;

    protected override void OnEnable()
    {
        base.OnEnable(); // 必须调用 base，注册到 PoolableEntity
        hp = 100;
        isDead = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // 必须调用 base，从链表中移除
    }

    public override void OnReset()
    {
        // 池回收时清理状态
        hp = 100;
        isDead = false;
        // 取消正在播放的动画、特效等
    }
}
```

> **注意**：`OnEnable`/`OnDisable` 必须调用 `base` 方法，否则该组件无法正确注册到池化管理链表。

---

## Inspector 属性

### `[NamedAssetSelect]`

标记 `string` 字段，在 Inspector 中显示资源搜索选择器。

```csharp
[NamedAssetSelect]
public string heroAsset; // 值为 "Character/hero_01" 格式
```

### `[FolderSelector]`

标记 `string` 字段，在 Inspector 中显示文件夹选择按钮。

```csharp
[FolderSelector]
public string outputFolder; // 值为 "Assets/..." 格式的相对路径
```

---

## `IPathProvider` 路径模式

`FileLocaltion` 支持四种路径类型，适配不同平台和部署方式：

| `FilePathType` | 用途 | 典型场景 |
|----------------|------|----------|
| `File` | 本地文件路径 | PC/iOS 的 StreamingAssets |
| `CombinFile` | 合并文件（偏移+长度） | 多 Bundle 合并为单文件时按范围读取 |
| `URL` | HTTP 地址 | CDN 热更新，Android StreamingAssets |
| `Bytes` | 内存字节数组 | 解密后的 Bundle 数据 |

```csharp
// 合并文件示例（多个 Bundle 拼接在一个文件中）
public FileLocaltion GetAssetBundlePath(string bundleName)
{
    var entry = fileTable[bundleName]; // 自定义查表
    return new FileLocaltion
    {
        Path = combinedFilePath,
        Type = FilePathType.CombinFile,
        Offset = entry.Offset,
        Length = entry.Length
    };
}
```

---

## 自定义依赖分包策略

继承 `DependencePackPolicy` 并通过 `CreateAssetMenu` 创建 ScriptableObject 实例：

```csharp
[CreateAssetMenu(fileName = "MyPolicy", menuName = "NamedAsset/Policy/自定义策略")]
public class MyPackPolicy : DependencePackPolicy
{
    public override List<string> PackDependence(AssetPackageBuilder builder, List<string> files)
    {
        var remain = new List<string>();
        foreach (var file in files)
        {
            if (ShouldHandle(file))
            {
                string bundleName = ComputeBundleName(file);
                builder.PackDepenceFile(file, bundleName);
            }
            else
            {
                remain.Add(file);
            }
        }
        return remain; // 未处理的文件传给下一个策略
    }
}
```

---

## 常见问题

### Q: Editor 下加载资源返回 AssetNotFound？
确认资源所在目录已在 **打包资源编辑器** 的 Packages 列表中配置，且 `SearchPattern` 匹配该文件类型。配置变更后 `AssetCollector` 会自动刷新。

### Q: 如何在 Editor 下测试 Bundle 加载？
在打包资源编辑器中勾选 "编辑器模式使用AssetBundle方式加载"，然后先执行一次 Build AssetBundle。

### Q: 资源重名报错如何处理？
同一 `Package.Name` 下不允许出现同名资源（去掉扩展名后的文件名）。不同 Package 下可以同名，因为最终资源名包含包名前缀。

### Q: 对象池的 GameObject 被外部 Destroy 了怎么办？
`GameObjectPool.GetAsync` 从池中取出时会跳过已被销毁的实例。不会报错，但会产生额外的 Instantiate 开销。

### Q: Bundle 加载并发数如何控制？
- `AssetBundleProvider.MaxLoadBundleCount`：最大同时加载 Bundle 数（默认 10），超出的请求自动排队
- `AssetDatabaseProvider.MaxLoadAssetCount`：Editor 模式最大并发数，通过 `AssetManager.SetEditorModeMaxLoadCount()` 设置
