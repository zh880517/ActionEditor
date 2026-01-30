using System;
using System.Collections.Generic;
using System.IO;
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
    [VisitField(1, 0)] public int damage;
}

[TestDataCatalog]
public class BuffSkill : SkillBase
{
    [VisitField(1, 0)] public float duration;
    [VisitField(2, 0)] public int buffType;
}

[TestDataCatalog]
public class CharacterData
{
    [VisitField(1, 1)] public int characterId;
    [VisitField(2, 2)] public string characterName;
    [VisitDynamicField(3, 3)] public SkillBase mainSkill;
    [VisitDynamicField(4, 4)] public List<SkillBase> skills;
}

public static class TestDataTypesRunner
{
    private const uint RootTag = 0;

    [UnityEditor.MenuItem("DataVisit/Test Data Types")]
    public static void RunAll()
    {
        EnsureVisitInit();
        TestReset();
        TestRawBitRoundtrip();
        TestSevenBitRoundtrip();
    }

    private static void EnsureVisitInit()
    {
        InnerTypeVisit.Register();
        TestNamespace.TestDataVisit.Init();
    }

    private static PlayerData CreatePlayerData()
    {
        return new PlayerData
        {
            id = 7,
            name = "Alice",
            level = 12,
            health = 98.5f
        };
    }

    private static InventoryData CreateInventoryData()
    {
        return new InventoryData
        {
            ownerId = 7,
            itemIds = new List<int> { 1001, 1002, 1003 },
            itemCounts = new Dictionary<int, int>
            {
                { 1001, 2 },
                { 1002, 5 },
                { 1003, 1 }
            }
        };
    }

    private static CharacterData CreateCharacterData()
    {
        return new CharacterData
        {
            characterId = 42,
            characterName = "Knight",
            mainSkill = new AttackSkill
            {
                skillId = 2001,
                skillName = "Slash",
                damage = 55
            },
            skills = new List<SkillBase>
            {
                new AttackSkill
                {
                    skillId = 2002,
                    skillName = "Pierce",
                    damage = 33
                },
                new BuffSkill
                {
                    skillId = 3001,
                    skillName = "Fortify",
                    duration = 12.5f,
                    buffType = 3
                }
            }
        };
    }

    private static void TestRawBitRoundtrip()
    {
        var player = CreatePlayerData();
        var inventory = CreateInventoryData();
        var character = CreateCharacterData();

        var playerOut = RoundtripRawBit(player);
        var inventoryOut = RoundtripRawBit(inventory);
        var characterOut = RoundtripRawBit(character);

        AssertEqual(player, playerOut, "RawBit PlayerData");
        AssertEqual(inventory, inventoryOut, "RawBit InventoryData");
        AssertEqual(character, characterOut, "RawBit CharacterData");
    }

    private static void TestSevenBitRoundtrip()
    {
        var player = CreatePlayerData();
        var inventory = CreateInventoryData();
        var character = CreateCharacterData();

        var playerOut = RoundtripSevenBit(player);
        var inventoryOut = RoundtripSevenBit(inventory);
        var characterOut = RoundtripSevenBit(character);

        AssertEqual(player, playerOut, "SevenBit PlayerData");
        AssertEqual(inventory, inventoryOut, "SevenBit InventoryData");
        AssertEqual(character, characterOut, "SevenBit CharacterData");
    }

    private static void TestReset()
    {
        var player = CreatePlayerData();
        var inventory = CreateInventoryData();
        var character = CreateCharacterData();

        ResetVisitier.Default.VisitClass(0, string.Empty, 0, ref player);
        ResetVisitier.Default.VisitClass(0, string.Empty, 0, ref inventory);
        ResetVisitier.Default.VisitClass(0, string.Empty, 0, ref character);

        AssertTrue(player.id == 0 && player.level == 0 && player.health == 0f, "Reset PlayerData value fields");
        AssertTrue(player.name == string.Empty, "Reset PlayerData name");

        AssertTrue(inventory.ownerId == 0, "Reset InventoryData ownerId");
        AssertTrue(inventory.itemIds != null && inventory.itemIds.Count == 0, "Reset InventoryData itemIds");
        AssertTrue(inventory.itemCounts != null && inventory.itemCounts.Count == 0, "Reset InventoryData itemCounts");

        AssertTrue(character.characterId == 0, "Reset CharacterData id");
        AssertTrue(character.characterName == string.Empty, "Reset CharacterData name");
        AssertTrue(character.mainSkill != null, "Reset CharacterData mainSkill not null");
        AssertTrue(character.skills != null && character.skills.Count == 0, "Reset CharacterData skills");

        AssertSkillReset(character.mainSkill, "Reset CharacterData mainSkill");
    }

    private static T RoundtripRawBit<T>(T value) where T : class, new()
    {
        using var memory = new MemoryStream();
        var pack = new RawBitPackVisitier(memory);
        pack.VisitClass(RootTag, string.Empty, 0, ref value);
        var data = memory.ToArray();
        var unpack = new RawBitUnPackVisitier(data);
        T result = null;
        unpack.VisitClass(RootTag, string.Empty, 0, ref result);
        return result;
    }

