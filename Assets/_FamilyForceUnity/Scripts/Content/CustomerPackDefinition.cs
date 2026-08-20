using System.Collections.Generic;
using UnityEngine;

namespace FamilyForceUnity.Content
{
    [CreateAssetMenu(menuName = "Family Force Unity/Content/Customer Pack", fileName = "CustomerPack_")]
    public sealed class CustomerPackDefinition : ScriptableObject
    {
        [SerializeField] private string packId = "base";
        [SerializeField] private string customerDisplayName = "Family Force Unity";
        [SerializeField] private Sprite logo;
        [SerializeField] private Color primaryColor = new(0.09f, 0.14f, 0.24f);
        [SerializeField] private Color accentColor = new(1f, 0.63f, 0.16f);
        [SerializeField] private List<CharacterDefinition> characters = new();

        public string PackId => packId;
        public string CustomerDisplayName => customerDisplayName;
        public Sprite Logo => logo;
        public Color PrimaryColor => primaryColor;
        public Color AccentColor => accentColor;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
    }
}

