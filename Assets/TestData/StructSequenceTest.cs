using System;
using UnityEngine;

namespace TestNamespace
{
    // ── 测试用 struct ──

    public struct DamageEvent : IUnmanagedStruct
    {
        public int targetId;       // offset 0, 4B
        public double damage;      // offset 4, 8B
        public string skillName;   // offset 12, 4B (ref index)
    }
    // MessageSize = 4(header) + 4 + 8 + 4 = 20

    public struct MoveEvent : IUnmanagedStruct
    {
        public int entityId;       // offset 0, 4B
        public float x;            // offset 4, 4B
        public float y;            // offset 8, 4B
        public float z;            // offset 12, 4B
    }
    // MessageSize = 4(header) + 4 + 4 + 4 + 4 = 20

    public struct SpawnEvent : IUnmanagedStruct
    {
        public int entityId;       // offset 0, 4B
        public string prefabName;  // offset 4, 4B (ref index)
        public int[] tags;         // offset 8, 4B (ref index)
    }
    // MessageSize = 4(header) + 4 + 4 + 4 = 16

    // ── 类型索引（模拟生成） ──

    public static class TestStructTypeIndex
    {
        public const int DamageEvent = 0;
        public const int MoveEvent = 1;
        public const int SpawnEvent = 2;
        public const int Max = 3;
    }

    // ── 手写 Write/Read（模拟代码生成器产出） ──

    public static unsafe class TestStructIO
    {
        // DamageEvent: MessageSize = 20
        public static void Write_DamageEvent(ref DamageEvent data, InternalSequence block)
        {
            byte* ptr = block.TryAlloc(20);
            *(int*)(ptr + 0) = TestStructTypeIndex.DamageEvent;
            *(int*)(ptr + 4) = data.targetId;
            *(double*)(ptr + 8) = data.damage;
            *(int*)(ptr + 16) = block.WriteRef(data.skillName);
            block.IncrementMessageCount();
        }

        public static DamageEvent Read_DamageEvent(InternalSequence block)
        {
            byte* ptr = block.AllocRead(16); // payload only, header already consumed
            DamageEvent data;
            data.targetId = *(int*)(ptr + 0);
            data.damage = *(double*)(ptr + 4);
            data.skillName = (string)block.GetRef(*(int*)(ptr + 12));
            return data;
        }

        // MoveEvent: MessageSize = 20
        public static void Write_MoveEvent(ref MoveEvent data, InternalSequence block)
        {
            byte* ptr = block.TryAlloc(20);
            *(int*)(ptr + 0) = TestStructTypeIndex.MoveEvent;
            *(int*)(ptr + 4) = data.entityId;
            *(float*)(ptr + 8) = data.x;
            *(float*)(ptr + 12) = data.y;
            *(float*)(ptr + 16) = data.z;
            block.IncrementMessageCount();
        }

        public static MoveEvent Read_MoveEvent(InternalSequence block)
        {
            byte* ptr = block.AllocRead(16);
            MoveEvent data;
            data.entityId = *(int*)(ptr + 0);
            data.x = *(float*)(ptr + 4);
            data.y = *(float*)(ptr + 8);
            data.z = *(float*)(ptr + 12);
            return data;
        }

        // SpawnEvent: MessageSize = 16
        public static void Write_SpawnEvent(ref SpawnEvent data, InternalSequence block)
        {
            byte* ptr = block.TryAlloc(16);
            *(int*)(ptr + 0) = TestStructTypeIndex.SpawnEvent;
            *(int*)(ptr + 4) = data.entityId;
            *(int*)(ptr + 8) = block.WriteRef(data.prefabName);
            *(int*)(ptr + 12) = block.WriteRef(data.tags);
            block.IncrementMessageCount();
        }

        public static SpawnEvent Read_SpawnEvent(InternalSequence block)
        {
            byte* ptr = block.AllocRead(12);
            SpawnEvent data;
            data.entityId = *(int*)(ptr + 0);
            data.prefabName = (string)block.GetRef(*(int*)(ptr + 4));
            data.tags = (int[])block.GetRef(*(int*)(ptr + 8));
            return data;
        }
    }

    // ── Push 封装（模拟 Push<T> 的生成代码） ──

    public static class TestStructPush
    {
        public static void Push(StructSequence seq, DamageEvent data)
        {
            var block = seq.AllocMessage(20);
            TestStructIO.Write_DamageEvent(ref data, block);
        }

        public static void Push(StructSequence seq, MoveEvent data)
        {
            var block = seq.AllocMessage(20);
            TestStructIO.Write_MoveEvent(ref data, block);
        }

        public static void Push(StructSequence seq, SpawnEvent data)
        {
            var block = seq.AllocMessage(16);
            TestStructIO.Write_SpawnEvent(ref data, block);
        }
    }

    // ── 测试入口 ──

    public static class StructSequenceTest
    {
        public static void RunAll()
        {
            TestBasicPushConsume();
            TestMultipleTypes();
            TestNullReference();
            TestMultiBlock();
            TestResetAndReuse();
            Debug.Log("[StructSequenceTest] All tests passed!");
        }

