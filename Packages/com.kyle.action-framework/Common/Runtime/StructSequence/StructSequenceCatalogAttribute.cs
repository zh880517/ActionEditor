using System;

// 用于标记结构体所属的 StructSequence 目录，使用时需继承此类后对具体结构体进行标记
[AttributeUsage(AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public abstract class StructSequenceCatalogAttribute : Attribute
{
    // 生成代码所在命名空间
    public abstract string NameSpace { get; }
    // 生成代码的输出目录路径
    public abstract string GeneratePath { get; }
}
