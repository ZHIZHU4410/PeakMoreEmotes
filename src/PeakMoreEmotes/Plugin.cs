using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PeakMoreEmotes;

[BepInPlugin(Id, "PEAK 更多动作", "1.1.1")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "local.peak.moreemotes";
    internal const string Prefix = "PeakMoreEmotes_";
    internal const string Slot = "A_Scout_Emote_Dance2";
    internal static Plugin Instance;
    internal static readonly List<Move> Moves = new();
    internal static readonly ConditionalWeakTable<CharacterAnimations, Playback> Playing = new();
    internal static readonly ConditionalWeakTable<EmoteWheel, object> Wheels = new();
    internal static readonly AccessTools.FieldRef<CharacterAnimations, Character> CharacterRef = AccessTools.FieldRefAccess<CharacterAnimations, Character>("character");
    internal static readonly AccessTools.FieldRef<CharacterAnimations, bool> Emoting = AccessTools.FieldRefAccess<CharacterAnimations, bool>("emoting");
    internal static readonly AccessTools.FieldRef<CharacterAnimations, float> SinceStart = AccessTools.FieldRefAccess<CharacterAnimations, float>("sinceEmoteStart");
    private Harmony harmony;
    private ConfigEntry<bool> modEnabled, variations;
    private readonly List<AssetBundle> bundles = new();

    private void Awake()
    {
        Instance = this;
        modEnabled = Config.Bind("General", "Enabled", true, "启用更多动作（修改后重启游戏）。");
        variations = Config.Bind("General", "TempoVariations", true, "额外加入5个节奏变体；关闭时只添加3个独立动作素材。");
        if (!modEnabled.Value) return;
        string root = Path.GetDirectoryName(Info.Location);
        Load(root, "default_dance.bundle", "Assets/Fortnite_Default_Dance.anim", "Assets/default_dance_icon.png",
            "default_dance", "默认舞", "Default Dance", new[] { 1f, 0.7f, 1.35f, 1.7f });
        Load(root, "twerk.bundle", "Assets/Twerk.anim", "Assets/Twerk.png",
            "twerk", "扭胯舞", "Twerk", new[] { 1f, 0.7f, 1.35f });
        Load(root, "middle_finger.bundle", "Assets/Middle_Finger.anim", "Assets/middle_finger.png",
            "middle_finger", "竖中指", "Middle Finger", new[] { 1f });
        if (Config.Bind("General", "RepoEmotes", true, "添加 SomeEmotesREPO 2.0.1 的27个重定向动作/姿势，重启生效。").Value)
            LoadRepo(root);
        if (Moves.Count == 0) { Logger.LogError("没有可用动作素材，保留原版轮盘。"); return; }
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo($"Loaded {Moves.Count} wheel entries; R wheel extends automatically.");
    }

    private void LoadRepo(string root)
    {
        string file = Path.Combine(root, "repo_emotes.bundle");
        if (!File.Exists(file)) return;
        try
        {
            var bundle = AssetBundle.LoadFromFile(file);
            if (!bundle) throw new IOException("Cannot load REPO emote bundle");
            bundles.Add(bundle);
            RepoRetarget.Prefab = bundle.LoadAsset<GameObject>("assets/emotebundle/emote.prefab");
            if (!RepoRetarget.Prefab) throw new IOException("Missing source humanoid rig");
            var translations = new Dictionary<string, string> {
                ["Air Horse Gallop"]="骑马舞", ["Baby Shark"]="鲨鱼宝宝", ["Bboy Uprock"]="街舞步伐", ["Bellydancing"]="肚皮舞",
                ["Breakdance Uprock"]="霹雳舞步", ["Brooklyn Uprock"]="布鲁克林街舞", ["Can Can"]="康康舞", ["Capoeira"]="卡波耶拉",
                ["Dancing"]="欢快舞蹈", ["Dancing Running Man"]="奔跑舞", ["Female Dance Pose 1"]="舞蹈姿势一", ["Female Dance Pose 2"]="舞蹈姿势二",
                ["Hip Hop Dancing 1"]="嘻哈舞一", ["Hip Hop Dancing 2"]="嘻哈舞二", ["Hip Hop Dancing 3"]="嘻哈舞三", ["House Dancing"]="浩室舞",
                ["Jazz Dancing"]="爵士舞", ["Male Dance Pose"]="舞蹈定格", ["Robot Hip Hop Dance"]="机械嘻哈舞",
                ["Samba Dancing 1"]="桑巴舞一", ["Samba Dancing 2"]="桑巴舞二", ["Samba Dancing 3"]="桑巴舞三", ["Shuffling"]="曳步舞",
                ["Swing Dancing"]="摇摆舞", ["Thriller"]="颤栗舞", ["Tut Hip Hop Dance"]="手臂几何舞", ["Twist Dance"]="扭扭舞"
            };
            using var preview = new RepoRetarget.SourceRig();
            foreach (var clip in bundle.LoadAllAssets<AnimationClip>().OrderBy(x => x.name, StringComparer.Ordinal))
            {
                if (!translations.TryGetValue(clip.name, out var zh) || !clip.isHumanMotion) continue;
                var data = ScriptableObject.CreateInstance<EmoteWheelData>();
                data.anim = data.emoteName = Prefix + "repo_" + clip.name.Replace(' ', '_');
                data.emoteSprite = preview.Icon(clip);
                data.requireGrounded = true;
                Moves.Add(new Move { Data=data, Clip=clip, Speed=1f, Zh=zh, En=clip.name, Retarget=true });
            }
            Logger.LogInfo("REPO source loaded: " + Moves.Count(x => x.Retarget) + " retargeted emotes/poses.");
        }
        catch (Exception ex) { Logger.LogError("REPO pack: " + ex); }
    }

    private void Load(string root, string file, string clipPath, string iconPath, string id, string zh, string en, float[] speeds)
    {
        try
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(root, file));
            if (!bundle) throw new IOException("Cannot load bundle " + file);
            bundles.Add(bundle);
            var clip = bundle.LoadAsset<AnimationClip>(clipPath);
            var texture = bundle.LoadAsset<Texture2D>(iconPath);
            if (!clip || !texture) throw new IOException("Missing clip/icon in " + file);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            for (int i = 0; i < speeds.Length; i++)
            {
                if (i > 0 && !variations.Value) break;
                string suffix = i == 0 ? "" : $" ×{speeds[i]:0.##}";
                var data = ScriptableObject.CreateInstance<EmoteWheelData>();
                data.anim = data.emoteName = Prefix + id + "_" + i;
                data.emoteSprite = sprite;
                data.requireGrounded = true;
                Moves.Add(new Move { Data = data, Clip = clip, Speed = speeds[i], Zh = zh + suffix, En = en + suffix });
            }
            Logger.LogInfo($"Asset OK: {id}, {clip.length:0.00}s, {clip.name}");
        }
        catch (Exception ex) { Logger.LogError(file + ": " + ex.Message); }
    }

    internal static Move Find(string name) => Moves.Find(m => m.Data.anim == name);
    internal void LoggerForRetarget(string message) => Logger.LogInfo(message);

    internal static void Extend(EmoteWheel wheel)
    {
        if (Wheels.TryGetValue(wheel, out _)) return;
        int start = wheel.data.Length;
        // Preserve all existing entries, including those supplied by other mods.
        int total = ((start + Moves.Count + 7) / 8) * 8;
        Array.Resize(ref wheel.data, total);
        for (int i = 0; i < Moves.Count; i++) wheel.data[start + i] = Moves[i].Data;
        wheel.pages = total / 8;
        Wheels.Add(wheel, new object());
        Instance.Logger.LogInfo($"Wheel extended: {start} existing + {Moves.Count} custom, {wheel.pages} pages.");
    }

    internal static void Stop(CharacterAnimations animations, Playback state)
    {
        var character = CharacterRef(animations);
        var animator = character.refs.animator;
        state.Move = null;
        Emoting(animations) = false;
        animator.SetBool("Emote", false);
        animator.speed = state.PreviousSpeed;
        character.data.overrideIKForSeconds = state.PreviousIK;
        character.GetComponent<RepoRetarget>()?.Release();
        // Restore after the state has exited, so its final frame doesn't snap to vanilla Dance.
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        // Unity destroys the loaded assets when the process exits.
    }

    internal sealed class Move
    {
        public EmoteWheelData Data;
        public AnimationClip Clip;
        public float Speed;
        public string Zh, En;
        public bool Retarget;
        public float Duration => Clip.length < .1f ? 5f : Clip.length / Speed;
    }
    internal sealed class Playback
    {
        public Move Move;
        public AnimatorOverrideController Controller;
        public RuntimeAnimatorController Base;
        public AnimationClip Original;
        public float Started, PreviousSpeed, PreviousIK;
    }

    [HarmonyPatch(typeof(EmoteWheel), "InitWheel")]
    private static class WheelPatch
    {
        private static void Prefix(EmoteWheel __instance) => Extend(__instance);
    }

    [HarmonyPatch(typeof(EmoteWheel), "Update")]
    private static class ScrollPatch
    {
        private static readonly MethodInfo Tab = AccessTools.Method(typeof(EmoteWheel), "Tab");
        private static void Postfix(EmoteWheel __instance)
        {
            if (Input.mouseScrollDelta.y != 0)
                Tab.Invoke(__instance, new object[] { Input.mouseScrollDelta.y < 0 ? 1 : -1 });
        }
    }

    [HarmonyPatch(typeof(LocalizedText), "GetText", new[] { typeof(string), typeof(bool) })]
    private static class NamePatch
    {
        private static bool Prefix(string id, ref string __result)
        {
            var move = Find(id);
            if (move == null) return true;
            bool chinese = LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.SimplifiedChinese ||
                           LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.TraditionalChinese;
            __result = chinese ? move.Zh : move.En;
            return false;
        }
    }

    [HarmonyPatch(typeof(CharacterAnimations), "RPCA_PlayRemove")]
    private static class PlayPatch
    {
        private static bool Prefix(CharacterAnimations __instance, ref string emoteName)
        {
            var character = CharacterRef(__instance);
            var animator = character.refs.animator;
            if (!animator || string.IsNullOrEmpty(emoteName)) return true;
            var state = Playing.GetOrCreateValue(__instance);
            if (state.Move != null) Stop(__instance, state);
            var move = Find(emoteName);
            if (move == null)
            {
                if (emoteName.StartsWith(Plugin.Prefix, StringComparison.Ordinal)) return false;
                if (state.Controller && animator.runtimeAnimatorController == state.Controller)
                    state.Controller[Slot] = state.Original;
                return true;
            }
            if (character.data.dead || character.data.fullyPassedOut || character.data.isClimbing || character.data.isRopeClimbing) return false;
            try
            {
                if (!state.Controller || animator.runtimeAnimatorController != state.Controller)
                {
                    state.Base = animator.runtimeAnimatorController;
                    state.Controller = new AnimatorOverrideController(state.Base);
                    state.Original = state.Controller[Slot];
                    if (!state.Original) throw new InvalidOperationException("Missing override slot " + Slot);
                    animator.runtimeAnimatorController = state.Controller;
                    // PEAK's ragdoll evaluates this graph explicitly in FixedUpdate.
                    // A controller replacement must retain that scheduling contract.
                    var graph = animator.playableGraph;
                    if (graph.IsValid())
                    {
                        graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
                        graph.Stop();
                    }
                }
                // Humanoid REPO clips run on their own source rig; the Scout keeps a generic idle base.
                state.Controller[Slot] = move.Retarget
                    ? state.Base.animationClips.FirstOrDefault(c => c.name == "A_Scout_Idle3") ?? state.Original
                    : move.Clip;
                state.Move = move;
                state.Started = Time.time;
                state.PreviousSpeed = animator.speed;
                state.PreviousIK = character.data.overrideIKForSeconds;
                character.data.overrideIKForSeconds = move.Duration;
                if (move.Retarget)
                {
                    var retarget = character.GetComponent<RepoRetarget>() ?? character.gameObject.AddComponent<RepoRetarget>();
                    retarget.Begin(animator, move.Clip);
                }
                emoteName = Slot;
                Instance.Logger.LogInfo($"Playing {move.En} on {character.name}, duration={move.Duration:0.00}s");
                return true;
            }
            catch (Exception ex) { Instance.Logger.LogError(ex); state.Move = null; return false; }
        }
    }

    [HarmonyPatch(typeof(Bodypart), nameof(Bodypart.SaveAnimationData))]
    private static class RagdollPosePatch
    {
        private static void Prefix(Bodypart __instance)
        {
            if (!Time.inFixedTimeStep) return;
            var retarget = __instance.GetComponentInParent<RepoRetarget>();
            if (!retarget || !retarget.Active) return;
            var character = __instance.GetComponentInParent<Character>();
            var parts = character.refs.ragdoll.partList;
            if (parts.Count > 0 && parts[0] == __instance)
                retarget.CapturePhysicsPose();
        }
    }

    [HarmonyPatch(typeof(CharacterAnimations), "Update")]
    private static class TimingPatch
    {
        private static void Prefix(CharacterAnimations __instance)
        {
            if (Playing.TryGetValue(__instance, out var state) && state.Move != null)
                SinceStart(__instance) = Mathf.Min(SinceStart(__instance), 1f);
        }
        private static void Postfix(CharacterAnimations __instance)
        {
            if (!Playing.TryGetValue(__instance, out var state) || state.Move == null) return;
            var character = CharacterRef(__instance);
            if (!Emoting(__instance) || Time.time - state.Started >= state.Move.Duration ||
                character.data.dead || character.data.fullyPassedOut || character.data.isClimbing || character.data.isRopeClimbing ||
                character.refs.animator.runtimeAnimatorController != state.Controller)
                Stop(__instance, state);
            else character.refs.animator.speed = state.Move.Speed;
        }
    }
}
