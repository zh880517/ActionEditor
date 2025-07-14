using System.Collections.Generic;
using UnityEngine;

namespace Flow
{
    public class FlowNode : ScriptableObject
    {
        [HideInInspector]
        public FlowGraphAsset Graph;
    }
}
