using System.Collections;
using FamilyForceUnity.AI;
using FamilyForceUnity.Input;
using UnityEngine;

namespace FamilyForceUnity.Characters
{
    public sealed class LinkCompanionAssist : MonoBehaviour
    {
        private int playerIndex;
        private Color color;
        private bool held;
        private float readyAt;
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);

        public void Configure(int index, Color companionColor)
        {
            playerIndex = index;
            color = companionColor;
        }

        private void Update()
        {
            bool pressed = ControllerDeviceRouter.ReadPlayerLink(playerIndex);
            if (pressed && !held && Time.time >= readyAt)
            {
                PrototypeEnemy target = NearestEnemy();
                if (target != null)
                {
                    readyAt = Time.time + 8f;
                    StartCoroutine(PerformAssist(target));
                }
            }
            held = pressed;
        }

        private PrototypeEnemy NearestEnemy()
        {
            PrototypeEnemy nearest = null;
            float best = float.MaxValue;
            foreach (PrototypeEnemy enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None))
            {
                if (enemy.GetComponent<FighterStateMachine>().IsDefeated) continue;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < best) { best = distance; nearest = enemy; }
            }
            return nearest;
        }

        private IEnumerator PerformAssist(PrototypeEnemy target)
        {
            GameObject orb = new($"P{playerIndex + 1} Link Assist");
            var renderer = orb.AddComponent<SpriteRenderer>();
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, color); texture.Apply(false, true);
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            renderer.sortingOrder = 30;
            orb.transform.position = transform.position + Vector3.up * 0.7f;
            orb.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
            float elapsed = 0f;
            while (elapsed < 0.28f && target != null)
            {
                elapsed += Time.deltaTime;
                orb.transform.position = Vector3.Lerp(orb.transform.position, target.transform.position + Vector3.up * 0.35f, 0.3f);
                yield return null;
            }
            if (target != null)
            {
                target.GetComponent<FighterStateMachine>().ApplyHit(28, 7, true);
                float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
                target.GetComponent<LaneMotor>().ApplyKnockback(new Vector2(direction * 0.9f, 0.15f));
            }
            Destroy(renderer.sprite); Destroy(texture); Destroy(orb);
        }
    }
}
