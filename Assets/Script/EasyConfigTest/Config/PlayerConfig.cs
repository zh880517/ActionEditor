using EasyConfig;

namespace EasyConfigTest
{
    [EntityTag(typeof(MovementAttribute), typeof(CombatAttribute))]
    public class PlayerConfig : EntityConfig
    {
    }
}
