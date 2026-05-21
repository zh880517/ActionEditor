using System;
using UnityEngine;

namespace TestNamespace
{
    // ── 测试用 struct ──

    public struct DamageEvent
    {
        public int targetId;       // offset 0, 4B
        public double damage;      // offset 4, 8B
        public string skillName;   // offset 12, 4B (ref index)
    }
    // PayloadSize = 4 + 8 + 4 = 16

    public struct MoveEvent
    {
        public int entityId;       // offset 0, 4B
        public float x;            // offset 4, 4B
        public float y;            // offset 8, 4B
        public float z;            // offset 12, 4B
    }
    // PayloadSize = 16 (blittable)

    public struct SpawnEvent
    {
        public int entityId;       // offset 0, 4B
        public string prefabName;  // offset 4, 4B (ref index)
        public int[] tags;         // offset 8, 4B (ref index)
    }
    // PayloadSize = 4 + 4 + 4 = 12

    // ── MessageID 常量 ──

    public static class TestMessageID
    {
        public const int DamageEvent = 0;
        public const int MoveEvent = 1;
        public const int SpawnEvent = 2;
        // 同一 struct 多个语义示例
        public const int DamageEvent_Crit = 3;
    }

    // ── 初始化 UnmanagedStructReadWrite 委托（模拟代码生成器产出） ──

    public static unsafe class TestStructIO
    {
        public static void RegisterAll()
        {
            // DamageEvent: 含引用字段，注册自定义委托
            UnmanagedStructReadWrite<DamageEvent>.Init(
                16,
                (InternalSequence block, byte* ptr, ref DamageEvent v) =>
                {
                    *(int*)(ptr + 0) = v.targetId;
                    *(double*)(ptr + 4) = v.damage;
                    *(int*)(ptr + 12) = block.WriteRef(v.skillName);
                },
                (InternalSequence block, byte* ptr) =>
                {
                    DamageEvent data;
                    data.targetId = *(int*)(ptr + 0);
                    data.damage = *(double*)(ptr + 4);
                    data.skillName = (string)block.GetRef(*(int*)(ptr + 12));
                    return data;
                }
            );

            // SpawnEvent: 含引用字段，注册自定义委托
            UnmanagedStructReadWrite<SpawnEvent>.Init(
                12,
                (InternalSequence block, byte* ptr, ref SpawnEvent v) =>
                {
                    *(int*)(ptr + 0) = v.entityId;
                    *(int*)(ptr + 4) = block.WriteRef(v.prefabName);
                    *(int*)(ptr + 8) = block.WriteRef(v.tags);
                },
                (InternalSequence block, byte* ptr) =>
                {
                    SpawnEvent data;
                    data.entityId = *(int*)(ptr + 0);
                    data.prefabName = (string)block.GetRef(*(int*)(ptr + 4));
                    data.tags = (int[])block.GetRef(*(int*)(ptr + 8));
                    return data;
                }
            );

            // MoveEvent: 纯值类型，无需注册，使用默认 sizeof(T) 路径
        }
    }

    // ── 测试入口 ──

    public static class StructSequenceTest
    {
        public static void RunAll()
        {
            TestStructIO.RegisterAll();
            TestBasicPushConsume();
            TestMultipleTypes();
            TestNullReference();
            TestMultiBlock();
            TestResetAndReuse();
            TestMultiMessageID();
            Debug.Log("[StructSequenceTest] All tests passed!");
        }

        static void Assert(bool condition, string msg)
        {
            if (!condition) throw new Exception($"Assertion failed: {msg}");
        }

        // 测试 1: 基本 Push → 读取 Meta
        static void TestBasicPushConsume()
        {
            var seq = new StructSequence();
            seq.Init();

            IStructSequenceWriter writer = seq;
            IStructSequenceReader reader = seq;

            var evt = new DamageEvent { targetId = 42, damage = 99.5, skillName = "Fireball" };
            writer.Push(TestMessageID.DamageEvent, ref evt);

            Assert(reader.Metas.Count == 1, "Metas.Count should be 1");

            var meta = reader.Metas[0];
            Assert(meta.MessageID == TestMessageID.DamageEvent, "MessageID mismatch");
            var received = reader.Read<DamageEvent>(meta);

            Assert(received.targetId == 42, "targetId mismatch");
            Assert(Math.Abs(received.damage - 99.5) < 0.001, "damage mismatch");
            Assert(received.skillName == "Fireball", "skillName mismatch");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestBasicPushConsume passed");
        }

