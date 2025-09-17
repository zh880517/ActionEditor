using System;
using System.Collections.Generic;
using UnityEditor;

namespace Flow.EditorView
{
    public abstract class TFlowGraphEditorContext<T> : FlowGraphEditorContext where T : FlowGraph
    {
        private List<T> graphs;
        private readonly MonoScript monoScript = MonoScriptUtil.GetMonoScript(typeof(T));
        public override IReadOnlyList<FlowGraph> Graphs => TGraphs;

        public IReadOnlyList<T> TGraphs
        {
            get
            {
                if (graphs == null)
                {
                    graphs = new List<T>();
                    RefrshAssetList();
                }
                return graphs;
            }
        }

        public abstract string AssetRootPath { get; }
        public override Type GetGraphType()
        {
            return typeof(T);
        }
        public override void OnGraphCreate(FlowGraph graph)
        {
            if (graph is T tGraph)
            {
                if(graphs != null && !graphs.Contains(tGraph))
                    graphs.Add(tGraph);
                OnCreate(tGraph);
            }
        }
        public override void Export(FlowGraph graph)
        {
            if (graph is T tGraph)
            {
                OnExport(tGraph);
            }
        }

        protected virtual void OnCreate(T graph)
        {
            FlowGraphEditorUtil.CreateNode<EntryNode>(graph, UnityEngine.Vector2.zero);
        }
        protected abstract void OnExport(T graph);

        private void RefrshAssetList()
        {
            var files = ScriptObjectCollector.instance.GetAssets(monoScript, AssetRootPath);
            graphs.Clear();
            foreach (var item in files)
            {
                var obj = AssetDatabase.LoadAssetAtPath<T>(item);
                if (!obj)
                {
                    graphs.Add(obj);
                }
            }
        }

        protected override void OnContextInit()
        {
            ScriptObjectCollector.OnAssetChanged += OnAssstChanged;
        }
        protected override void OnContextDestroy()
        {
            ScriptObjectCollector.OnAssetChanged -= OnAssstChanged;
        }

        private void OnAssstChanged(MonoScript monoType)
        {
            if (monoType != monoScript)
                return;
            if(graphs != null)
                RefrshAssetList();
        }

    }
}
