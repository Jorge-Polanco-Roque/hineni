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
        // Arms: an idle repertoire played one gesture at a time with slow eased transitions (two-bone IK), only for
        // characters that opt in; everyone else keeps the authored pose plus breathing and drift.
        public enum Gesture { None, WringHands, TouchFace, LookAtHand }
        public Gesture[] repertoire = new Gesture[0];   // opt-in per character; seated or wall-facing people keep their pose
        public bool talkWithHands;
        Gesture current; int gestureSide = 1; float gestureStart, gestureLength, nextGesture;
        Transform handL, handR;
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
            foreL = Find("lowerarm_l"); foreR = Find("lowerarm_r"); handL = Find("hand_l"); handR = Find("hand_r");
            nextGesture = Time.time + Random.Range(1f, 4f);
            chain = new[] { pelvis, spine1, spine2, chest, neck, head, clavL, clavR, armL, armR, foreL, foreR };
            rest = new Quaternion[chain.Length];
            for (int i = 0; i < chain.Length; i++) if (chain[i]) rest[i] = chain[i].localRotation;
            if (head) faceLocal = Quaternion.Inverse(head.rotation) * transform.forward;
            skins = GetComponentsInChildren<SkinnedMeshRenderer>();
            blinkIdx = Shapes("Parpadeo"); fearIdx = Shapes("Miedo"); screamIdx = Shapes("Grito");
            nextBlink = Time.time + Random.Range(.5f, 3f);
            nextLine = Time.time + Random.Range(2f, 5f);
        }

        // ---- arms
        void Arms(float t)
        {
            if (!armL || !armR || !foreL || !foreR || !handL || !handR || !chest) return;
            if (current == Gesture.None && Time.time > nextGesture && repertoire.Length > 0)
            {
                current = repertoire[Random.Range(0, repertoire.Length)]; gestureSide = Random.value < .5f ? -1 : 1;
                gestureStart = Time.time; gestureLength = Random.Range(2.8f, 5.5f);
            }
            float g = 0;
            if (current != Gesture.None)
            {
                float k = Time.time - gestureStart;
                g = Mathf.SmoothStep(0, 1, Mathf.Min(k / 1.2f, (gestureLength - k) / 1.2f)) * .9f;   // slow in, slow out, never fully
                if (k > gestureLength) { current = Gesture.None; g = 0; nextGesture = Time.time + Random.Range(1.5f, 5f); }
            }
            bool talk = talkWithHands && (Talking || Time.time < talkUntil);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform upper = side < 0 ? armL : armR, lower = side < 0 ? foreL : foreR, hand = side < 0 ? handL : handR;
                if (Target(talk ? Gesture.LookAtHand : current, side, t, out var at, out var pole, talk)) Reach(upper, lower, hand, at, pole, talk ? .8f : g);
            }
        }

        // Where a hand goes for a gesture (world space), and which way its elbow points.
        bool Target(Gesture gesture, int side, float t, out Vector3 at, out Vector3 pole, bool talking = false)
        {
            Vector3 R = transform.right, F = transform.forward, U = Vector3.up;
            Vector3 c = chest.position, hd = head ? head.position : c + U * .45f, nk = neck ? neck.position : c + U * .3f;
            float s = side;
            pole = R * s - U * .7f - F * .2f; at = default;
            switch (gesture)
            {
                case Gesture.WringHands:
                    at = c + F * .26f - U * .2f + R * s * .03f + R * s * .015f * Mathf.Sin(t * 5 + s) + U * .012f * Mathf.Sin(t * 3.1f); return true;
                case Gesture.TouchFace:
                    if (side != gestureSide) return false;
                    at = hd + F * .1f + R * s * .07f - U * .05f + U * .008f * Mathf.Sin(t * 4); pole = R * s - U * .8f; return true;
                case Gesture.LookAtHand:
                    if (!talking && side != gestureSide) return false;
                    at = c + F * .33f + R * s * (talking ? .17f : .1f) + U * (talking ? -.05f : .04f)
                       + (talking ? U * .05f * Mathf.Sin(t * 3 + s * 1.7f) + R * .04f * Mathf.Sin(t * 2.2f + s) : Vector3.zero);
                    if (talking && side == gestureSide) at -= U * .18f;   // one hand speaks, the other rests lower
                    return true;
            }
            return false;
        }

        // Two-bone IK: shoulder-elbow-hand reaches a point with the elbow bent toward the pole, blended by weight.
        static void Reach(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 pole, float weight)
        {
            if (weight <= .001f) return;
            Quaternion u0 = upper.rotation, l0 = lower.rotation;
            Vector3 s = upper.position;
            // Hands do not go through walls: if the way from the shoulder to the target is blocked, stop at the surface.
            if (Physics.Linecast(s, target, out var hit, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(upper.root))
                target = hit.point - (target - s).normalized * .04f;
            float a = Vector3.Distance(s, lower.position), b = Vector3.Distance(lower.position, hand.position);
            Vector3 to = target - s; float d = Mathf.Clamp(to.magnitude, Mathf.Abs(a - b) + .01f, a + b - .01f); Vector3 dir = to.normalized;
            float cos = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1), sin = Mathf.Sqrt(1 - cos * cos);
            Vector3 bend = Vector3.ProjectOnPlane(pole, dir); if (bend.sqrMagnitude < 1e-4f) bend = Vector3.ProjectOnPlane(Vector3.down, dir);
            Vector3 elbow = s + dir * a * cos + bend.normalized * a * sin;
            upper.rotation = Quaternion.FromToRotation(lower.position - s, elbow - s) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, s + dir * d - lower.position) * lower.rotation;
            upper.rotation = Quaternion.Slerp(u0, upper.rotation, weight);
            lower.rotation = Quaternion.Slerp(l0, lower.rotation, weight);
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

            Arms(t);

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
