using CodeGen;
using System.Collections.Generic;
using UnityEditor;
namespace Flow.EditorView
{
    public class FlowCodeGenSetting
    {
        public string Tag;
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
                var types = FlowNodeTypeCollector.GetNodeTypes(setting.Tag);
                if (types == null || types.Count == 0)
                    continue;

                //TODO: 生成RuntimeData定义脚本
                //TODO: 生成Executor定义脚本
                foreach (var item in types)
                {
                    //TODO: 生成节点脚本
                    //TODO: 生成执行器脚本
                }
            }
            RefrshFiles();
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


        private void RefrshFiles()
        {
            foreach (var item in modifiedFiles)
            {
                AssetDatabase.ImportAsset(item);
            }
            modifiedFiles.Clear();
        }
    }
}