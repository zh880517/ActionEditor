using UnityEditor;
using TestNamespace;

public static class StructSequenceTestMenu
{
    [MenuItem("Tools/StructSequence Test")]
    public static void Run()
    {
        StructSequenceTest.RunAll();
    }
}
