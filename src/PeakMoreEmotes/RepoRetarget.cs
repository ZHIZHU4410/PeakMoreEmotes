using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PeakMoreEmotes;

// Samples the source pack's humanoid rig and maps it to Scout's generic skin bones.
// No R.E.P.O. game code, controller, player prefab or gameplay systems are loaded.
[DefaultExecutionOrder(24000)]
public sealed class RepoRetarget : MonoBehaviour
{
    internal static GameObject Prefab;
    private SourceRig source;
    private Animator target;
    private readonly List<Binding> bindings = new();
    private float started;
    public bool Active { get; private set; }
    public int BoneCount => bindings.Count;
    public string ClipName => source?.ClipName ?? "";
    public float PoseMotion { get; private set; }
    private Transform hip;
    private Transform[] contacts;
    private float bodyScale = 1;
    private Character character;
    public int PhysicsPoseUpdates { get; private set; }
    public float LastPoseFixedTime { get; private set; }

    // Order follows the target hierarchy, so parent rotations are written first.
    private static readonly (string target, string source, string child)[] Map = {
        ("Hip","Hips","Spine"), ("Mid","Spine","Spine1"), ("Torso","Spine2","Neck"), ("Head","Head","HeadTop_End"),
        ("S_Shoulder_L","LeftShoulder","LeftArm"), ("Arm_L","LeftArm","LeftForeArm"), ("Elbow_L","LeftForeArm","LeftHand"), ("Hand_L","LeftHand","LeftHandMiddle1"),
        ("S_Shoulder_R","RightShoulder","RightArm"), ("Arm_R","RightArm","RightForeArm"), ("Elbow_R","RightForeArm","RightHand"), ("Hand_R","RightHand","RightHandMiddle1"),
        ("Leg_L","LeftUpLeg","LeftLeg"), ("Knee_L","LeftLeg","LeftFoot"), ("Foot_L","LeftFoot","LeftToeBase"),
        ("Leg_R","RightUpLeg","RightLeg"), ("Knee_R","RightLeg","RightFoot"), ("Foot_R","RightFoot","RightToeBase")
    };
    private sealed class Binding
    {
        internal Transform Target, Source;
        internal Quaternion Offset, Previous;
    }

    internal void Begin(Animator animator, AnimationClip clip)
    {
        if (source == null) source = new SourceRig();
        if (target != animator || bindings.Count == 0) Bind(animator);
        started = Time.fixedTime;
        source.Play(clip);
        Active = true;
    }

