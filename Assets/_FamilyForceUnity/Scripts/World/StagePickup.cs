using FamilyForceUnity.Characters;
using FamilyForceUnity.Input;
using UnityEngine;

namespace FamilyForceUnity.World
{
    public enum PickupKind { Food, Bat, Pipe }

    public sealed class StagePickup : MonoBehaviour
    {
        private PickupKind kind;
        private float age;
        private bool pickedUp;

        public void Configure(PickupKind value) => kind = value;

        private void Update()
        {
            age += Time.deltaTime;
            if (pickedUp) return;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 3f) * 4f);
            foreach (PrototypeFighterController player in FindObjectsByType<PrototypeFighterController>(FindObjectsSortMode.None))
            {
                if (Vector2.Distance(player.transform.position, transform.position) > 0.62f) continue;
                if (kind == PickupKind.Food)
                {
                    player.GetComponent<FighterStateMachine>().Heal(28);
                    Destroy(gameObject);
                }
                else
                {
                    player.EquipTemporaryWeapon(kind == PickupKind.Pipe ? 12 : 8);
                    pickedUp = true;
                    transform.SetParent(player.transform, false);
                    transform.localPosition = new Vector3(0.42f, 0.18f, -0.08f);
                    transform.localScale = new Vector3(kind == PickupKind.Pipe ? 0.09f : 0.12f, 0.78f, 1f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
                    GetComponent<SpriteRenderer>().sortingOrder = 13;
                    Destroy(this);
                }
                break;
            }
        }
    }
}
