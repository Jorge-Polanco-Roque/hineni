using UnityEngine;

namespace LosQueQuedan
{
    // Procedural life for a character, kept inside anatomical limits so nothing reads as a puppet:
    // a gaze shared by spine, neck and head with damping; breathing that lifts chest and shoulders;
    // slow weight shifts; drifting arms; human blink rhythm (sometimes double); small saccade-like
    // head corrections. Unease comes from the face and from what they say, not from broken necks.
    // Works on the bones and blend shapes exported by Tools/build_characters.py (Parpadeo, Miedo, Grito).
    public sealed class Uncanny : MonoBehaviour
    {
        public string displayName = "";
        public float blinkEvery = 4;          // average seconds between blinks; 0 = never blinks
        [Range(0, 1)] public float follow;     // how much attention the player gets
        public float followLimit = 50;         // total turn shared by spine, neck and head
        public float tilt;                     // resting head roll, degrees (keep it small)
        public float sway = 1;                 // weight shifts and drift; 0 = still
        public float tremor;                   // fine shaking, degrees
        public float rock;                     // slow rocking, degrees
        public float knock;                    // forehead taps per second (against a wall)
        [Range(0, 1)] public float fear, fearNear, scream;
        public string[] lines = new string[0];
        public float whisperRadius = 3.4f;
        public bool Talking { get; set; }
        [System.NonSerialized] public float talkUntil;   // a whispered line keeps the mouth moving
        public Transform Head => head;

        Transform pelvis, spine1, spine2, chest, neck, head, clavL, clavR, armL, armR, foreL, foreR;
        Quaternion[] rest;
        Transform[] chain;
        Vector3 faceLocal;
        SkinnedMeshRenderer[] skins;
        int[] blinkIdx, fearIdx, screamIdx;
        float seed, nextBlink, blinkStart = -9, nextSaccade, nextLine, fearNow, look;
        float yaw, pitch, yawVel, pitchVel;     // damped gaze
        Vector2 saccade;
        bool doubleBlink;
        int line;

        void Start()
        {
            seed = Random.value * 100;
            pelvis = Find("pelvis"); spine1 = Find("spine_01"); spine2 = Find("spine_02"); chest = Find("spine_03");
            neck = Find("neck_01"); head = Find("Head");
            clavL = Find("clavicle_l"); clavR = Find("clavicle_r"); armL = Find("upperarm_l"); armR = Find("upperarm_r");
            foreL = Find("lowerarm_l"); foreR = Find("lowerarm_r");
            chain = new[] { pelvis, spine1, spine2, chest, neck, head, clavL, clavR, armL, armR, foreL, foreR };
            rest = new Quaternion[chain.Length];
            for (int i = 0; i < chain.Length; i++) if (chain[i]) rest[i] = chain[i].localRotation;
            if (head) faceLocal = Quaternion.Inverse(head.rotation) * transform.forward;
            skins = GetComponentsInChildren<SkinnedMeshRenderer>();
            blinkIdx = Shapes("Parpadeo"); fearIdx = Shapes("Miedo"); screamIdx = Shapes("Grito");
            nextBlink = Time.time + Random.Range(.5f, 3f);
            nextLine = Time.time + Random.Range(2f, 5f);
        }

        Transform Find(string bone)
        {
            foreach (var t in GetComponentsInChildren<Transform>()) if (t.name == bone) return t;
            return null;
        }

        int[] Shapes(string name)
        {
            var idx = new int[skins.Length];
            for (int i = 0; i < skins.Length; i++)
            {
                idx[i] = -1;
                var m = skins[i].sharedMesh; if (!m) continue;
                for (int s = 0; s < m.blendShapeCount; s++)
                    if (m.GetBlendShapeName(s).EndsWith(name)) idx[i] = s;
            }
            return idx;
        }

        void SetShape(int[] idx, float weight)
        {
            for (int i = 0; i < skins.Length; i++) if (idx[i] >= 0) skins[i].SetBlendShapeWeight(idx[i], weight * 100);
        }

        static void Turn(Transform b, Vector3 axis, float degrees)
        { if (b) b.rotation = Quaternion.AngleAxis(degrees, axis) * b.rotation; }

        float Noise(float speed, float channel) => Mathf.PerlinNoise((Time.time + seed) * speed, channel) * 2 - 1;

