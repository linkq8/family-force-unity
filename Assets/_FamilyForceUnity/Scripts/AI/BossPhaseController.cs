using FamilyForceUnity.Characters;
using UnityEngine;

namespace FamilyForceUnity.AI
{
    public sealed class BossPhaseController : MonoBehaviour
    {
        private FighterStateMachine fighter;
        private PrototypeEnemy enemy;
        private SpriteRenderer body;
        private int phase = 1;

        private void Awake()
        {
            fighter = GetComponent<FighterStateMachine>();
            enemy = GetComponent<PrototypeEnemy>();
            body = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float ratio = fighter.Health / (float)fighter.MaxHealth;
            if (phase == 1 && ratio <= 0.6f)
            {
                phase = 2;
                enemy.Configure(3.65f, 13, 55);
                if (body != null) body.color = new Color(0.7f, 0.12f, 0.18f);
            }
            else if (phase == 2 && ratio <= 0.3f)
            {
                phase = 3;
                enemy.Configure(4.05f, 16, 46);
                if (body != null) body.color = new Color(0.95f, 0.25f, 0.12f);
            }
        }
    }
}
