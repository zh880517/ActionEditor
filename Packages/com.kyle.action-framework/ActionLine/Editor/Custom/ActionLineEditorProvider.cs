
using System.Collections.Generic;
using UnityEngine;

namespace ActionLine.EditorView
{
    public class ActionLineEditorProvider
    {
        public virtual ActionLineEditorContext CreateEditorContext(ActionLineAsset asset)
        {
            var context = ScriptableObject.CreateInstance<ActionLineEditorContext>();
            context.SetTarget(asset);
            context.hideFlags = HideFlags.DontSave;
            return context;
        }

        public virtual ActionLinePreviewContext CreatePreview(ActionLineAsset target, PreviewResourceContext resourceContext)
        {
            var simulate = new ActionLinePreviewContext();
            simulate.Target = target;
            simulate.ResourceContext = resourceContext;
            return simulate;
        }

        public virtual PreviewResourceContext CreateResourceContext()
        {
            var context = ScriptableObject.CreateInstance<PreviewResourceContext>();
            context.hideFlags = HideFlags.DontSave;
            return context;
        }
        private static Dictionary<System.Type, ActionLineEditorProvider> providers;

        private static ActionLineEditorProvider CreateProvider(System.Type assetType)
        {
            var types = TypeWithAttributeCollector<ActionLineEditorProvider, CustomEditorProviderAttribute>.Types;
            foreach (var kv in types)
            {
                if (kv.Value.AssetType == assetType)
                {
                    return (ActionLineEditorProvider)System.Activator.CreateInstance(kv.Key);
                }
            }
            return new ActionLineEditorProvider();
        }

        public static ActionLineEditorProvider GetProvider(System.Type assetType)
        {
            if (providers == null)
            {
                providers = new Dictionary<System.Type, ActionLineEditorProvider>();
            }
            if (!providers.TryGetValue(assetType, out var provider))
            {
                provider = CreateProvider(assetType);
                providers[assetType] = provider;
            }
            return provider;
        }

        public static ActionLineEditorContext
    }
}
