using FamilyForceUnity.AI;
using FamilyForceUnity.Combat;
using FamilyForceUnity.Characters;
using NUnit.Framework;
using System.Reflection;
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

        [Test]
        public void KnockdownRecoversThroughGetUpWhenHealthRemains()
        {
            var fighterObject = new GameObject("Recovering Fighter");
            var fighter = fighterObject.AddComponent<FighterStateMachine>();
            MethodInfo fixedUpdate = typeof(FighterStateMachine).GetMethod("FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);

            fighter.ApplyHit(20, 0, true);
            Assert.That(fighter.State, Is.EqualTo(FighterState.Knockdown));
            for (int i = 0; i < 45; i++) fixedUpdate.Invoke(fighter, null);
            Assert.That(fighter.State, Is.EqualTo(FighterState.GetUp));
            for (int i = 0; i < 18; i++) fixedUpdate.Invoke(fighter, null);
            Assert.That(fighter.State, Is.EqualTo(FighterState.Idle));

            Object.DestroyImmediate(fighterObject);
        }

        [Test]
        public void ConfiguredHealthAndFoodHealingStayWithinMaximum()
        {
            var fighterObject = new GameObject("Health Fighter");
            var fighter = fighterObject.AddComponent<FighterStateMachine>();
            fighter.ConfigureHealth(240);
            fighter.ApplyHit(80, 0, false);
            fighter.Heal(28);
            Assert.That(fighter.MaxHealth, Is.EqualTo(240));
            Assert.That(fighter.Health, Is.EqualTo(188));
            fighter.Heal(999);
            Assert.That(fighter.Health, Is.EqualTo(240));
            Object.DestroyImmediate(fighterObject);
        }
    }

    public sealed class GameSettingsTests
    {
        [Test]
        public void KidsDifficultyReducesDamageAndIncreasesRecoveryTime()
        {
            bool previous = Core.GameSettings.KidsMode;
            Core.GameSettings.KidsMode = true;
            Assert.Less(Core.GameSettings.EnemyDamage(10), 10);
            Assert.Greater(Core.GameSettings.EnemyCooldown(60), 60);
            Assert.Greater(Core.GameSettings.BetweenWaveHeal, 18);
            Core.GameSettings.KidsMode = previous;
        }
    }
}
