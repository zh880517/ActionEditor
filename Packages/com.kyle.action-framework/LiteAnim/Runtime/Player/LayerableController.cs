using UnityEngine.Animations;

namespace LiteAnim
{
    [System.Serializable]
    public class LayerableController : AnimController
    {
        private AnimationLayerMixerPlayable layerMixerPlayable;
        private int rootIndex = -1;
        protected override void OnInit()
        {
            layerMixerPlayable = AnimationLayerMixerPlayable.Create(graph.Graph, asset.Layers.Count);
            for (int i = 0; i < asset.Layers.Count; i++)
            {
                var layer = asset.Layers[i];
                layerMixerPlayable.SetLayerAdditive((uint)i, layer.Additive);
                if (layer.Mask)
                    layerMixerPlayable.SetLayerMaskFromAvatarMask((uint)i, layer.Mask);
            }
            rootIndex = graph.ConnectToRoot(layerMixerPlayable, 0);
        }

        protected override void OnWeightChanged()
        {
            graph.SetRootWeight(rootIndex, Weight);
        }
    }
}
