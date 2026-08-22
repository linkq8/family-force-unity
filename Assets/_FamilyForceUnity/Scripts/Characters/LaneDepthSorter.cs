using UnityEngine;

namespace FamilyForceUnity.Characters
{
    public sealed class LaneDepthSorter : MonoBehaviour
    {
        private SpriteRenderer[] renderers;
        private int[] offsets;

        public void Configure()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            offsets = new int[renderers.Length];
            int rootOrder = GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
            for (int i = 0; i < renderers.Length; i++) offsets[i] = renderers[i].sortingOrder - rootOrder;
        }

        private void LateUpdate()
        {
            if (renderers == null) Configure();
            int baseOrder = 100 - Mathf.RoundToInt(transform.position.y * 12f);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sortingOrder = baseOrder + offsets[i];
        }
    }
}