        static void Assert(bool condition, string msg)
        {
            if (!condition) throw new Exception($"Assertion failed: {msg}");
        }

        // 测试 1: 基本 Push → Consume
        static void TestBasicPushConsume()
        {
            var seq = new StructSequence();
            seq.Init(TestStructTypeIndex.Max);

            DamageEvent received = default;
            seq.RegisterHandler(TestStructTypeIndex.DamageEvent, block =>
            {
                received = TestStructIO.Read_DamageEvent(block);
            });

            TestStructPush.Push(seq, new DamageEvent
            {
                targetId = 42,
                damage = 99.5,
                skillName = "Fireball"
            });

            Assert(seq.TotalMessageCount == 1, "TotalMessageCount should be 1");
            seq.Consume();

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
            seq.Init(TestStructTypeIndex.Max);

            DamageEvent dmg = default;
            MoveEvent mov = default;
            int dmgCount = 0, movCount = 0;

            seq.RegisterHandler(TestStructTypeIndex.DamageEvent, block =>
            {
                dmg = TestStructIO.Read_DamageEvent(block);
                dmgCount++;
            });
            seq.RegisterHandler(TestStructTypeIndex.MoveEvent, block =>
            {
                mov = TestStructIO.Read_MoveEvent(block);
                movCount++;
            });

            TestStructPush.Push(seq, new DamageEvent { targetId = 1, damage = 10.0, skillName = "Slash" });
            TestStructPush.Push(seq, new MoveEvent { entityId = 2, x = 1f, y = 2f, z = 3f });
            TestStructPush.Push(seq, new DamageEvent { targetId = 3, damage = 20.0, skillName = "Arrow" });

            seq.Consume();

            Assert(dmgCount == 2, "dmgCount should be 2");
            Assert(movCount == 1, "movCount should be 1");
            Assert(dmg.targetId == 3, "last dmg targetId should be 3");
            Assert(mov.entityId == 2, "mov entityId should be 2");
            Assert(Math.Abs(mov.x - 1f) < 0.001f, "mov.x mismatch");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestMultipleTypes passed");
        }

        // 测试 3: null 引用字段
        static void TestNullReference()
        {
            var seq = new StructSequence();
            seq.Init(TestStructTypeIndex.Max);

            SpawnEvent received = default;
            seq.RegisterHandler(TestStructTypeIndex.SpawnEvent, block =>
            {
                received = TestStructIO.Read_SpawnEvent(block);
            });

            TestStructPush.Push(seq, new SpawnEvent
            {
                entityId = 100,
                prefabName = null,
                tags = null
            });

            seq.Consume();

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
            seq.Init(TestStructTypeIndex.Max);

            int consumeCount = 0;
            seq.RegisterHandler(TestStructTypeIndex.MoveEvent, block =>
            {
                var data = TestStructIO.Read_MoveEvent(block);
                Assert(data.entityId == consumeCount, $"multi-block entityId mismatch at {consumeCount}");
                consumeCount++;
            });

            // MoveEvent MessageSize = 20, block = 4096B → ~204 messages per block
            int totalMessages = 1000;
            for (int i = 0; i < totalMessages; i++)
            {
                TestStructPush.Push(seq, new MoveEvent
                {
                    entityId = i,
                    x = i * 0.1f,
                    y = i * 0.2f,
                    z = i * 0.3f
                });
            }

            Assert(seq.TotalMessageCount == totalMessages, "TotalMessageCount mismatch");
            seq.Consume();
            Assert(consumeCount == totalMessages, $"consumeCount should be {totalMessages}, got {consumeCount}");

            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestMultiBlock passed");
        }

        // 测试 5: Reset 后复用
        static void TestResetAndReuse()
        {
            var seq = new StructSequence();
            seq.Init(TestStructTypeIndex.Max);

            int consumeCount = 0;
            seq.RegisterHandler(TestStructTypeIndex.DamageEvent, block =>
            {
                var data = TestStructIO.Read_DamageEvent(block);
                consumeCount++;
            });

            // 第一轮
            for (int i = 0; i < 100; i++)
            {
                TestStructPush.Push(seq, new DamageEvent { targetId = i, damage = i, skillName = $"skill_{i}" });
            }
            seq.Consume();
            Assert(consumeCount == 100, "round 1 consumeCount should be 100");

            seq.Reset();
            Assert(seq.TotalMessageCount == 0, "TotalMessageCount should be 0 after reset");

            // 第二轮：验证 Reset 后仍可正常使用
            consumeCount = 0;
            for (int i = 0; i < 50; i++)
            {
                TestStructPush.Push(seq, new DamageEvent { targetId = i, damage = i * 2, skillName = null });
            }
            seq.Consume();
            Assert(consumeCount == 50, "round 2 consumeCount should be 50");

            // 第一轮的引用应仍有效（消费者已持有）
            seq.Dispose();
            Debug.Log("[StructSequenceTest] TestResetAndReuse passed");
        }
    }
}
