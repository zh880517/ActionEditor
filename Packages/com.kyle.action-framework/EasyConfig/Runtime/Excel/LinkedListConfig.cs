namespace EasyConfig
{
    public class LinkedListConfig<TLinked, TPrimary> : ListConfig<TLinked>
        where TLinked : LinkedListConfig<TLinked, TPrimary>
        where TPrimary : IListConfig
    {
        public TPrimary Primary { get; internal set; }

        // 供生成代码在加载后恢复主配置关联。
        public static void LinkPrimary(TLinked linked, TPrimary primary)
        {
            linked.Primary = primary;
        }
    }
}
