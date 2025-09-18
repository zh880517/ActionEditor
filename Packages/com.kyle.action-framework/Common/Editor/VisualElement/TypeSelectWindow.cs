using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


public abstract class TypeSelectWindow : ScriptableObject, ISearchWindowProvider
{
    internal struct TypeData
    {
        public string Name;
        public System.Type Type;
    }
    internal class TypeTree
    {
        public string Name;
        public List<TypeTree> Children = new List<TypeTree>();
        public List<TypeData> Types = new List<TypeData>();

        public TypeTree GetChild(string name)
        {
            var child = Children.Find(it => it.Name == name);
            if (child == null)
            {
                child = new TypeTree() { Name = name };
                Children.Add(child);
            }
            return child;
        }
    }

    [SerializeField]
    private Texture2D icon;
    protected virtual string RootName => "选择类型";
    protected abstract IEnumerable<System.Type> GetTypes();

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>();
        TypeTree rootTree = new TypeTree() { Name = RootName };
        List<string> tmpList = new List<string>();
        var types = GetTypes();
        if (types != null)
        {
            foreach (var type in types)
            {
                if(type.IsDefined(typeof(HiddenInTypeSelectAttribute)))
                    continue;
                var dpName = GetAlias(type);
                string name = dpName == null ? ObjectNames.NicifyVariableName(type.Name) : dpName.Name;
                tmpList.Clear();
                GetTypeCreatePath(type, tmpList);
                var root = rootTree;
                for (int i = tmpList.Count - 1; i >= 0; --i)
                {
                    root = rootTree.GetChild(tmpList[i]);
                }
                root.Types.Add(new TypeData { Name = name, Type = type });
            }
            BuildTree(rootTree, 0, tree);
        }

        return tree;
    }

    protected virtual AliasAttribute GetAlias(System.Type type)
    {
        return type.GetCustomAttribute<AliasAttribute>(false);
    }

    protected virtual void GetTypeCreatePath(System.Type type, List<string> names)
    {
        var catalogType = type;
        while (catalogType != null)
        {
            var catalog = catalogType.GetCustomAttribute<TypeCatalogAttribute>(false);
            if (catalog != null && !string.IsNullOrEmpty(catalog.Name))
            {
                names.Add(catalog.Name);
            }
            catalogType = catalogType.BaseType;
        }
    }

    private void BuildTree(TypeTree tree, int level, List<SearchTreeEntry> entries)
    {
        entries.Add(new SearchTreeGroupEntry(new GUIContent(tree.Name), level));

        var sortChildren = tree.Children.OrderBy(it => it.Name);
        foreach (var child in sortChildren)
        {
            BuildTree(child, level + 1, entries);
        }

        var sortTypes = tree.Types.OrderBy(it => it.Name);
        foreach (var t in sortTypes)
        {
            Texture2D typeIcon = MonoScriptUtil.GetTypeIcon(t.Type);
            if (typeIcon == null)
            {
                typeIcon = icon;
            }
            entries.Add(new SearchTreeEntry(new GUIContent(t.Name, typeIcon, t.Type.Name))
            {
                level = level + 1,
                userData = t.Type
            });
        }
    }
    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        OnSelect(SearchTreeEntry.userData as System.Type, context.screenMousePosition);
        return true;
    }

    protected abstract void OnSelect(System.Type type, Vector2 localMousePosition);

    private void OnDestroy()
    {
        if (icon != null)
        {
            DestroyImmediate(icon);
            icon = null;
        }
    }

    public void Show(Vector2 screenPosition,float requestedWidth = 0f, float requestedHeight = 0f)
    {
        SearchWindow.Open(new SearchWindowContext(screenPosition, requestedWidth, requestedHeight), this);
    }

    public static T Create<T>() where T : TypeSelectWindow
    {
        var w = CreateInstance<T>();
        w.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        w.icon = new Texture2D(1, 1);
        w.icon.SetPixel(0, 0, Color.clear);
        w.icon.Apply();
        return w;
    }
}