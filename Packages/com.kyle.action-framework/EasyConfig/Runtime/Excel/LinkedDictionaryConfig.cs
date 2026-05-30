namespace EasyConfig
{
    public class LinkedDictionaryConfig<TKey, TLinked, TPrimary> : DictionaryConfig<TKey, TLinked>
        where TKey : struct
        where TLinked : LinkedDictionaryConfig<TKey, TLinked, TPrimary>
        where TPrimary : IDictionaryConfig
    {
        public TPrimary Primary { get; internal set; }

        // 供生成代码在加载后恢复主配置关联。
        public static void LinkPrimary(TLinked linked, TPrimary primary)
        {
            linked.Primary = primary;
        }
    }
}
