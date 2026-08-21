using FamilyForceUnity.Combat;
using UnityEngine;

namespace FamilyForceUnity.Characters
{
    public sealed class PrototypeFighterVisual : MonoBehaviour
    {
        private FighterStateMachine fighter;
        private SpriteRenderer body;
        private GameObject punchVisual;
        private Vector3 baseScale;
        private Color baseColor;

        public void Configure(SpriteRenderer bodyRenderer, GameObject punchObject)
        {
            fighter = GetComponent<FighterStateMachine>();
            body = bodyRenderer;
            punchVisual = punchObject;
            baseScale = transform.localScale;
            baseColor = body != null ? body.color : Color.white;
            if (punchVisual != null) punchVisual.SetActive(false);
        }

        private void LateUpdate()
        {
            if (fighter == null || body == null) return;

            if (fighter.State != FighterState.Attack || fighter.CurrentMove == null)
            {
                transform.localScale = baseScale;
                body.color = baseColor;
                if (punchVisual != null) punchVisual.SetActive(false);
                return;
            }

            int startupEnd = fighter.CurrentMove.StartupTicks;
            int activeEnd = startupEnd + fighter.CurrentMove.ActiveTicks;
            bool active = fighter.StateTick >= startupEnd && fighter.StateTick < activeEnd;

            if (fighter.StateTick < startupEnd)
            {
                transform.localScale = new Vector3(baseScale.x * 0.9f, baseScale.y * 1.06f, baseScale.z);
                body.color = Color.Lerp(baseColor, Color.white, 0.2f);
            }
            else if (active)
            {
                MoveId id = fighter.CurrentMove.Id;
                float width = id == MoveId.Special ? 1.35f : id == MoveId.HeavyPunch ? 1.25f : 1.16f;
                float height = id == MoveId.Kick ? 0.82f : id == MoveId.Jump ? 1.18f : 0.94f;
                transform.localScale = new Vector3(baseScale.x * width, baseScale.y * height, baseScale.z);
                Color flash = id switch
                {
                    MoveId.Kick => new Color(0.3f, 1f, 0.55f),
                    MoveId.HeavyPunch => new Color(1f, 0.35f, 0.2f),
                    MoveId.Jump => new Color(0.4f, 0.85f, 1f),
                    MoveId.Special => new Color(0.85f, 0.35f, 1f),
                    _ => new Color(1f, 0.82f, 0.25f)
                };
                body.color = Color.Lerp(baseColor, flash, 0.65f);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 0.35f);
                body.color = Color.Lerp(body.color, baseColor, 0.35f);
            }

            if (punchVisual != null)
            {
                punchVisual.SetActive(active && fighter.CurrentMove.Id != MoveId.Jump);
                float effectScale = fighter.CurrentMove.Id switch
                {
                    MoveId.Special => 2.1f,
                    MoveId.HeavyPunch => 1.55f,
                    MoveId.Kick => 1.3f,
                    _ => 1f
                };
                punchVisual.transform.localScale = new Vector3(0.5f * effectScale, 0.28f * effectScale, 1f);
            }
        }
    }
}
