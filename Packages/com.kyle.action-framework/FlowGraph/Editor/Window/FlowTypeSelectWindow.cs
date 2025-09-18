using System;
using System.Collections.Generic;
using System.Reflection;

namespace Flow.EditorView
{
    public abstract class FlowTypeSelectWindow : TypeSelectWindow
    {
        protected readonly List<Type> types = new List<Type>();
        protected override IEnumerable<Type> GetTypes()
        {
            return types;
        }

        public void SetTags(string[] tags)
        {
            types.Clear();
            foreach (var item in tags)
            {
                var types = FlowNodeTypeCollector.GetNodeTypes(item);
                if (types != null)
                {
                    this.types.AddRange(types);
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

    }
}
