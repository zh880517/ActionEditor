namespace VECS
{
    public interface IViewSystem
    {
    }
    //初始化，所有system创建完成后统一调用，只调用一次
    public interface IInitializeSystem : IViewSystem
    {
        void OnInitialize();
    }

    //Update调用
    public interface IUpdateSystem : IViewSystem
    {
        void OnUpdate();
    }
    //LateUpdate调用
    public interface ILateUpdateSystem : IViewSystem
    {
        void OnLateUpdate();
    }
    //ILateExecuteSystem只够
    public interface ICleanupSystem : IViewSystem
    {
        void OnCleanup();
    }
    //销毁时调用
    public interface ITearDownSystem : IViewSystem
    {
        void OnTearDown();
    }
}
