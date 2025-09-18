using CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
namespace Flow.EditorView
{
    public class FlowCodeGenSetting
    {
        public string Tag;
        public System.Type RuntimeContextType;
        public string Namespace;//生成的脚本命名空间
        public string NodeScriptRoot;//生成的节点脚本根目录，继承自TFlowNode<>
        public string RuntimeDataDefineFile;//生成的RuntimeData脚本文件，同一个Tag的节点共用一个文件
        public string ExecutorDefineFile;//生成的Executor定义脚本文件，同一个Tag的节点共用一个文件
        public string ExecutorScriptRoot;//生成的Executor脚本根目录，一个节点一个类型，生成的文件可编辑
    }
    public class FlowCodeGenContext
    {
        private readonly HashSet<string> modifiedFiles = new HashSet<string>();
        private readonly List<FlowCodeGenSetting> settings = new List<FlowCodeGenSetting>();

        public void AddSetting(FlowCodeGenSetting setting)
        {
            if(settings.Exists(it => it.Tag == setting.Tag))
            {
                throw new System.Exception($"FlowCodeGenContext already has setting with tag {setting.Tag}");
            }
            settings.Add(setting);
        }

        public void Generate()
        {
            foreach (var setting in settings)
            {
                var nodeDatas = GetNodeDatas(setting);
                if (nodeDatas == null)
                    continue;
                string contextTypeName = GeneratorUtils.TypeToName(setting.RuntimeContextType, setting.Namespace);
                //生成RuntimeData定义脚本
                var runtimeDataDefine = FlowCodeGenHelper.WriteRuntimeDataDefine(nodeDatas, setting.Namespace);
                WriteToFile(runtimeDataDefine, setting.RuntimeDataDefineFile);
                //生成执行器定义脚本
                var executorDefine = FlowCodeGenHelper.WriteExecutorDefine(nodeDatas, contextTypeName, setting.Tag, setting.Namespace);
                WriteToFile(executorDefine, setting.ExecutorDefineFile);
                //生成节点脚本
                foreach (var item in nodeDatas)
                {
                    string nodeScriptFile = Path.Combine(setting.NodeScriptRoot, $"{item.NodeTypeName}.cs");
                    var nodeWriter = FlowCodeGenHelper.WriteNodeScript(item, setting.Namespace);
                    var nodeType = FlowNodeTypeCollector.DataTypeToNodeType(item.DataType, setting.Tag);
                    if (nodeType != null)
                    {
                        //节点存在，节点脚本文件不存在，说明被重命名了
                        MonoScript script = MonoScriptUtil.GetMonoScript(nodeType);
                        if(script != null)
                        {
                            string oldFile = AssetDatabase.GetAssetPath(script);
                            if (!string.Equals(oldFile, nodeScriptFile, System.StringComparison.OrdinalIgnoreCase))
                            {
                                //重命名
                                AssetDatabase.RenameAsset(oldFile, nodeScriptFile);
                            }
                        }
                    }
                    WriteToFile(nodeWriter, nodeScriptFile);
                }
                //生成执行器脚本
                foreach (var item in nodeDatas)
                {
                    string executorFile = Path.Combine(setting.ExecutorScriptRoot, $"{item.NodeTypeName}.cs");
                    if (File.Exists(executorFile))
                        continue;
                    var executorWriter = FlowCodeGenHelper.WriteExecutor(item, contextTypeName, setting.Namespace);
                    WriteToFile(executorWriter, executorFile);
                }
            }


            //刷新脚本
            if(modifiedFiles.Count > 0)
            {
                AssetDatabase.Refresh();
                modifiedFiles.Clear();
            }
        }

        private List<FlowNodeCodeGenData> GetNodeDatas(FlowCodeGenSetting setting)
        {
            var types = FlowNodeTypeCollector.GetDataTypes(setting.Tag);
            if (types == null || types.Count == 0)
                return null;
            return types.Select(it => ToNodeData(setting, it)).ToList();
        }

        private FlowNodeCodeGenData ToNodeData(FlowCodeGenSetting setting, System.Type type)
        {
            FlowNodeCodeGenData data = new FlowNodeCodeGenData();
            data.DataType = type;
            data.DataTypeName = GeneratorUtils.TypeToName(type, setting.Namespace);
            data.RuntimeDataTypeName = FlowCodeGenHelper.DataTypeToRuntimeDataTypeName(type);
            data.NodeTypeName = FlowCodeGenHelper.DataTypeToNodeTypeName(type);
            data.ExecutorTypeName = FlowCodeGenHelper.DataTypeToExecutorTypeName(type);
            data.IsUpdateable = typeof(IFlowUpdateable).IsAssignableFrom(type);
            data.IsConditional = typeof(IFlowConditionable).IsAssignableFrom(type);
            data.IsDynamicOutput = typeof(IFlowDynamicOutputable).IsAssignableFrom(type);
            data.IsPureDataNode = typeof(IFlowDataProvider).IsAssignableFrom(type) && !typeof(IFlowInputable).IsAssignableFrom(type);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            data.InputFields = fields.Where((f) => f.IsDefined(typeof(InputableAttribute), true)).ToArray();
            return data;
        }

        private void WriteToFile(CSharpCodeWriter writer, string filePath)
        {
            FileUtil.CheckDirectory(filePath);
            string content = writer.ToString();
            if(FileUtil.WriteFile(filePath, content))
            {
                modifiedFiles.Add(filePath);
            }
        }

    }
}