using System.IO;
using System.Text;

public static class FileUtil
{
    private static readonly UTF8Encoding encoding = new UTF8Encoding(false);

    public static void WriteFile(string path, string content)
    {
        if (File.Exists(path))
        {
            if (File.ReadAllText(path, encoding) == content)
            {
                return;
            }
        }
        File.WriteAllText(path, content, encoding);
    }

    public static void ForceWrite(string path, string content)
    {
        CheckDirectory(path);
        File.WriteAllText(path, content, encoding);
    }

    public static void CheckDirectory(string path)
    {
        string folder = Path.GetDirectoryName(path);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

    }

    public static void WriteFileWithCreateFolder(string path, string content)
    {
        CheckDirectory(path);
        WriteFile(path, content);
    }
    //写入文件，如果写入的内容和已经存在的一致就不再写入，防止文件被修改导致Unity重新编译
    public static void WriteToFile(string filePath, string context)
    {
        if (File.Exists(filePath))
        {
            string existContext = File.ReadAllText(filePath, encoding);
            if (existContext == context)
                return;
        }
        else
        {
            CheckDirectory(filePath);
        }
        File.WriteAllText(filePath, context, encoding);
    }
}