using System.Text;


public static class MD5Util
{
    private static readonly System.Security.Cryptography.MD5 _md5 = System.Security.Cryptography.MD5.Create();
    private static readonly StringBuilder _stringBuilder = new StringBuilder();

    public static string BytesToString(byte[] bytes)
    {
        _stringBuilder.Clear();
        foreach (byte b in bytes)
        {
            _stringBuilder.Append(b.ToString("x2"));
        }
        return _stringBuilder.ToString();
    }

    public static string ToMD5(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = _md5.ComputeHash(inputBytes);
        return BytesToString(hashBytes);
    }

    public static string ToMD5(byte[] inputBytes)
    {
        if (inputBytes == null || inputBytes.Length == 0)
        {
            return string.Empty;
        }
        byte[] hashBytes = _md5.ComputeHash(inputBytes);
        return BytesToString(hashBytes);
    }

    public static string ToMD5(System.IO.Stream stream)
    {
        if (stream == null || stream.Length == 0)
        {
            return string.Empty;
        }
        byte[] hashBytes = _md5.ComputeHash(stream);
        return BytesToString(hashBytes);
    }

    public static string ToMD5(System.IO.FileInfo file)
    {
        if (file == null || !file.Exists)
        {
            return string.Empty;
        }
        using (var stream = file.OpenRead())
        {
            return ToMD5(stream);
        }
    }
    public static string FileMD5(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return string.Empty;
        }
        using (var stream = System.IO.File.OpenRead(filePath))
        {
            return ToMD5(stream);
        }
    }
}