    private static T RoundtripSevenBit<T>(T value) where T : class, new()
    {
        using var memory = new MemoryStream();
        var pack = new SevenBitPackVisitier(memory);
        pack.VisitClass(RootTag, string.Empty, 0, ref value);
        var data = memory.ToArray();
        var unpack = new SevenBitUnPackVisitier(data);
        T result = null;
        unpack.VisitClass(RootTag, string.Empty, 0, ref result);
        return result;
    }

    private static void AssertEqual(PlayerData expected, PlayerData actual, string label)
    {
        AssertNotNull(actual, label);
        AssertTrue(expected.id == actual.id, $"{label} id");
        AssertTrue(expected.name == actual.name, $"{label} name");
        AssertTrue(expected.level == actual.level, $"{label} level");
        AssertTrue(Math.Abs(expected.health - actual.health) < 0.0001f, $"{label} health");
    }

    private static void AssertEqual(InventoryData expected, InventoryData actual, string label)
    {
        AssertNotNull(actual, label);
        AssertTrue(expected.ownerId == actual.ownerId, $"{label} ownerId");
        var expectedItemIds = expected.itemIds ?? throw new InvalidOperationException($"{label} itemIds expected null");
        var actualItemIds = actual.itemIds ?? throw new InvalidOperationException($"{label} itemIds actual null");
        AssertTrue(expectedItemIds.Count == actualItemIds.Count, $"{label} itemIds count");
        for (int i = 0; i < expectedItemIds.Count; i++)
        {
            AssertTrue(expectedItemIds[i] == actualItemIds[i], $"{label} itemIds[{i}]");
        }

        var expectedItemCounts = expected.itemCounts ?? throw new InvalidOperationException($"{label} itemCounts expected null");
        var actualItemCounts = actual.itemCounts ?? throw new InvalidOperationException($"{label} itemCounts actual null");
        AssertTrue(expectedItemCounts.Count == actualItemCounts.Count, $"{label} itemCounts count");
        foreach (var pair in expectedItemCounts)
        {
            AssertTrue(actualItemCounts.TryGetValue(pair.Key, out int value), $"{label} itemCounts key {pair.Key}");
            AssertTrue(value == pair.Value, $"{label} itemCounts value {pair.Key}");
        }
    }

    private static void AssertEqual(CharacterData expected, CharacterData actual, string label)
    {
        AssertNotNull(actual, label);
        AssertTrue(expected.characterId == actual.characterId, $"{label} characterId");
        AssertTrue(expected.characterName == actual.characterName, $"{label} characterName");

        AssertEqualSkill(expected.mainSkill, actual.mainSkill, $"{label} mainSkill");

        var expectedSkills = expected.skills ?? throw new InvalidOperationException($"{label} skills expected null");
        var actualSkills = actual.skills ?? throw new InvalidOperationException($"{label} skills actual null");
        AssertTrue(expectedSkills.Count == actualSkills.Count, $"{label} skills count");
        for (int i = 0; i < expectedSkills.Count; i++)
        {
            AssertEqualSkill(expectedSkills[i], actualSkills[i], $"{label} skills[{i}]");
        }
    }

    private static void AssertEqualSkill(SkillBase expected, SkillBase actual, string label)
    {
        if (expected == null)
        {
            AssertTrue(actual == null, $"{label} expected null");
            return;
        }
        AssertNotNull(actual, label);
        AssertTrue(expected.GetType() == actual.GetType(), $"{label} type");
        AssertTrue(expected.skillId == actual.skillId, $"{label} skillId");
        AssertTrue(expected.skillName == actual.skillName, $"{label} skillName");

        if (expected is AttackSkill expectedAttack && actual is AttackSkill actualAttack)
        {
            AssertTrue(expectedAttack.damage == actualAttack.damage, $"{label} damage");
            return;
        }

        if (expected is BuffSkill expectedBuff && actual is BuffSkill actualBuff)
        {
            AssertTrue(Math.Abs(expectedBuff.duration - actualBuff.duration) < 0.0001f, $"{label} duration");
            AssertTrue(expectedBuff.buffType == actualBuff.buffType, $"{label} buffType");
            return;
        }
    }

    private static void AssertSkillReset(SkillBase skill, string label)
    {
        AssertTrue(skill.skillId == 0, $"{label} skillId");
        AssertTrue(skill.skillName == string.Empty, $"{label} skillName");

        if (skill is AttackSkill attack)
        {
            AssertTrue(attack.damage == 0, $"{label} damage");
            return;
        }

        if (skill is BuffSkill buff)
        {
            AssertTrue(buff.duration == 0f, $"{label} duration");
            AssertTrue(buff.buffType == 0, $"{label} buffType");
        }
    }

    private static void AssertNotNull(object value, string label)
    {
        if (value == null)
            throw new InvalidOperationException($"{label} is null");
    }

    private static void AssertTrue(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Assert failed: {label}");
    }
}
