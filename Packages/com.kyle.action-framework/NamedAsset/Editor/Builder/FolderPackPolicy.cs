using System;
using System.Collections.Generic;
using UnityEngine;

namespace NamedAsset.Editor
{
    [CreateAssetMenu(fileName = "FolderPack", menuName = "NamedAsset/Policy/文件夹分包")]
    public class FolderPackPolicy : DependencePackPolicy
    {
        public List<string> FolderLimit = new List<string>();
        public List<string> FileExternLimit = new List<string>();
        public override List<string> PackDependence(AssetPackageBuilder builder, List<string> files)
        {
            List<string> remainFiles = new List<string>();
            foreach (var file in files)
            {
                if (FolderLimit.Count > 0 && !FolderLimit.Exists(it => IsInFolderLimit(file, it)))
                {
                    remainFiles.Add(file);
                    continue;
                }
                if (FileExternLimit.Count > 0)
                {
                    string fileExtern = System.IO.Path.GetExtension(file);
                    if (!FileExternLimit.Exists(it=>it.Equals(fileExtern, StringComparison.OrdinalIgnoreCase)))
                    {
                        remainFiles.Add(file);
                        continue;
                    }
                }
                string bundleName = GetFolderBundleName(file);
                builder.PackDepenceFile(file, bundleName);
            }
            return remainFiles;
        }

        private static bool IsInFolderLimit(string file, string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return true;

            string normalizedFile = NormalizePath(file).TrimEnd('/');
            string normalizedFolder = NormalizePath(folder).TrimEnd('/');
            return normalizedFile.Equals(normalizedFolder, StringComparison.OrdinalIgnoreCase)
                || normalizedFile.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
