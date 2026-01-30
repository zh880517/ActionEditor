
using DataVisit;
//Generate by tools : DataVisitCodeGenerator.GenerateAll()
namespace TestNamespace
{
    public partial class TestDataVisit
    {
        private static void VisitPlayerData(IVisitier visitier, uint tag, string name, uint flag, ref PlayerData value)
        {
            visitier.Visit(1, nameof(value.id), 0, ref value.id);
            visitier.Visit(2, nameof(value.name), 0, ref value.name);
            visitier.Visit(3, nameof(value.level), 0, ref value.level);
            visitier.Visit(4, nameof(value.health), 0, ref value.health);
        }
        
        private static void VisitInventoryData(IVisitier visitier, uint tag, string name, uint flag, ref InventoryData value)
        {
            visitier.Visit(1, nameof(value.ownerId), 0, ref value.ownerId);
            visitier.VisitList(2, nameof(value.itemIds), 0, ref value.itemIds);
            visitier.VisitDictionary(3, nameof(value.itemCounts), 0, ref value.itemCounts);
        }
        
        private static void VisitSkillBase(IVisitier visitier, uint tag, string name, uint flag, ref SkillBase value)
        {
            visitier.Visit(1, nameof(value.skillId), 0, ref value.skillId);
            visitier.Visit(2, nameof(value.skillName), 0, ref value.skillName);
        }
        
        private static void VisitAttackSkill(IVisitier visitier, uint tag, string name, uint flag, ref AttackSkill value)
        {
            var _base = (SkillBase)value;
            visitier.VisitClass(0, "", 0, ref _base);
            visitier.Visit(1, nameof(value.damage), 0, ref value.damage);
        }
        
        private static void VisitBuffSkill(IVisitier visitier, uint tag, string name, uint flag, ref BuffSkill value)
        {
            var _base = (SkillBase)value;
            visitier.VisitClass(0, "", 0, ref _base);
            visitier.Visit(1, nameof(value.duration), 0, ref value.duration);
            visitier.Visit(2, nameof(value.buffType), 0, ref value.buffType);
        }
        
        private static void VisitCharacterData(IVisitier visitier, uint tag, string name, uint flag, ref CharacterData value)
        {
            visitier.Visit(1, nameof(value.characterId), 1, ref value.characterId);
            visitier.Visit(2, nameof(value.characterName), 2, ref value.characterName);
            visitier.VisitDynamicClass(3, nameof(value.mainSkill), 3, ref value.mainSkill);
            visitier.VisitDynamicList(4, nameof(value.skills), 4, ref value.skills);
        }
        
    }
}