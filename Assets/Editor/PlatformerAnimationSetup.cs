/*
PlatformerAnimationSetup.cs

Editor utility: create or repair the platformer Animator Controller and animation clips for the configured
platformer player assets. It is setup-time tooling and writes imported animation assets, not runtime logic.
*/

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlatformerAnimationSetup
{
    private const string ControllerPath = "Assets/Art/Characters/Level4/Player/Animations/Player.controller";
    private const string SceneName = "PlatformerLevel3";

    private static readonly string[] Directions = new[] { "Front", "Left", "Right" };

    private static readonly Dictionary<string, string> IdleSourcePaths = new Dictionary<string, string>
    {
        { "Front", "Assets/Art/Characters/Level4/Player/Animations/Idle/idle_down.png" },
        { "Left", "Assets/Art/Characters/Level4/Player/Animations/Idle/idle_left_down.png" },
        { "Right", "Assets/Art/Characters/Level4/Player/Animations/Idle/idle_right_down.png" }
    };

    private static readonly Dictionary<string, string> JumpSourcePaths = new Dictionary<string, string>
    {
        { "Front", "Assets/Art/Characters/Level4/Player/Animations/Jump/Jump_Down.png" },
        { "Left", "Assets/Art/Characters/Level4/Player/Animations/Jump/Jump_Left_Down.png" },
        { "Right", "Assets/Art/Characters/Level4/Player/Animations/Jump/Jump_Right_Down.png" }
    };

    private static readonly Dictionary<string, string> DashSourcePaths = new Dictionary<string, string>
    {
        { "Front", "Assets/Art/Characters/Level4/Player/Animations/Dash/Dash_Down.png" },
        { "Left", "Assets/Art/Characters/Level4/Player/Animations/Dash/Dash_left_Down.png" },
        { "Right", "Assets/Art/Characters/Level4/Player/Animations/Dash/Dash_right_Down.png" }
    };

    // Phase 2 has no forward-facing run; only Left/Right are needed.
    private static readonly string[] RunDirections = new[] { "Left", "Right" };

    // Run sprites are not in the project yet. These paths are pending: drop matching sheets here
    // and the Run_Left/Right states will pick them up automatically.
    private static readonly Dictionary<string, string> RunSourcePaths = new Dictionary<string, string>
    {
        { "Left", "Assets/Art/Characters/Level4/Player/Animations/Run/run_left_down.png" },
        { "Right", "Assets/Art/Characters/Level4/Player/Animations/Run/run_right_down.png" }
    };

    static PlatformerAnimationSetup()
    {
        EditorApplication.delayCall += EnsureSetup;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [MenuItem("Tools/Platformer/Ensure Platformer Animation Setup")]
    public static void EnsureSetupMenu()
    {
        EnsureSetup();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EnsureSetup();
    }

    private static void EnsureSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        AnimatorController controller = EnsureController();
        if (controller == null)
            return;

        if (SceneManager.GetActiveScene().name == SceneName)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                EnsurePlayerComponents(player, controller);
        }
    }

    private static AnimatorController EnsureController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        if (controller == null)
            return null;

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (string direction in Directions)
        {
            clips["Idle_" + direction] = EnsureClip("Idle_" + direction + ".anim", IdleSourcePaths[direction], true);
            clips["Jump_" + direction] = EnsureClip("Jump_" + direction + ".anim", JumpSourcePaths[direction], true);
            clips["Dash_" + direction] = EnsureClip("Dash_" + direction + ".anim", DashSourcePaths[direction], false);
        }

        foreach (string direction in RunDirections)
        {
            clips["Run_" + direction] = EnsureClip("Run_" + direction + ".anim", RunSourcePaths[direction], true);
        }

        ConfigureController(controller, clips);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ControllerPath);
        return controller;
    }

    private static AnimationClip EnsureClip(string fileName, string sourcePath, bool loop)
    {
        string clipPath = "Assets/Art/Characters/Level4/Player/Animations/" + fileName;
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        List<Sprite> sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath)
            .OfType<Sprite>()
            .OrderBy(ExtractFrameIndex)
            .ThenBy(sprite => sprite.name, System.StringComparer.Ordinal)
            .ToList();

        if (sprites.Count == 0)
        {
            Debug.LogError("PlatformerAnimationSetup: no sprites found at " + sourcePath + ".");
            return clip;
        }

        clip.frameRate = 12f;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / 12f,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(clipPath);
        return clip;
    }

    private static int ExtractFrameIndex(Sprite sprite)
    {
        if (sprite == null)
            return int.MaxValue;

        string name = sprite.name;
        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && underscore < name.Length - 1)
        {
            int index;
            if (int.TryParse(name.Substring(underscore + 1), out index))
                return index;
        }

        return int.MaxValue;
    }

    private static void ConfigureController(AnimatorController controller, Dictionary<string, AnimationClip> clips)
    {
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters[0]);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        foreach (ChildAnimatorState state in stateMachine.states)
            stateMachine.RemoveState(state.state);

        foreach (AnimatorStateMachine child in stateMachine.stateMachines.Select(entry => entry.stateMachine).ToArray())
            stateMachine.RemoveStateMachine(child);

        AnimatorState defaultState = null;
        foreach (string action in new[] { "Idle", "Jump", "Dash" })
        {
            foreach (string direction in Directions)
            {
                string stateName = action + "_" + direction;
                AnimatorState state = stateMachine.AddState(stateName);
                state.motion = clips[stateName];
                state.writeDefaultValues = true;

                if (defaultState == null && stateName == "Idle_Front")
                    defaultState = state;
            }
        }

        foreach (string direction in RunDirections)
        {
            string stateName = "Run_" + direction;
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clips[stateName];
            state.writeDefaultValues = true;
        }

        stateMachine.defaultState = defaultState;
        layer.defaultWeight = 1f;
        controller.layers[0] = layer;
    }

    private static void EnsurePlayerComponents(GameObject player, AnimatorController controller)
    {
        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(player);

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (player.GetComponent<PlatformerCharacterAnimationController>() == null)
            Undo.AddComponent<PlatformerCharacterAnimationController>(player);
    }
}
#endif