    private void Bind(Animator animator)
    {
        target = animator;
        character = GetComponent<Character>();
        bindings.Clear();
        var transforms = animator.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key,g => g.First());
        var rest = new Dictionary<Transform, Matrix4x4>();
        foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!renderer.sharedMesh) continue;
            var poses = renderer.sharedMesh.bindposes;
            var bones = renderer.bones;
            Matrix4x4 relative = animator.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int i=0;i<Math.Min(poses.Length,bones.Length);i++)
                if (bones[i] && !rest.ContainsKey(bones[i])) rest[bones[i]] = relative * poses[i].inverse;
        }
        foreach (var mapping in Map)
        {
            if (!transforms.TryGetValue(mapping.target,out var t)) continue;
            var s = source.Bone(mapping.source);
            var sc = source.Bone(mapping.child);
            if (!s || !sc || !rest.TryGetValue(t,out var targetRest)) continue;
            Transform tc = null;
            if (mapping.target == "Hip") transforms.TryGetValue("Mid",out tc);
            else if(mapping.target == "Mid") transforms.TryGetValue("Torso",out tc);
            else if(mapping.target == "Torso") transforms.TryGetValue("Head",out tc);
            else if(mapping.target == "Head") tc = null;
            else if(mapping.target.StartsWith("S_Shoulder")) transforms.TryGetValue("Arm_"+mapping.target.Last(),out tc);
            else if(mapping.target.StartsWith("Arm_")) transforms.TryGetValue("Elbow_"+mapping.target.Last(),out tc);
            else if(mapping.target.StartsWith("Elbow_")) transforms.TryGetValue("Hand_"+mapping.target.Last(),out tc);
            else if(mapping.target.StartsWith("Hand_")) transforms.TryGetValue("Middle_1_"+mapping.target.Last(),out tc);
            else if(mapping.target.StartsWith("Leg_")) transforms.TryGetValue("Knee_"+mapping.target.Last(),out tc);
            else if(mapping.target.StartsWith("Knee_")) transforms.TryGetValue("Foot_"+mapping.target.Last(),out tc);
            else if(mapping.target.StartsWith("Foot_")) transforms.TryGetValue("S_Toe_1_"+mapping.target.Last(),out tc);
            Vector3 sd = source.RestPosition(sc) - source.RestPosition(s);
            Vector3 td = tc && rest.TryGetValue(tc,out var childRest) ? (Vector3)(childRest.GetColumn(3)-targetRest.GetColumn(3)) : Vector3.up;
            Quaternion align = sd.sqrMagnitude > 1e-8f && td.sqrMagnitude > 1e-8f ? Quaternion.FromToRotation(td,sd) : Quaternion.identity;
            Quaternion offset = Quaternion.Inverse(source.RestRotation(s)) * align * targetRest.rotation;
            bindings.Add(new Binding{Target=t,Source=s,Offset=offset});
        }
        if (bindings.Count < 14) throw new InvalidOperationException("Scout rig has only "+bindings.Count+" mapped bones");
        hip = transforms["Hip"];
        contacts = new[] { "Foot_L", "Foot_R", "Hand_L", "Hand_R", "Head" }
            .Where(transforms.ContainsKey).Select(n => transforms[n]).ToArray();
        float targetLeg = 1f;
        if (rest.TryGetValue(transforms["Leg_L"],out var thigh) && rest.TryGetValue(transforms["Knee_L"],out var knee) && rest.TryGetValue(transforms["Foot_L"],out var foot))
            targetLeg = Vector3.Distance(thigh.GetColumn(3),knee.GetColumn(3))+Vector3.Distance(knee.GetColumn(3),foot.GetColumn(3));
        bodyScale = targetLeg / source.LegLength;
        Plugin.Instance.LoggerForRetarget($"Scout REPO mapping ready: {bindings.Count} bones, body scale {bodyScale:0.000}");
    }

    // Called inside CharacterRagdoll.FixedUpdate, after native animation/IK evaluation
    // and before the first Bodypart captures its joint target. Do not write simulated
    // body transforms from Update/LateUpdate: that fights Rigidbody interpolation.
    internal void CapturePhysicsPose()
    {
        if (!Active || !target || !character) return;
        if (!Plugin.Playing.TryGetValue(character.refs.animations,out var playback) || playback.Move == null || !playback.Move.Retarget) { Release(); return; }
        source.Sample(Mathf.Max(0,Time.fixedTime-started));
        float weight = Mathf.Clamp01((Time.fixedTime-started)/.18f);
        float ground = contacts.Where(t => t.name.StartsWith("Foot_")).Min(t => t.position.y);
        PoseMotion = 0;
        foreach (var b in bindings)
        {
            Quaternion rotation = target.transform.rotation * source.Rotation(b.Source) * b.Offset;
            PoseMotion += Quaternion.Angle(b.Previous,rotation);
            b.Previous=rotation;
            b.Target.rotation = Quaternion.Slerp(b.Target.rotation,rotation,weight);
        }
        // Humanoid root offsets are authored for a different model origin. Copying them moves
        // the Scout sideways or lifts it off the floor. Keep horizontal motion in place and
        // ground the lowest foot/hand/head contact after applying the retargeted rotations.
        float correction = Mathf.Clamp(ground - contacts.Min(t => t.position.y), -.9f, .9f);
        hip.position += Vector3.up * correction * weight;
        PhysicsPoseUpdates++;
        LastPoseFixedTime = Time.fixedTime;
    }
    internal void Release()
    {
        if (!Active) return;
        Active=false;
        source?.Pause();
    }
    private void OnDestroy() { Release();source?.Dispose();source=null; }

    internal sealed class SourceRig : IDisposable
    {
        private readonly GameObject instance;
        private readonly Animator animator;
        private readonly Dictionary<string,Transform> bones;
        private readonly Dictionary<Transform,Vector3> restPosition = new();
        private readonly Dictionary<Transform,Quaternion> restRotation = new();
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        public string ClipName {get;private set;}="";
        public float LegLength {get;}
        public SourceRig()
        {
            instance=UnityEngine.Object.Instantiate(Prefab);
            instance.name="PeakMoreEmotes source rig (hidden)";
            UnityEngine.Object.DontDestroyOnLoad(instance);
            foreach(var r in instance.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            animator=instance.GetComponentInChildren<Animator>();
            if(!animator || !animator.avatar || !animator.avatar.isHuman) throw new InvalidOperationException("REPO source needs a humanoid Avatar");
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion=false;
            bones=instance.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name.Replace("mixamorig:","")).ToDictionary(g=>g.Key,g=>g.First());
            foreach(var t in bones.Values){restPosition[t]=Position(t);restRotation[t]=Rotation(t);}
            LegLength=Vector3.Distance(RestPosition(Bone("LeftUpLeg")),RestPosition(Bone("LeftLeg")))+Vector3.Distance(RestPosition(Bone("LeftLeg")),RestPosition(Bone("LeftFoot")));
            animator.runtimeAnimatorController=null;
        }
        public Transform Bone(string n)=>bones.TryGetValue(n,out var t)?t:null;
        public Vector3 Position(Transform t)=>animator.transform.InverseTransformPoint(t.position);
        public Quaternion Rotation(Transform t)=>Quaternion.Inverse(animator.transform.rotation)*t.rotation;
        public Vector3 RestPosition(Transform t)=>restPosition[t];
        public Quaternion RestRotation(Transform t)=>restRotation[t];
        public void Play(AnimationClip clip)
        {
            if(graph.IsValid())graph.Destroy();
            graph=PlayableGraph.Create("PEAK REPO "+clip.name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playable=AnimationClipPlayable.Create(graph,clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            var output=AnimationPlayableOutput.Create(graph,"Humanoid sample",animator);
            output.SetSourcePlayable(playable);graph.Play();ClipName=clip.name;
            Sample(0);
        }
        public void Sample(float t){playable.SetTime(t);graph.Evaluate(0);}
        public void Pause(){if(graph.IsValid())graph.Stop();}
        public void Dispose(){if(graph.IsValid())graph.Destroy();if(instance)UnityEngine.Object.Destroy(instance);}
        public Sprite Icon(AnimationClip clip)
        {
            Play(clip);Sample(Mathf.Min(clip.length*.35f,1.3f));
            var texture=new Texture2D(96,96,TextureFormat.RGBA32,false);
            var pixels=new Color[96*96];
            var color=new Color(.2f,.55f,.65f,1);
            var segments=new[]{("Hips","Spine2"),("Spine2","Neck"),("Neck","Head"),("LeftArm","RightArm"),("LeftArm","LeftForeArm"),("LeftForeArm","LeftHand"),("RightArm","RightForeArm"),("RightForeArm","RightHand"),("Hips","LeftUpLeg"),("Hips","RightUpLeg"),("LeftUpLeg","LeftLeg"),("LeftLeg","LeftFoot"),("RightUpLeg","RightLeg"),("RightLeg","RightFoot")};
            Vector2 Project(Transform t){var p=Position(t);return new Vector2(48+p.x/LegLength*33,12+p.y/LegLength*33);}
            void Dot(int x,int y,int radius){for(int a=-radius;a<=radius;a++)for(int b=-radius;b<=radius;b++)if(a*a+b*b<=radius*radius && x+a>=0 && x+a<96 && y+b>=0 && y+b<96)pixels[(y+b)*96+x+a]=color;}
            foreach(var pair in segments)
            {
                var a=Project(Bone(pair.Item1));var b=Project(Bone(pair.Item2));int steps=Mathf.CeilToInt(Vector2.Distance(a,b));
                for(int i=0;i<=steps;i++){var p=Vector2.Lerp(a,b,steps==0?0:(float)i/steps);Dot(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),2);}
            }
            var head=Project(Bone("Head"));Dot(Mathf.RoundToInt(head.x),Mathf.RoundToInt(head.y+3),6);
            texture.SetPixels(pixels);texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,96,96),new Vector2(.5f,.5f));
        }
    }
}
