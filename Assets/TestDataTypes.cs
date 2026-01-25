using System.Collections.Generic;
using DataVisit;
public class TestDataCatalogAttribute : VisitCatalogAttribute
{
    public override byte TypeIDFieldIndex => 1;
    public override string NameSpace => "TestNamespace";
    public override string GeneratePath => "Assets/TestData/";
}
[TestDataCatalog]
public class PlayerData
{
    [VisitField(1, 0)] public int id;
    [VisitField(2, 0)] public string name;
    [VisitField(3, 0)] public int level;
    [VisitField(4, 0)] public float health;
}

[TestDataCatalog]
public class InventoryData
{
    [VisitField(1, 0)] public int ownerId;
    [VisitField(2, 0)] public List<int> itemIds;
    [VisitField(3, 0)] public Dictionary<int, int> itemCounts;
}

[TestDataCatalog]
public class SkillBase
{
    [VisitField(1, 0)] public int skillId;
    [VisitField(2, 0)] public string skillName;
}

[TestDataCatalog]
public class AttackSkill : SkillBase
{
    [VisitField(3, 3)] public int damage;
}

[TestDataCatalog]
public class BuffSkill : SkillBase
{
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
