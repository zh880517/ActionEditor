namespace Flow
{
    public interface IFlowDebugerProvider
    {
        FlowRuntimeDebuger Create(string scriptName, string key);
        void OnStop(FlowRuntimeDebuger debuger);
    }

    public static class FlowDebugContext
    {
        public static IFlowDebugerProvider Provider { get; set; }
        public static FlowRuntimeDebuger CreateDebuger(string scriptName, string key)
        {
            if (Provider != null)
                return Provider.Create(scriptName, key);
            return null;
        }

        public static void Stop(FlowRuntimeDebuger debuger)
        {
            if (debuger == null)
                return;
            Provider?.OnStop(debuger);
        }
    }
}
