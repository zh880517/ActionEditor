using System.Collections.Generic;
using DataVisit;

[TestDataCatalog]
public class PlayerData
{
    [VisitField(1, 1)] public int id;
    [VisitField(2, 2)] public string name;
    [VisitField(3, 3)] public int level;
    [VisitField(4, 4)] public float health;
}

[TestDataCatalog]
public class InventoryData
{
    [VisitField(1, 1)] public int ownerId;
    [VisitField(2, 2)] public List<int> itemIds;
    [VisitField(3, 3)] public Dictionary<int, int> itemCounts;
}

[TestDataCatalog]
[VisitDynamicType]
public class SkillBase
{
    [VisitField(1, 1)] public int skillId;
    [VisitField(2, 2)] public string skillName;
}

[TestDataCatalog]
public class AttackSkill : SkillBase
{
    public const int TYPE_ID = 1;
    [VisitField(3, 3)] public int damage;
}

[TestDataCatalog]
public class BuffSkill : SkillBase
{
    public const int TYPE_ID = 2;
    [VisitField(3, 3)] public float duration;
    [VisitField(4, 4)] public int buffType;
}

[TestDataCatalog]
public class CharacterData
{
    [VisitField(1, 1)] public int characterId;
    [VisitField(2, 2)] public string characterName;
    [VisitDynamicField(3, 3)] public SkillBase mainSkill;
    [VisitDynamicField(4, 4)] public List<SkillBase> skills;
}
