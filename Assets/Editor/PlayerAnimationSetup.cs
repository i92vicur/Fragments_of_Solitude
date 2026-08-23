#if UNITY_EDITOR
/*
PlayerAnimationSetup.cs

Purpose: Editor-only helper that creates the player's 24 directional animation clips and a
simple AnimatorController for the first animation cycle.

Created clips:
- 8 Idle clips
- 8 Run clips
- 8 Dead clips

Notes:
- Idle and Dead do not currently have dedicated pure Left/Right sheets in the imported art.
  For those two missing directions we reuse the closest available diagonal sheet, as requested.
- Automatic attack is intentionally ignored; the animation graph only represents action + direction.
- The generated controller is intentionally simple: a flat list of states with no transitions.
  PlayerAnimationController selects the correct state directly.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayerAnimationSetup
{
    private const string ControllerPath = "Assets/Art/Characters/Level3/Player/Animations/Player.controller";

    private static readonly string[] Directions = new[]
    {
        "Up",
        "Down",
        "Left",
        "Right",
        "UpLeft",
        "UpRight",
        "DownLeft",
        "DownRight"
    };

    private static readonly Dictionary<string, string> IdleSourcePaths = new Dictionary<string, string>
    {
        { "Up", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Up.png" },
        { "Down", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Down.png" },
        { "Left", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Left_Up.png" },
        { "Right", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Right_Up.png" },
        { "UpLeft", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Left_Up.png" },
        { "UpRight", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Right_Up.png" },
        { "DownLeft", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Left_Down.png" },
        { "DownRight", "Assets/Art/Characters/Level3/Player/Animations/Idle/Idle_Gun_Right_Down.png" }
    };

    private static readonly Dictionary<string, string> RunSourcePaths = new Dictionary<string, string>
    {
        { "Up", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Up.png" },
        { "Down", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Down.png" },
        { "Left", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Left.png" },
        { "Right", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Right.png" },
        { "UpLeft", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Left_Up.png" },
        { "UpRight", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Right_Up.png" },
        { "DownLeft", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Left_Down.png" },
        { "DownRight", "Assets/Art/Characters/Level3/Player/Animations/Run/Run_while_shooting_Right_Down.png" }
    };

    private static readonly Dictionary<string, string> DeadSourcePaths = new Dictionary<string, string>
    {
        { "Up", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Up.png" },
        { "Down", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Down.png" },
        { "Left", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Left_Up.png" },
        { "Right", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Right_Up.png" },
        { "UpLeft", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Left_Up.png" },
        { "UpRight", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Right_Up.png" },
        { "DownLeft", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Left_Down.png" },
        { "DownRight", "Assets/Art/Characters/Level3/Player/Animations/Dead/death_Gun_Right_Down.png" }
    };

    static PlayerAnimationSetup()
    {
        EditorApplication.delayCall += EnsureSetup;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [MenuItem("Tools/Roguelite/Ensure Player Animation Setup")]
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

        var controller = EnsureController();
        if (controller == null)
            return;

        if (SceneManager.GetActiveScene().name == "Main")
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                EnsurePlayerComponents(player, controller);
        }
    }

    private static AnimatorController EnsureController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        if (controller == null)
            return null;

        var clips = new Dictionary<string, AnimationClip>();

        foreach (var direction in Directions)
        {
            clips["Idle_" + direction] = EnsureClip("Idle_" + direction + ".anim", IdleSourcePaths[direction], true);
            clips["Run_" + direction] = EnsureClip("Run_" + direction + ".anim", RunSourcePaths[direction], true);
            clips["Dead_" + direction] = EnsureClip("Dead_" + direction + ".anim", DeadSourcePaths[direction], false);
        }

        ConfigureController(controller, clips);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ControllerPath);
        return controller;
    }

    private static AnimationClip EnsureClip(string fileName, string sourcePath, bool loop)
    {
        string clipPath = "Assets/Art/Characters/Level3/Player/Animations/" + fileName;
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        var sprites = LoadSprites(sourcePath);
        if (sprites.Count == 0)
        {
            Debug.LogError($"PlayerAnimationSetup: no sprites found at {sourcePath}.");
            return clip;
        }

        clip.frameRate = 12f;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keyframes = new ObjectReferenceKeyframe[sprites.Count];
        float frameDuration = 1f / 12f;
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameDuration,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(clipPath);
        return clip;
    }

    private static List<Sprite> LoadSprites(string sourcePath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(sourcePath)
            .OfType<Sprite>()
            .OrderBy(ExtractFrameIndex)
            .ThenBy(s => s.name, StringComparer.Ordinal)
            .ToList();
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

        var layer = controller.layers[0];
        var stateMachine = layer.stateMachine;

        foreach (var state in stateMachine.states)
            stateMachine.RemoveState(state.state);
        foreach (var anyTransition in stateMachine.anyStateTransitions)
            stateMachine.RemoveAnyStateTransition(anyTransition);
        foreach (var entryTransition in stateMachine.entryTransitions)
            stateMachine.RemoveEntryTransition(entryTransition);
        foreach (var child in stateMachine.stateMachines)
            stateMachine.RemoveStateMachine(child.stateMachine);

        AnimatorState idleDown = null;

        foreach (var action in new[] { "Idle", "Run", "Dead" })
        {
            foreach (var direction in Directions)
            {
                string stateName = action + "_" + direction;
                var state = stateMachine.AddState(stateName);
                state.motion = clips[stateName];
                state.writeDefaultValues = true;

                if (action == "Idle" && direction == "Down")
                    idleDown = state;
            }
        }

        stateMachine.defaultState = idleDown;
        controller.layers[0] = layer;
    }

    private static void EnsurePlayerComponents(GameObject player, AnimatorController controller)
    {
        var animator = player.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(player);

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        var animationController = player.GetComponent<PlayerAnimationController>();
        if (animationController == null)
            Undo.AddComponent<PlayerAnimationController>(player);

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
    }
}
#endif