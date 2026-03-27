using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NamedAsset.Editor
{
    [CreateAssetMenu(fileName = "MaterialPackByShader", menuName = "NamedAsset/Policy/同Shader材质分包")]
    public class MaterialPackByShaderPolicy : DependencePackPolicy
    {
        public override List<string> PackDependence(AssetPackageBuilder builder, List<string> files)
        {
            List<string> remainFiles = new List<string>();
            foreach (var file in files)
            {
                if (file.EndsWith(".mat"))
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(file);
                    if (material != null && material.shader != null)
                    {
                        string bundleName = material.shader.name;
                        bundleName = bundleName.Replace('/', '_') + "_mat";
                        builder.PackDepenceFile(file, bundleName);
                        continue;
                    }
                }
                remainFiles.Add(file);
            }
            return remainFiles;
        }
    }
}
