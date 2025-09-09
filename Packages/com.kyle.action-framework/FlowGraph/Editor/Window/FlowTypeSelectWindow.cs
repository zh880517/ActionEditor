using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Flow.EditorView
{
    public class FlowTypeSelectWindow : TypeSelectWindow
    {
        private readonly List<Type> _types = new List<Type>();
        public FlowGraphView Current { get; set; }
        public Vector2 MousePosition { get; set; }
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

        protected override void GetTypeCreatePath(Type type, List<string> names)
        {
            var dataType = FlowNodeTypeUtil.GetNodeTypeInfo(type)?.DataType;
            if (dataType != null)
            {
                var pathAttr = dataType.GetCustomAttribute<FlowNodePathAttribute>();
                if (pathAttr != null && !string.IsNullOrEmpty(pathAttr.Path))
                {
                    var paths = pathAttr.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < paths.Length; ++i)
                    {
                        names.Add(paths[i]);
                    }
                }
            }
            else
            {
                base.GetTypeCreatePath(type, names);
            }

        }

        protected override AliasAttribute GetAlias(Type type)
        {
            var dataType = FlowNodeTypeUtil.GetNodeTypeInfo(type)?.DataType;
            if (dataType != null)
            {
                return dataType.GetCustomAttribute<AliasAttribute>();
            }
            return base.GetAlias(type);
        }

        protected override void OnSelect(Type type, Vector2 localMousePosition)
        {
            Current?.OnNodeCreate(type, MousePosition);
        }
    }
}
