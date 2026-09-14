using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prototype
{
    public static class PlacePhase1Level6Staircase
    {
        private const string Level2ScenePath = "Assets/Scenes/Level2/Level2.unity";

        [MenuItem("Prototype/Roguelite/Place Phase1 Level6 Staircase")]
        public static void CreateOrUpdateInLevel2()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            Scene desiredScene = activeScene;

            if (!string.Equals(activeScene.path, Level2ScenePath, System.StringComparison.Ordinal))
            {
                desiredScene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
                if (!desiredScene.IsValid())
                {
                    Debug.LogError("[PlacePhase1Level6Staircase] Could not open Level2 scene.");
                    return;
                }
            }

            GameObject levelRoot = desiredScene.GetRootGameObjects().FirstOrDefault(go => go.name == "Level2");
            if (levelRoot == null)
            {
                levelRoot = new GameObject("Level2");
                SceneManager.MoveGameObjectToScene(levelRoot, desiredScene);
            }

            GameObject stairGO = GameObject.Find("Phase2Stairs");
            if (stairGO == null)
            {
                stairGO = new GameObject("Phase2Stairs");
                stairGO.transform.SetParent(levelRoot.transform, false);
            }

            var sceneLoader = Object.FindAnyObjectByType<SceneLoader>();
            if (sceneLoader == null)
            {
                sceneLoader = new GameObject("SceneLoader").AddComponent<SceneLoader>();
                sceneLoader.transform.SetParent(levelRoot.transform, false);
            }

            var staircase = stairGO.GetComponent<Phase1Level6Staircase>();
            if (staircase == null)
            {
                staircase = stairGO.AddComponent<Phase1Level6Staircase>();
            }

            var box = stairGO.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = stairGO.AddComponent<BoxCollider2D>();
            }
            box.isTrigger = true;
            box.enabled = false;
            box.size = new Vector2(2.5f, 2.5f);
            box.offset = Vector2.zero;

            var renderer = stairGO.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = stairGO.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = CreatePlaceholderSprite();
            renderer.sortingOrder = 10;
            renderer.color = new Color(0.35f, 0.45f, 0.9f, 0.45f);
            renderer.enabled = false;

            GameObject bossSpawnPoint = GameObject.Find("BossSpawnPoint");
            if (bossSpawnPoint != null)
            {
                stairGO.transform.position = bossSpawnPoint.transform.position;
                stairGO.transform.rotation = bossSpawnPoint.transform.rotation;
            }

            var triggerScript = stairGO.GetComponent<Phase1Level6Staircase>();
            if (triggerScript == null)
            {
                Debug.LogError("[PlacePhase1Level6Staircase] The staircase component could not be created on Phase2Stairs.");
                return;
            }

            var serializedLoader = new SerializedObject(triggerScript);
            var loaderProp = serializedLoader.FindProperty("sceneLoader");
            if (loaderProp != null)
            {
                loaderProp.objectReferenceValue = sceneLoader;
                serializedLoader.ApplyModifiedProperties();
            }

            var visualProp = serializedLoader.FindProperty("visual");
            if (visualProp != null)
            {
                visualProp.objectReferenceValue = renderer;
                serializedLoader.ApplyModifiedProperties();
            }

            var phase2SceneNameProp = serializedLoader.FindProperty("phase2SceneName");
            if (phase2SceneNameProp != null)
            {
                phase2SceneNameProp.stringValue = "Level2_Phase2";
                serializedLoader.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(desiredScene);
            EditorSceneManager.SaveScene(desiredScene);

            Debug.Log("[PlacePhase1Level6Staircase] Phase2Stairs created and configured in Level2.");
        }

        private static Sprite CreatePlaceholderSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.30f, 0.45f, 0.95f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
