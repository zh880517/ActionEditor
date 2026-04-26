namespace GOAP
{
    public static class ActionRunner<T> where T : struct, IActionData
    {
        public static TActionRunner<T> Runner { get; set; }
    }
}
