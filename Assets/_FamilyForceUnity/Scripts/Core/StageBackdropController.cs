using UnityEngine;

namespace FamilyForceUnity.Core
{
    public sealed class StageBackdropController : MonoBehaviour
    {
        private StageProgressionController progression;
        private GameObject backdrop;
        private Sprite loadedSprite;
        private bool harborLoaded;

        public void Configure(StageProgressionController stage)
        {
            progression = stage;
            LoadBackdrop(false);
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            bool useHarbor = progression != null && progression.CurrentWave >= 3;
            if (useHarbor != harborLoaded) LoadBackdrop(useHarbor);
        }

        private void LoadBackdrop(bool harbor)
        {
            if (backdrop != null) Destroy(backdrop);
            if (loadedSprite != null) Resources.UnloadAsset(loadedSprite);
            loadedSprite = Resources.Load<Sprite>(harbor ? "StageArt/harbor-warehouse-v1" : "StageArt/neon-street-v1");
            harborLoaded = harbor;
            if (loadedSprite == null) return;
            backdrop = new GameObject(harbor ? "Harbor Warehouse Backdrop" : "Neon Street Backdrop");
            backdrop.transform.SetParent(transform, false);
            backdrop.transform.position = new Vector3(5f, 0f, 8f);
            backdrop.transform.localScale = new Vector3(3.25f, 1f, 1f);
            var renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = loadedSprite;
            renderer.sortingOrder = -18;
        }

        private void OnDestroy()
        {
            if (loadedSprite != null) Resources.UnloadAsset(loadedSprite);
        }
    }
}
