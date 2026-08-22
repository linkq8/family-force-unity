using FamilyForceUnity.Characters;
using UnityEngine;

namespace FamilyForceUnity.AI
{
    public sealed class PrototypeEnemyVisual : MonoBehaviour
    {
        private FighterStateMachine fighter;
        private Transform arm;
        private SpriteRenderer[] renderers;
        private float baseArmX;

        public void Configure(Transform attackArm)
        {
            fighter = GetComponent<FighterStateMachine>();
            arm = attackArm;
            baseArmX = arm != null ? arm.localPosition.x : 0f;
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (fighter == null || arm == null) return;
            bool attacking = fighter.State == FighterState.Attack;
            arm.localPosition = new Vector3(baseArmX + (attacking ? -0.38f : 0f), arm.localPosition.y, arm.localPosition.z);
            Color tint = fighter.State == FighterState.Hurt ? new Color(1f, 0.45f, 0.45f) : Color.white;
            foreach (SpriteRenderer item in renderers) item.color = tint;
            if (fighter.State == FighterState.Knockdown)
                transform.localRotation = Quaternion.Euler(0f, 0f, 82f);
            else if (fighter.State == FighterState.GetUp)
                transform.localRotation = Quaternion.Euler(0f, 0f, 82f * (1f - Mathf.Clamp01(fighter.StateTick / 18f)));
            else transform.localRotation = Quaternion.identity;
        }
    }
}
