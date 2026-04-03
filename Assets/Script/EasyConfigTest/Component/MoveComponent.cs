using UnityEngine;
using EasyConfig;

namespace EasyConfigTest
{
    public class MoveComponentData : IConfigComponent
    {
        public float speed = 5f;
        public Vector3 direction;
    }

    [MovementAttribute]
    [Alias("移动组件")]
    public class MoveComponent : TConfigComponent<MoveComponentData>
    {
    }
}
