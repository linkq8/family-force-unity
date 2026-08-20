using System.Collections.Generic;
using FamilyForceUnity.Combat;
using UnityEngine;

namespace FamilyForceUnity.Content
{
    [CreateAssetMenu(menuName = "Family Force Unity/Content/Character", fileName = "Character_")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string characterId;
        [SerializeField] private string displayName;
        [Min(1), SerializeField] private int heightCentimeters = 170;
        [SerializeField] private Color placeholderColor = Color.white;
        [Min(1), SerializeField] private int maxHealth = 100;
        [Min(0.1f), SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private List<MoveDefinition> moves = new();

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public int HeightCentimeters => heightCentimeters;
        public Color PlaceholderColor => placeholderColor;
        public int MaxHealth => maxHealth;
        public float WalkSpeed => walkSpeed;
        public IReadOnlyList<MoveDefinition> Moves => moves;

#if UNITY_EDITOR
        public void Configure(string id, string name, int heightCm, Color color)
        {
            characterId = id;
            displayName = name;
            heightCentimeters = heightCm;
            placeholderColor = color;
        }
#endif
    }
}

