using DataVisit;
using System.Collections.Generic;

//主要提供给容器序列化和反序列化使用
public class TypeVisit<T>
{
    public delegate void Delegate(IVisitier visitier, uint tag, string name,uint flag, ref T value);
    public delegate T CreatorDelegate();
    public static Delegate VisitFunc;
    public static CreatorDelegate New = ()=> default;
    public static bool IsCustomStruct = false;//是否是自定义结构体，容器和泛型在处理时会有区别

    public static void Visit(IVisitier visitier, uint tag, string name,uint flag, ref T value)
    {
        if (VisitFunc != null)
        {
            VisitFunc.Invoke(visitier, tag, name, flag, ref value);
            return;
        }
        throw new System.Exception($"None visit define for type {typeof(T)}");
    }
}

public class DynamicTypeVisit<T> : TypeVisit<T> where T : class, new()
{
    private static readonly Dictionary<int, Delegate> idToVisits = new Dictionary<int, Delegate>();
    private static readonly Dictionary<System.Type, int> typeToIds = new Dictionary<System.Type, int>();

    public static int GetTypeId(T v)
    {
        if (v == null)
            return -1;
        var type = v.GetType();
        if (type == typeof(T))
            return 0;
        if (typeToIds.TryGetValue(type, out int id))
            return id;
        throw new System.Exception($"Type {type} not register in DynamicTypeVisit<{typeof(T)}>");
    }

    public static Delegate GetVisit(int typeId)
    {
        if(typeId == 0)
            return Visit;

        if (idToVisits.TryGetValue(typeId, out Delegate visit))
            return visit;
        throw new System.Exception($"TypeId {typeId} not register in DynamicTypeVisit<{typeof(T)}>");
    }

    public static void RegisterType<TChild>(int id) where TChild : class, T, new()
    {
        System.Type type = typeof(TChild);

        if (typeToIds.ContainsKey(type))
        {
            throw new System.Exception($"Type {type} already register in DynamicTypeVisit<{typeof(T)}>");
        }
        if (idToVisits.ContainsKey(id))
        {
            throw new System.Exception($"TypeId {id} already register in DynamicTypeVisit<{typeof(T)}>");
        }
        typeToIds[type] = id;
        static void func(IVisitier visitier, uint tag, string name,uint flag, ref T value)
        {
            var v = value as TChild;
            TypeVisit<TChild>.Visit(visitier, tag, name, flag, ref v);
            value = v;
        }
        idToVisits[id] = func;
    }
}
