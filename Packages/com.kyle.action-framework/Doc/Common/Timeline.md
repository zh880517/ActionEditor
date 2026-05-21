# Timeline 时间轴控件

路径：`Common/Editor/VisualElement/Timeline/`

可复用的 UIToolkit 时间轴组件，是 ActionLine 编辑器的 UI 基础，也可独立嵌入其他编辑器窗口。

---

## 组件概览

```
TimelineView                      顶层容器
├── TickMarkView                  帧刻度栏
├── CursorView                    当前帧指针
├── trackContainer
│   └── TrackView × N             每条轨道
│       ├── ClipView × N          Clip 显示块（只读，不处理输入）
│       └── OverlapDrawElement    重叠区域绘制层
└── InsertIndicatorElement        轨道拖拽插入位置指示线
```

---

## 核心类

### TimelineView

顶层容器，统一管理所有轨道和全局状态。

| 属性/方法 | 说明 |
|---------|------|
| `FrameWidth` | 每帧像素宽度（缩放基准） |
| `TitleBarHeight` | 刻度栏高度 |
| `AutoHeight` | 开启后根据轨道数自动调整高度并隐藏垂直滚动条 |
| `OnClipMoved` | `Action<string clipKey, int frameDelta>`，Clip 拖拽完成回调 |

### TrackView

单条轨道，持有有序 `List<ClipView>`（按 `StartFrame` 升序），统一处理鼠标命中测试、拖拽和选中。

- `TrackFlag.ClipMixable` — 允许 Clip 重叠（OverlapDrawElement 绘制重叠区）
- `TrackFlag.ClipMovable` — 允许拖拽移动 Clip

### ClipView

纯显示元素，`PickingMode.Ignore`，不处理任何输入事件，由 TrackView 统一管理交互。

### OverlapDrawElement

`ImmediateModeElement`，在 TrackView 坐标系内用对角线 + 半透明填充绘制 Clip 重叠区域。

### TickMarkView / CursorView

刻度栏点击触发 `FrameIndexChangeEvent`；`CursorView` 显示当前帧位置指针。

### InsertIndicatorElement

轨道拖拽重排时显示的插入位置指示线。

---

## 事件

所有事件均继承 `EventBase<T>`，通过 UIToolkit 事件系统向上冒泡。

### Timeline 内部事件（`Timeline/Event/`）

| 事件类 | 触发时机 | 携带数据 |
|--------|----------|---------|
| `ClipMoveEvent` | Clip 拖拽结束，冒泡一次 | `ClipKey`（string）、`FrameDelta`（int，累计帧偏移） |
| `ClipSelectEvent` | Clip 被点击选中 | `ClipKey`（string） |
| `FrameIndexChangeEvent` | 点击刻度栏 | `FrameIndex`（int，目标帧号） |

### 通用 VisualElement 事件（`VisualElement/Event/`）

| 事件类 | 说明 |
|--------|------|
| `PlayButtonChangeEvent` | `PlayButtonsView` 播放/暂停/停止按钮状态变更 |
| `PropertyChangeEvent` | 属性值发生变化（编辑器通用） |
| `RegisterUndoEvent` | 请求向 Unity 注册 Undo 操作 |

---

## 快速接入

```csharp
using Timeline;
using UnityEngine.UIElements;

// 创建时间轴
var timeline = new TimelineView();
timeline.FrameWidth = 12f;
timeline.AutoHeight = true;
rootVisualElement.Add(timeline);

// 监听 Clip 移动
timeline.OnClipMoved = (clipKey, delta) =>
{
    // 更新数据层，clipKey 为业务标识，delta 为帧偏移
};

// 监听 Clip 选中（在父容器监听冒泡事件）
timeline.RegisterCallback<ClipSelectEvent>(evt =>
{
    Debug.Log($"选中 Clip: {evt.ClipKey}");
});

// 监听帧指针点击
timeline.RegisterCallback<FrameIndexChangeEvent>(evt =>
{
    Debug.Log($"跳转到帧: {evt.FrameIndex}");
});
```
