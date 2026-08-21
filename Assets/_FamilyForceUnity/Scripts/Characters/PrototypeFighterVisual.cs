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
                transform.localScale = new Vector3(baseScale.x * 1.16f, baseScale.y * 0.94f, baseScale.z);
                body.color = Color.Lerp(baseColor, new Color(1f, 0.82f, 0.25f), 0.55f);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 0.35f);
                body.color = Color.Lerp(body.color, baseColor, 0.35f);
            }

            if (punchVisual != null) punchVisual.SetActive(active);
        }
    }
}
