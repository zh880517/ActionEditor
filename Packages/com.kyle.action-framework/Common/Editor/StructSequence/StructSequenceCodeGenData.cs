using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeGen.StructSequence
{
    // 一个 Catalog（由一个 StructSequenceCatalogAttribute 子类定义）的所有信息
    public class SSCatalogData
    {
        // Catalog Attribute 的具体类型（StructSequenceCatalogAttribute 的子类）
        public Type AttributeType;
        // 生成代码的命名空间
        public string NameSpace;
        // 生成文件的输出目录路径
        public string GeneratePath;
        // 生成的静态类名，例如 "GameEventStructSequenceRegister"
        public string GenClassName;
        // 该 Catalog 下收集到的所有结构体
        public readonly List<SSStructData> Structs = new List<SSStructData>();
    }

    // 一个结构体类型的分析数据
    public class SSStructData
    {
        // 结构体的 System.Type
        public Type Type;
        // 是否是 unmanaged 类型（所有字段递归均为值类型且无引用）
        public bool IsUnmanaged;
        // 结构体的字段列表（仅 non-unmanaged struct 填充 public instance 字段）
        public readonly List<SSFieldData> Fields = new List<SSFieldData>();

        public string TypeName => Type.Name;
    }

    // 一个字段的分析数据
    public class SSFieldData
    {
        // 字段的反射信息
        public FieldInfo Field;
        // 该字段是否是 unmanaged 类型（可以直接用指针读写）
        public bool IsUnmanaged;
        // 字段在 payload 中的紧凑字节偏移（无对齐填充，由 BuildOffsets 计算）
        public int ByteOffset;
        // 字段的 unmanaged 字节大小（IsUnmanaged 为 true 时有效）
        public int UnmanagedSize;

        public string FieldName => Field.Name;
        public Type FieldType => Field.FieldType;
    }
}
