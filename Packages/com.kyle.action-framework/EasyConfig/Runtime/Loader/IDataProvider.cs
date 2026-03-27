namespace EasyConfig
{
    public interface IDataProvider
    {
        /// <summary>
        /// 加载指定类型配置的二进制数据
        /// </summary>
        /// <param name="type">配置类型</param>
        /// <param name="name">导出的配置文件名(不带文件后缀)</param>
        /// <returns></returns>
        byte[] LoadData(string type, string name);
    }
}
