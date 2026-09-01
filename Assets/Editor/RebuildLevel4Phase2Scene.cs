/*
RebuildLevel4Phase2Scene.cs

Editor utility to rebuild Level4_Phase2 completely from code.
This avoids YAML corruption issues from manual editing.

Usage:
1. In the Unity Editor menu: Prototype/Rebuild Level4 Phase2 Scene
2. The scene will be regenerated with all platforms, controllers, and proper configuration.
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RebuildLevel4Phase2Scene
{
    private const string ScenePath = "Assets/Scenes/Level4/Level4_Phase2.unity";

    [MenuItem("Prototype/Rebuild Level4 Phase2 Scene")]
    public static void Rebuild()
    {
        Debug.Log("[RebuildLevel4Phase2Scene] Starting scene rebuild...");

        // Create new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create Main Camera
        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        Camera cam = cameraGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.032744676f, 0.033161193f, 0.03396219f, 1f);
        cameraGO.transform.position = new Vector3(0, 62, -10);
        cameraGO.AddComponent<AudioListener>();

        // Add Phase2CameraScroller
        var cameraScroller = cameraGO.AddComponent<Phase2CameraScroller>();
        SceneManager.MoveGameObjectToScene(cameraGO, scene);

        // Create Player
        GameObject playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(0, 60, 0);
        playerGO.transform.localScale = new Vector3(0.8f, 1, 1);
        
        SpriteRenderer playerSprite = playerGO.AddComponent<SpriteRenderer>();
        playerSprite.color = new Color(0.28490567f, 0.5874484f, 1, 1);

        BoxCollider2D playerCollider = playerGO.AddComponent<BoxCollider2D>();
        playerCollider.size = Vector2.one;

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.bodyType = RigidbodyType2D.Dynamic;
        playerRb.gravityScale = 3f;
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var movement = playerGO.AddComponent<Phase2MovementController>();
        SceneManager.MoveGameObjectToScene(playerGO, scene);

        // Set camera target to player
        SerializedObject cameraScrollerSO = new SerializedObject(cameraScroller);
        cameraScrollerSO.FindProperty("target").objectReferenceValue = playerGO.transform;
        cameraScrollerSO.ApplyModifiedProperties();

        // Create SceneLoader
        GameObject sceneLoaderGO = new GameObject("SceneLoader");
        var sceneLoader = sceneLoaderGO.AddComponent<SceneLoader>();
        var slSO = new SerializedObject(sceneLoader);
        slSO.FindProperty("gameplaySceneName").stringValue = "Level4";
        slSO.FindProperty("platformerSceneName").stringValue = "Level4";
        slSO.FindProperty("phase2SceneName").stringValue = "Level4_Phase2";
        slSO.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
        slSO.ApplyModifiedProperties();
        SceneManager.MoveGameObjectToScene(sceneLoaderGO, scene);

        // Create RestartController
        GameObject restartGO = new GameObject("RestartController");
        var restartController = restartGO.AddComponent<RestartController>();
        var restartSO = new SerializedObject(restartController);
        restartSO.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
        restartSO.ApplyModifiedProperties();
        SceneManager.MoveGameObjectToScene(restartGO, scene);

        // Create DeathController
        GameObject deathGO = new GameObject("DeathController");
        var deathController = deathGO.AddComponent<Phase2DeathController>();
        var deathSO = new SerializedObject(deathController);
        deathSO.FindProperty("player").objectReferenceValue = playerGO.transform;
        deathSO.FindProperty("cameraTransform").objectReferenceValue = cameraGO.transform;
        deathSO.FindProperty("sceneLoader").objectReferenceValue = sceneLoader;
        deathSO.FindProperty("killMargin").floatValue = 1f;
        deathSO.ApplyModifiedProperties();
        SceneManager.MoveGameObjectToScene(deathGO, scene);

        // Helper to create platform
        void CreatePlatform(string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject platformGO = new GameObject(name);
            platformGO.transform.position = pos;
            platformGO.transform.localScale = scale;

            SpriteRenderer sprite = platformGO.AddComponent<SpriteRenderer>();
            sprite.color = color;

            BoxCollider2D collider = platformGO.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            SceneManager.MoveGameObjectToScene(platformGO, scene);
        }

        // Create platforms
        CreatePlatform("Ground", new Vector3(0, -15, 0), new Vector3(20, 1, 1), new Color(0.42f, 0.42f, 0.42f));
        CreatePlatform("Platform_A", new Vector3(-4, 48, 0), new Vector3(3, 0.8f, 1), new Color(0.32f, 0.5f, 0.32f));
        CreatePlatform("Platform_B", new Vector3(4, 36, 0), new Vector3(3, 0.8f, 1), new Color(0.32f, 0.5f, 0.32f));
        CreatePlatform("Platform_C", new Vector3(-4, 24, 0), new Vector3(3, 0.8f, 1), new Color(0.32f, 0.5f, 0.32f));
        CreatePlatform("Platform_D", new Vector3(0, 12, 0), new Vector3(4, 0.8f, 1), new Color(0.8f, 0.8f, 0.2f));
        CreatePlatform("Platform_E", new Vector3(4, 0, 0), new Vector3(3, 0.8f, 1), new Color(0.32f, 0.5f, 0.32f));
        CreatePlatform("Platform_Meta", new Vector3(0, -12, 0), new Vector3(8, 1, 1), new Color(0.2f, 0.8f, 0.2f));

        // Save scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[RebuildLevel4Phase2Scene] Scene rebuilt and saved to {ScenePath}");
    }
}
