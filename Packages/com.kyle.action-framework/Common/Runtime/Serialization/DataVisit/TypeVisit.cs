using DataVisit;

//主要提供给容器序列化和反序列化使用
public class TypeVisit<T>
{
    public delegate void Delegate(IVisitier visitier, uint tag, string name, bool require, ref T value);
    public delegate T CreatorDelegate();
    public static Delegate VisitFunc;
    public static CreatorDelegate New = ()=> default;
    public static bool IsCustomStruct = false;//是否是自定义结构体，容器和泛型在处理时会有区别

    public static void Visit(IVisitier visitier, uint tag, string name, bool require, ref T value)
    {
        if (VisitFunc != null)
        {
            VisitFunc.Invoke(visitier, tag, name, require, ref value);
            return;
        }
        throw new System.Exception($"No Visit function for type {typeof(T)}");
    }
}
