using System;
using System.Collections.Generic;

namespace Flow.EditorView
{
    public abstract class FlowGraphEditorContext
    {
        #region Static 
        private static Dictionary<Type, FlowGraphEditorContext> contextCache;

        public static FlowGraphEditorContext GetContext(Type graphType)
        {
            if (contextCache == null)
            {
                contextCache = new Dictionary<Type, FlowGraphEditorContext>();
                foreach (var type in TypeCollector<FlowGraphEditorContext>.Types)
                {
                    var instance = Activator.CreateInstance(type) as FlowGraphEditorContext;
                    if (instance != null)
                    {
                        var targetType = instance.GetGraphType();
                        if (targetType != null && !contextCache.ContainsKey(targetType))
                        {
                            contextCache.Add(targetType, instance);
                        }
                    }
                }
            }
            contextCache.TryGetValue(graphType, out var context);
            if(context != null && !context.initialized)
            {
                context.initialized = true;
                context.OnContextInit();
            }
            return context;
        }

        internal static void Destroy()
        {
            if (contextCache != null)
            {
                foreach (var context in contextCache.Values)
                {
                    if (context.initialized)
                    {
                        context.OnContextDestroy();
                        context.initialized = false;
                    }
                }
                contextCache.Clear();
                contextCache = null;
            }
        }

        /*
        public static T LoadOrCreate<T>(string path) where T : FlowGraph
        {

        }
        */

        #endregion
        private bool initialized = false;
        public Type GetEditorWindowType()
        {
            return typeof(FlowGraphEditorWindow);
        }

        public abstract IReadOnlyList<FlowGraph> Graphs { get; }
        public abstract Type GetGraphType();
        public abstract void OnGraphCreate(FlowGraph graph);
        public abstract void Export(FlowGraph graph);
        protected abstract void OnContextInit();
        protected abstract void OnContextDestroy();
    }
}
