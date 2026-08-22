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
        private Sprite[] animationFrames;
        private float lastX;
        private FamilyForceUnity.Input.PrototypeFighterController controller;

        public void Configure(SpriteRenderer bodyRenderer, GameObject punchObject, Sprite[] frames = null)
        {
            fighter = GetComponent<FighterStateMachine>();
            controller = GetComponent<FamilyForceUnity.Input.PrototypeFighterController>();
            body = bodyRenderer;
            punchVisual = punchObject;
            baseScale = transform.localScale;
            baseColor = body != null ? body.color : Color.white;
            animationFrames = frames;
            lastX = transform.position.x;
            if (punchVisual != null) punchVisual.SetActive(false);
        }

        private void LateUpdate()
        {
            if (fighter == null || body == null) return;

            if (animationFrames is { Length: >= 16 })
            {
                UpdateSpriteAnimation();
                return;
            }

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

        private void UpdateSpriteAnimation()
        {
            float deltaX = transform.position.x - lastX;
            if (controller != null) body.flipX = controller.FacingSign < 0f;
            else if (Mathf.Abs(deltaX) > 0.001f) body.flipX = deltaX < 0f;
            lastX = transform.position.x;

            int frame;
            bool attacking = fighter.State == FighterState.Attack && fighter.CurrentMove != null;
            bool active = false;
            MoveId moveId = MoveId.Punch;
            if (attacking)
            {
                moveId = fighter.CurrentMove.Id;
                int rowStart = moveId switch
                {
                    MoveId.Punch => 8,
                    MoveId.Kick => 12,
                    MoveId.HeavyPunch => 8,
                    MoveId.Special => 8,
                    MoveId.Jump => 12,
                    _ => 0
                };
                float progress = fighter.StateTick / (float)Mathf.Max(1, fighter.CurrentMove.TotalTicks);
                frame = rowStart + Mathf.Clamp(Mathf.FloorToInt(progress * 4f), 0, 3);
                int activeStart = fighter.CurrentMove.StartupTicks;
                active = fighter.StateTick >= activeStart &&
                    fighter.StateTick < activeStart + fighter.CurrentMove.ActiveTicks;
            }
            else if (fighter.State == FighterState.Walk)
            {
                frame = 4 + Mathf.FloorToInt(Time.time * 10f) % 4;
            }
            else
            {
                frame = Mathf.FloorToInt(Time.time * 6f) % 4;
            }

            body.sprite = animationFrames[frame];
            if (punchVisual != null)
            {
                Vector3 effectPosition = punchVisual.transform.localPosition;
                effectPosition.x = Mathf.Abs(effectPosition.x) * (body.flipX ? -1f : 1f);
                punchVisual.transform.localPosition = effectPosition;
            }
            Color actionColor = moveId switch
            {
                MoveId.HeavyPunch => new Color(1f, 0.42f, 0.24f),
                MoveId.Jump => new Color(0.45f, 0.88f, 1f),
                MoveId.Special => new Color(0.9f, 0.42f, 1f),
                _ => Color.white
            };
            body.color = attacking && active ? actionColor : Color.white;
            if (fighter.State == FighterState.Hurt) body.color = new Color(1f, 0.4f, 0.4f);
            if (fighter.State == FighterState.Knockdown)
                transform.localRotation = Quaternion.Euler(0f, 0f, body.flipX ? -82f : 82f);
            else if (fighter.State == FighterState.GetUp)
            {
                float remaining = 1f - Mathf.Clamp01(fighter.StateTick / 18f);
                transform.localRotation = Quaternion.Euler(0f, 0f, (body.flipX ? -82f : 82f) * remaining);
            }
            else transform.localRotation = Quaternion.identity;
            float scale = attacking && active && moveId == MoveId.HeavyPunch ? 1.08f : 1f;
            transform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
            if (punchVisual != null)
            {
                bool showEffect = attacking && active && moveId != MoveId.Jump;
                punchVisual.SetActive(showEffect);
                float effectScale = moveId switch
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
