
using DataVisit;
//Generate by tools : DataVisitCodeGenerator.GenerateAll()
namespace TestNamespace
{
    public partial class TestDataVisit
    {
        [VisitTypeIDCatalog(typeof(TestDataCatalogAttribute))]
        public enum TypeID
        {
            [VisitTypeTag(typeof(PlayerData))]
            PlayerData = 0x101,
            [VisitTypeTag(typeof(InventoryData))]
            InventoryData = 0x201,
            [VisitTypeTag(typeof(SkillBase))]
            SkillBase = 0x301,
            [VisitTypeTag(typeof(AttackSkill))]
            AttackSkill = 0x401,
            [VisitTypeTag(typeof(BuffSkill))]
            BuffSkill = 0x501,
            [VisitTypeTag(typeof(CharacterData))]
            CharacterData = 0x601,
        }
        private static bool isInit = false;
        public static void Init()
        {
            if(isInit)return;
            isInit = true;
            TypeVisitClassT<PlayerData>.Register((int)TypeID.PlayerData, VisitPlayerData);
            TypeVisitClassT<InventoryData>.Register((int)TypeID.InventoryData, VisitInventoryData);
            TypeVisitClassT<SkillBase>.Register((int)TypeID.SkillBase, VisitSkillBase);
            TypeVisitClassT<AttackSkill>.Register((int)TypeID.AttackSkill, VisitAttackSkill);
            TypeVisitClassT<BuffSkill>.Register((int)TypeID.BuffSkill, VisitBuffSkill);
            TypeVisitClassT<CharacterData>.Register((int)TypeID.CharacterData, VisitCharacterData);
        }
    }
}