        void LateUpdate()
        {
            for (int i = 0; i < chain.Length; i++) if (chain[i]) chain[i].localRotation = rest[i];
            float t = Time.time + seed;
            Vector3 up = Vector3.up, right = transform.right, fwd = transform.forward;
            var game = DemoGame.Instance;
            Vector3 eye = game && game.player ? game.player.view.transform.position : transform.position + fwd * 3;
            float dist = Vector3.Distance(eye, transform.position);

            // Breathing: chest pitches back a little, shoulders rise on the inhale.
            float breath = Mathf.Sin(t * 1.25f) * .5f + .5f;
            Turn(chest, right, -breath * 1.6f);
            Turn(spine2, right, -breath * .6f);
            Turn(clavL, fwd, -breath * 1.4f); Turn(clavR, fwd, breath * 1.4f);

            // Weight shifts and drifting arms: slow, low amplitude, never in sync.
            Turn(pelvis, fwd, Noise(.13f, 1) * 1.6f * sway);
            Turn(spine1, fwd, -Noise(.13f, 1) * 1.1f * sway);
            Turn(spine2, up, Noise(.09f, 2) * 2f * sway);
            Turn(armL, right, Noise(.21f, 3) * 3f * sway); Turn(armR, right, Noise(.19f, 4) * 3f * sway);
            Turn(foreL, right, Noise(.27f, 5) * 2.5f * sway); Turn(foreR, right, Noise(.25f, 6) * 2.5f * sway);
            if (rock > 0) Turn(spine1, right, Mathf.Sin(t * 1.1f) * rock);

            // Gaze: a damped target, split across the spine so the turn reads as a whole body attending.
            look = Mathf.MoveTowards(look, follow, Time.deltaTime * .5f);
            Vector3 to = eye - (head ? head.position : transform.position);
            float wantYaw = Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(fwd, up), Vector3.ProjectOnPlane(to, up), up), -followLimit, followLimit) * look;
            float wantPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(to.normalized.y, -1, 1)) * Mathf.Rad2Deg, -22, 22) * look;
            if (Time.time > nextSaccade) { saccade = new Vector2(Random.Range(-3f, 3f), Random.Range(-2f, 2f)); nextSaccade = Time.time + Random.Range(.6f, 2.4f); }
            yaw = Mathf.SmoothDamp(yaw, wantYaw + saccade.x, ref yawVel, .45f);
            pitch = Mathf.SmoothDamp(pitch, wantPitch + saccade.y, ref pitchVel, .35f);
            Turn(spine2, up, yaw * .12f); Turn(chest, up, yaw * .18f);
            Turn(neck, up, yaw * .3f); Turn(neck, right, pitch * .4f);
            if (head)
            {
                Turn(head, up, yaw * .4f); Turn(head, right, pitch * .6f);
                if (knock > 0)
                {
                    float tap = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * knock * Mathf.PI * 2)), 12);
                    Turn(neck, right, tap * 5);
                }
                var face = head.rotation * faceLocal;
                Turn(head, face, tilt + Noise(.17f, 7) * 2.5f * sway);
                if (tremor > 0) head.rotation = Quaternion.Euler(Noise(9, 8) * tremor, Noise(11, 9) * tremor, Noise(7, 10) * tremor * .5f) * head.rotation;
            }

            // Blinks: irregular, sometimes doubled; faster close than open.
            if (blinkEvery > 0 && Time.time > nextBlink)
            {
                blinkStart = Time.time;
                doubleBlink = Random.value < .18f;
                nextBlink = Time.time + Mathf.Max(.4f, blinkEvery * Random.Range(.35f, 1.6f));
            }
            float b = Time.time - blinkStart;
            float lid = b < .06f ? b / .06f : b < .2f ? 1 - (b - .06f) / .14f : 0;
            if (doubleBlink && b > .26f) { float b2 = b - .26f; lid = Mathf.Max(lid, b2 < .06f ? b2 / .06f : b2 < .2f ? 1 - (b2 - .06f) / .14f : 0); }
            SetShape(blinkIdx, lid);

            float target = Mathf.Clamp01(fear + fearNear * Mathf.Clamp01(1 - (dist - 1) / 4));
            fearNow = Mathf.MoveTowards(fearNow, target, Time.deltaTime * 1.2f);
            SetShape(fearIdx, fearNow);
            bool talking = Talking || Time.time < talkUntil;
            float mouth = scream + (talking ? .12f + Mathf.Abs(Mathf.Sin(t * 8.5f)) * .22f * (.5f + Mathf.PerlinNoise(t * 3, 4)) : 0);
            SetShape(screamIdx, Mathf.Clamp01(mouth));

            // Under the breath.
            if (lines.Length > 0 && game && !game.DialogueOpen && dist < whisperRadius && Time.time > nextLine)
            {
                game.Whisper(this, lines[line++ % lines.Length]);
                nextLine = Time.time + Random.Range(7f, 11f);
            }
        }
    }
}
