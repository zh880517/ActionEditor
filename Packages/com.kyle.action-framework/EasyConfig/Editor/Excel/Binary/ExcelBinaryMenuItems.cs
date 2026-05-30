using UnityEditor;

namespace EasyConfig.Editor
{
    public static class ExcelBinaryMenuItems
    {
        [MenuItem("Tools/EasyConfig/Generate Binary Registries")]
        public static void GenerateBinaryRegistries()
        {
            ExcelBinaryCodeGenerator.GenerateAll();
        }
    }
}
