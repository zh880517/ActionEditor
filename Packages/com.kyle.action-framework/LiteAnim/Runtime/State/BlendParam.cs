using System.Collections.Generic;
using UnityEngine;

namespace LiteAnim
{
    [System.Serializable]
    public class BlendParam
    {
        [System.Serializable]
        public struct ParamInfo
        {
            public string Name;
            public float Value;
        }
        [SerializeField]
        protected List<ParamInfo> paramInfos = new List<ParamInfo>();

        public void SetParam(string name, float value)
        {
            for (int i = 0; i < paramInfos.Count; i++)
            {
                var p = paramInfos[i];
                if (p.Name == name)
                {
                    p.Value = value;
                    paramInfos[i] = p;
                    return;
                }
            }
            paramInfos.Add(new ParamInfo { Name = name, Value = value });
        }

        public float GetParam(string name)
        {
            for (int i = 0; i < paramInfos.Count; i++)
            {
                var p = paramInfos[i];
                if (p.Name == name)
                {
                    return p.Value;
                }
            }
            return 0;
        }
    }
}
