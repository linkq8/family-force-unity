using UnityEngine;

namespace FamilyForceUnity.Characters
{
    public sealed class LaneMotor : MonoBehaviour
    {
        [SerializeField] private float speed = 3.5f;
        [SerializeField] private Vector2 horizontalBounds = new(-8.5f, 8.5f);
        [SerializeField] private Vector2 depthBounds = new(-2.1f, 1.6f);

        public Vector2 GroundPosition { get; private set; }
        public float VisualHeight { get; private set; }

        private void Awake()
        {
            GroundPosition = new Vector2(transform.position.x, transform.position.y);
        }

        public void SimulateMove(Vector2 input)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            GroundPosition += input * (speed * Time.fixedDeltaTime);
            GroundPosition = new Vector2(
                Mathf.Clamp(GroundPosition.x, horizontalBounds.x, horizontalBounds.y),
                Mathf.Clamp(GroundPosition.y, depthBounds.x, depthBounds.y));
            transform.position = new Vector3(GroundPosition.x, GroundPosition.y + VisualHeight, GroundPosition.y);
        }

        public void SetVisualHeight(float height)
        {
            VisualHeight = Mathf.Max(0f, height);
        }

        public void ApplyKnockback(Vector2 force)
        {
            GroundPosition += force;
            GroundPosition = new Vector2(
                Mathf.Clamp(GroundPosition.x, horizontalBounds.x, horizontalBounds.y),
                Mathf.Clamp(GroundPosition.y, depthBounds.x, depthBounds.y));
            transform.position = new Vector3(GroundPosition.x, GroundPosition.y + VisualHeight, GroundPosition.y);
        }

        public void SetHorizontalBounds(float minimum, float maximum)
        {
            horizontalBounds = new Vector2(Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum));
            GroundPosition = new Vector2(Mathf.Clamp(GroundPosition.x, horizontalBounds.x, horizontalBounds.y), GroundPosition.y);
        }
    }
}
