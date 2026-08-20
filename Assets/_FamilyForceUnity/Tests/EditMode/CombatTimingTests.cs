using FamilyForceUnity.AI;
using FamilyForceUnity.Combat;
using NUnit.Framework;
using UnityEngine;

namespace FamilyForceUnity.Tests
{
    public sealed class CombatTimingTests
    {
        [Test]
        public void MoveActiveWindowUsesStartupAndActiveTicks()
        {
            var move = ScriptableObject.CreateInstance<MoveDefinition>();
            move.Configure(MoveId.Punch, 5, 3, 8, 10, 4, Vector2.right);

            Assert.That(move.IsActiveAt(4), Is.False);
            Assert.That(move.IsActiveAt(5), Is.True);
            Assert.That(move.IsActiveAt(7), Is.True);
            Assert.That(move.IsActiveAt(8), Is.False);
            Assert.That(move.TotalTicks, Is.EqualTo(16));

            Object.DestroyImmediate(move);
        }

        [Test]
        public void AttackTokensNeverExceedCapacity()
        {
            var managerObject = new GameObject("Tokens");
            var manager = managerObject.AddComponent<AttackTokenManager>();
            manager.ConfigureCapacity(2);
            var first = new GameObject("First");
            var second = new GameObject("Second");
            var third = new GameObject("Third");

            Assert.That(manager.TryAcquire(first), Is.True);
            Assert.That(manager.TryAcquire(second), Is.True);
            Assert.That(manager.TryAcquire(third), Is.False);
            Assert.That(manager.ActiveCount, Is.EqualTo(2));

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(third);
            Object.DestroyImmediate(managerObject);
        }
    }
}

