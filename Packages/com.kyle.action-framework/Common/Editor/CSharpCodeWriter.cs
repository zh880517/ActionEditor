using System;
using System.Text;

namespace CodeGen
{
    public class CSharpCodeWriter
    {
        private int tabCount = 0;

        private string lineBreak = "\n";
        private StringBuilder stream = new StringBuilder();

        //带大括号的代码块
        public struct Scop : IDisposable
        {
            private readonly CSharpCodeWriter writer;
            private readonly bool withSemicolon;

            public Scop(CSharpCodeWriter writer, string lineContent, bool withSemicolon = false)
            {
                writer.WriteLine(lineContent);
                writer.BeginScope();
                this.writer = writer;
                this.withSemicolon = withSemicolon;
            }

            public Scop(CSharpCodeWriter writer, bool withSemicolon = false)
            {
                writer.BeginScope();
                this.writer = writer;
                this.withSemicolon = withSemicolon;
            }

            public void Dispose()
            {
                writer.EndScope();
                if (withSemicolon)
                {
                    writer.Append(";");
                }
            }

        }
        //缩进
        public struct TableScope : IDisposable
        {
            private readonly CSharpCodeWriter writer;
            public TableScope(CSharpCodeWriter writer, string lineContent)
            {
                writer.WriteLine(lineContent);
                this.writer = writer;
                writer.tabCount++;
            }
            public TableScope(CSharpCodeWriter writer)
            {
                this.writer = writer;
                writer.tabCount++;
            }

            public void Dispose()
            {
                writer.tabCount--;
            }
        }
        //命名空间
        public struct NameSpaceScop : IDisposable
        {
            private readonly CSharpCodeWriter writer;
            private bool valid;
            public NameSpaceScop(CSharpCodeWriter writer, string nameSpace)
            {
                valid = !string.IsNullOrEmpty(nameSpace);
                if (valid)
                {
                    if (writer.stream.Length > 0)
                    {
                        writer.NewLine();
                    }
                    writer.Append($"namespace {nameSpace}");
                    writer.BeginScope();
                }
                this.writer = writer;
            }
            public void Dispose()
            {
                if (valid)
                {
                    writer.EndScope();
                }
            }
        }

        public struct DoWhileScop : IDisposable
        {
            private readonly CSharpCodeWriter writer;
            private readonly bool trueLoop;
            public DoWhileScop(CSharpCodeWriter writer, bool trueLoop = true)
            {
                writer.WriteLine("do");
                writer.BeginScope();
                this.writer = writer;
                this.trueLoop = trueLoop;
            }

            public void Dispose()
            {
                writer.EndScope();
                if (trueLoop)
                    writer.Append("while(true);");
                else
                    writer.Append("while(false);");
            }
        }

        public CSharpCodeWriter(bool editorable = false)
        {
            lineBreak = editorable ? "\r\n" : "\n";
        }

        public CSharpCodeWriter BeginScope()
        {
            NewLine();
            stream.Append('{');
            tabCount += 1;
            return this;
        }

        public CSharpCodeWriter EndScope()
        {
            tabCount -= 1;
            NewLine();
            stream.Append('}');
            return this;
        }

        public CSharpCodeWriter NewLine()
        {
            stream.Append(lineBreak).Append(' ', tabCount * 4);
            return this;
        }

        public CSharpCodeWriter Append(string val)
        {
            stream.Append(val);
            return this;
        }

        public CSharpCodeWriter WriteLine(string val)
        {
            NewLine();
            stream.Append(val);
            return this;
        }
        public override string ToString()
        {
            return stream.ToString();
        }
    }
}