        // 测试 2: 多类型消息混合
        static void TestMultipleTypes()
        {
            var seq = new StructSequence();
            seq.Init();

            IStructSequenceWriter writer = seq;
            IStructSequenceReader reader = seq;

            var dmg1 = new DamageEvent { targetId = 1, damage = 10.0, skillName = "Slash" };
            var mov = new MoveEvent { entityId = 2, x = 1f, y = 2f, z = 3f };
            var dmg2 = new DamageEvent { targetId = 3, damage = 20.0, skillName = "Arrow" };

            writer.Push(TestMessageID.DamageEvent, ref dmg1);
            writer.Push(TestMessageID.MoveEvent, ref mov);
            writer.Push(TestMessageID.DamageEvent, ref dmg2);

            Assert(reader.Metas.Count == 3, "Metas.Count should be 3");

            int dmgCount = 0, movCount = 0;
            DamageEvent lastDmg = default;
            MoveEvent lastMov = default;

            var metas = reader.Metas;
            for (int i = 0; i < metas.Count; i++)
            {
                var meta = metas[i];
                switch (meta.MessageID)
                {
                    case TestMessageID.DamageEvent:
                        lastDmg = reader.Read<DamageEvent>(meta);
                        dmgCount++;
                        break;
                    case TestMessageID.MoveEvent:
                        lastMov = reader.Read<MoveEvent>(meta);
                        movCount++;
                        break;
                }
            }

            Assert(dmgCount == 2, "dmgCount should be 2");
            Assert(movCount == 1, "movCount should be 1");
            Assert(lastDmg.targetId == 3, "last dmg targetId should be 3");
            Assert(lastMov.entityId == 2, "mov entityId should be 2");
            Assert(Math.Abs(lastMov.x - 1f) < 0.001f, "mov.x mismatch");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestMultipleTypes passed");
        }

        // 测试 3: null 引用字段
        static void TestNullReference()
        {
            var seq = new StructSequence();
            seq.Init();

            IStructSequenceWriter writer = seq;
            IStructSequenceReader reader = seq;

            var evt = new SpawnEvent { entityId = 100, prefabName = null, tags = null };
            writer.Push(TestMessageID.SpawnEvent, ref evt);

            var received = reader.Read<SpawnEvent>(reader.Metas[0]);

            Assert(received.entityId == 100, "entityId mismatch");
            Assert(received.prefabName == null, "prefabName should be null");
            Assert(received.tags == null, "tags should be null");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestNullReference passed");
        }

        // 测试 4: 大量消息触发多块链表
        static void TestMultiBlock()
        {
            var seq = new StructSequence();
            seq.Init();

            IStructSequenceWriter writer = seq;
            IStructSequenceReader reader = seq;

            // MoveEvent PayloadSize = 16, block = 4096B → 256 messages per block
            int totalMessages = 1000;
            for (int i = 0; i < totalMessages; i++)
            {
                var evt = new MoveEvent { entityId = i, x = i * 0.1f, y = i * 0.2f, z = i * 0.3f };
                writer.Push(TestMessageID.MoveEvent, ref evt);
            }

            Assert(reader.Metas.Count == totalMessages, "Metas.Count mismatch");

            var metas = reader.Metas;
            for (int i = 0; i < metas.Count; i++)
            {
                var data = reader.Read<MoveEvent>(metas[i]);
                Assert(data.entityId == i, $"multi-block entityId mismatch at {i}");
            }

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestMultiBlock passed");
        }

        // 测试 5: Reset 后复用
        static void TestResetAndReuse()
        {
            var seq = new StructSequence();
            seq.Init();

            IStructSequenceWriter writer = seq;
            IStructSequenceReader reader = seq;

            // 第一轮
            for (int i = 0; i < 100; i++)
            {
                var evt = new DamageEvent { targetId = i, damage = i, skillName = $"skill_{i}" };
                writer.Push(TestMessageID.DamageEvent, ref evt);
            }
            Assert(reader.Metas.Count == 100, "round 1 Metas.Count should be 100");

            seq.Reset();
            Assert(reader.Metas.Count == 0, "Metas.Count should be 0 after reset");

            // 第二轮
            for (int i = 0; i < 50; i++)
            {
                var evt = new DamageEvent { targetId = i, damage = i * 2, skillName = null };
                writer.Push(TestMessageID.DamageEvent, ref evt);
            }
            Assert(reader.Metas.Count == 50, "round 2 Metas.Count should be 50");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestResetAndReuse passed");
        }

        // 测试 6: 同一 struct 多个 MessageID（不同语义）
        static void TestMultiMessageID()
        {
            var seq = new StructSequence();
            seq.Init();

            IStructSequenceWriter writer = seq;
            IStructSequenceReader reader = seq;

            var normal = new DamageEvent { targetId = 1, damage = 10.0, skillName = "Slash" };
            var crit = new DamageEvent { targetId = 2, damage = 50.0, skillName = "Crit" };

            writer.Push(TestMessageID.DamageEvent, ref normal);
            writer.Push(TestMessageID.DamageEvent_Crit, ref crit);

            Assert(reader.Metas.Count == 2, "Metas.Count should be 2");
            Assert(reader.Metas[0].MessageID == TestMessageID.DamageEvent, "first should be DamageEvent");
            Assert(reader.Metas[1].MessageID == TestMessageID.DamageEvent_Crit, "second should be DamageEvent_Crit");

            var r0 = reader.Read<DamageEvent>(reader.Metas[0]);
            var r1 = reader.Read<DamageEvent>(reader.Metas[1]);
            Assert(r0.targetId == 1, "normal targetId mismatch");
            Assert(r1.targetId == 2, "crit targetId mismatch");
            Assert(Math.Abs(r1.damage - 50.0) < 0.001, "crit damage mismatch");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestMultiMessageID passed");
        }
    }
}
