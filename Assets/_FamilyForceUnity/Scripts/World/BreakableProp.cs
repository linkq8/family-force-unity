using UnityEngine;

namespace FamilyForceUnity.World
{
    public sealed class BreakableProp : MonoBehaviour
    {
        private int health = 24;
        private System.Action<Vector3> onBroken;
        private SpriteRenderer renderer;

        public void Configure(System.Action<Vector3> dropAction)
        {
            onBroken = dropAction;
            renderer = GetComponent<SpriteRenderer>();
        }

        public void ApplyHit(int damage)
        {
            health -= Mathf.Max(1, damage);
            if (renderer != null) renderer.color = new Color(1f, 0.55f, 0.25f);
            if (health > 0) return;
            onBroken?.Invoke(transform.position);
            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (renderer != null) renderer.color = Color.Lerp(renderer.color, Color.white, 0.16f);
        }
    }
}
