using UnityEngine.UIElements;

namespace GOAP.EditorView
{
    // 数据变更事件：子元素数据发生修改时向上冒泡，父层监听后执行持久化
    public class DataChangedEvent : EventBase<DataChangedEvent> { }

    // 删除请求事件：卡片点击删除按钮时向上冒泡，父层监听后执行移除逻辑
    public class DeleteRequestEvent : EventBase<DeleteRequestEvent> { }
}
