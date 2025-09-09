using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flow.EditorView
{
    public class FlowTypeSelectWindow : TypeSelectWindow
    {
        private readonly List<Type> _types = new List<Type>();
        public FlowGraphView Current { get; set; }
        protected override IReadOnlyList<Type> GetTypes()
        {
            return _types;
        }

        public void SetTags(string[] tags)
        {
            _types.Clear();
            foreach (var item in tags)
            {
                var types = FlowNodeTypeCollector.GetNodeTypes(item);
                if (types != null)
                {
                    _types.AddRange(types);
                }
            }
        }

        protected override void OnSelect(Type type, Vector2 localMousePosition)
        {
            Current?.OnNodeCreate(type, localMousePosition);
        }
    }
}
