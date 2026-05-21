# CSharpCodeWriter 代码生成器

路径：`Common/Editor/CSharpCodeWriter.cs`，命名空间 `CodeGen`

流式 C# 源码生成工具，自动管理缩进层级和代码块结构，供 DataVisit、FlowGraph 等模块的代码生成器使用。

---

## 核心成员

| 成员 | 说明 |
|------|------|
| `Scop` | 带大括号的代码块作用域（`IDisposable`）；构造时写 `{`，Dispose 时写 `}`；可选末尾追加 `;` |
| `TableScope` | 纯缩进作用域（无大括号），适合 `if`/`for` 单行语句的缩进 |
| `WriteLine(line)` | 写入一行带当前缩进的内容，自动换行 |
| `Append(text)` | 在当前行末尾追加文本，不换行（用于行内拼接） |
| `ToString()` | 输出完整的代码字符串 |

---

## 用法示例

```csharp
using CodeGen;

var w = new CSharpCodeWriter();

w.WriteLine("// 自动生成代码，请勿手动修改");
w.WriteLine("using System;");
w.WriteLine("");

using (new CSharpCodeWriter.Scop(w, "namespace MyGame"))
{
    using (new CSharpCodeWriter.Scop(w, "public static class GeneratedVisit"))
    {
        using (new CSharpCodeWriter.Scop(w, "public static void Register()"))
        {
            w.WriteLine("TypeVisit<MyStruct>.Pack = PackMyStruct;");
            w.WriteLine("TypeVisit<MyStruct>.Unpack = UnpackMyStruct;");
        }

        using (new CSharpCodeWriter.Scop(w, "private static void PackMyStruct(IWriter writer, MyStruct value)"))
        {
            w.WriteLine("writer.WriteInt(value.Id);");
            w.WriteLine("writer.WriteFloat(value.Speed);");
        }
    }
}

string code = w.ToString();
System.IO.File.WriteAllText("Assets/Generated/GeneratedVisit.cs", code);
```

输出：
```csharp
// 自动生成代码，请勿手动修改
using System;

namespace MyGame
{
    public static class GeneratedVisit
    {
        public static void Register()
        {
            TypeVisit<MyStruct>.Pack = PackMyStruct;
            TypeVisit<MyStruct>.Unpack = UnpackMyStruct;
        }
        private static void PackMyStruct(IWriter writer, MyStruct value)
        {
            writer.WriteInt(value.Id);
            writer.WriteFloat(value.Speed);
        }
    }
}
```

---

## Scop 末尾分号

某些语法（如 struct 定义）末尾需要 `;`，传入 `withSemicolon: true`：

```csharp
using (new CSharpCodeWriter.Scop(w, "public struct MyData", withSemicolon: true))
{
    w.WriteLine("public int Id;");
}
// 输出: }; 而非 }
```
