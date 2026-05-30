using UnityEditor;

namespace EasyConfig.Editor
{
    public static class ConfigBinaryMenuItems
    {
        [MenuItem("Tools/EasyConfig/Generate Binary Registries")]
        public static void GenerateBinaryRegistries()
        {
            ConfigBinaryCodeGenerator.GenerateAll();
        }
    }
}
