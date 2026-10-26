using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LosQueQuedan
{
    // --layout-qa: checks the built hospital for floating objects, interpenetrating props and covered wall text,
    // and photographs every interactable and every wall message. Report: /tmp/spooky-layout-qa/report.txt
    public sealed class LayoutAudit : MonoBehaviour
    {
        const string Dir = "/tmp/spooky-layout-qa/";
        static readonly string[] Structure = { "suelo", "techo", "Pared", "Muro", "Dintel", "Zocalo", "Rodapie", "Guardacamillas", "Mancha", "Escurrimiento", "Ala ", "Marco", "Cuadro", "Placa de palanca" };
        readonly List<string> report = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--layout-qa") >= 0)
            { Application.runInBackground = true; new GameObject("Layout audit").AddComponent<LayoutAudit>(); }
        }
        static bool IsStructure(string n) { foreach (var s in Structure) if (n.Contains(s)) return true; return false; }

        IEnumerator Start()
        {
            if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            Directory.CreateDirectory(Dir);
            yield return new WaitForSeconds(3);
            var g = DemoGame.Instance; var world = g.world.transform;
            DemoGame.HideInterface = true; g.player.CanMove = false; g.horror.enabled = false;
            Physics.SyncTransforms();

            // 1. Interactables must rest on something.
            var seen = new HashSet<GameObject>();
            foreach (var it in world.GetComponentsInChildren<Interactable>(true))
            {
                var o = it.gameObject; if (!seen.Add(o) || o.GetComponent<Uncanny>() || o.GetComponent<TextMesh>()) continue;
                var b = VisualAudit.GeometryBounds(o);
                float gap = Gap(o, b);
                report.Add($"{(gap > .03f ? "FLOAT" : "ok   ")} {it.actionId} ({o.name}) gap={gap:F3} min={b.min:F2} max={b.max:F2}");
            }

            // 2. Props must not interpenetrate each other or the walls.
            var solids = new List<Collider>();
            foreach (Transform t in world) { var c = t.GetComponent<Collider>(); if (c && c.enabled && !t.GetComponent<TextMesh>()) solids.Add(c); }
            for (int i = 0; i < solids.Count; i++)
                for (int j = i + 1; j < solids.Count; j++)
                {
                    var a = solids[i]; var b = solids[j];
                    bool aProp = a.GetComponentInChildren<Renderer>() is var ra && ra && !(a is BoxCollider && a.GetComponent<MeshFilter>() && a.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Cube"));
                    bool bProp = b.GetComponentInChildren<Renderer>() is var rb && rb && !(b is BoxCollider && b.GetComponent<MeshFilter>() && b.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Cube"));
                    if (!aProp && !bProp) continue;   // architecture against architecture is by design
                    if (Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out float d) && d > .03f
                        && !a.name.Contains("suelo") && !b.name.Contains("suelo") && !(a.name.Contains("Colchon") || b.name.Contains("Colchon") || a.name.Contains("Almohada") || b.name.Contains("Almohada")))
                        report.Add($"OVERLAP {a.name} <> {b.name} depth={d:F3}");
                }

            // 3. Wall writing must sit on a wall and be clear of furniture, doors and fixtures.
            var texts = world.GetComponentsInChildren<TextMesh>(true);
            foreach (var tm in texts)
            {
                if (!tm.gameObject.activeInHierarchy) continue;
                var r = tm.GetComponent<Renderer>(); var n = -tm.transform.forward; var rb = r.bounds;
                var box = rb; box.Encapsulate(rb.min + Vector3.down * (tm.name.StartsWith("Pintada") ? 1.1f : .05f));
                box.Encapsulate(rb.center + n * .45f); box.Expand(new Vector3(n.x == 0 ? .04f : 0, 0, n.z == 0 ? .04f : 0));
                var hits = new List<string>();
                foreach (var other in world.GetComponentsInChildren<Renderer>())
                {
                    if (!other.gameObject.activeInHierarchy || other == r || other.transform.IsChildOf(tm.transform) || IsStructure(other.name) || other.GetComponent<TextMesh>()) continue;
                    var root = other.transform; while (root.parent != world) root = root.parent;
                    if (IsStructure(root.name)) continue;
                    if (other.bounds.Intersects(box)) hits.Add(root.name);
                }
                // Every corner of the text must have wall right behind it (not an opening or a door).
                int off = 0; var right = Vector3.Cross(Vector3.up, n).normalized;
                foreach (var u in new[] { -.5f, 0, .5f }) foreach (var v in new[] { -.5f, .5f })
                {
                    var p = rb.center + right * u * Mathf.Max(rb.size.x, rb.size.z) + Vector3.up * v * rb.size.y + n * .3f;
                    Collider wall = null; float near = 9;
                    foreach (var h in Physics.RaycastAll(p, -n, .5f)) if (!h.collider.transform.IsChildOf(tm.transform) && h.distance < near) { near = h.distance; wall = h.collider; }
                    if (!wall || !IsStructure(wall.name)) off++;
                }
                var flag = hits.Count > 0 ? "COVERED" : off > 0 ? "OFFWALL" : "ok     ";
                report.Add($"{flag} «{tm.text.Replace('\n', ' ')}» at {tm.transform.position:F2} {(hits.Count > 0 ? "by " + string.Join(", ", new HashSet<string>(hits)) : "")}{(off > 0 ? $" offwall={off}/6" : "")}");
            }

            // 4. Photographs, raw and ASCII.
            var cam = g.player.view; cam.fieldOfView = 60;
            int k = 0; seen.Clear();
            foreach (var it in world.GetComponentsInChildren<Interactable>(true))
            {
                var o = it.gameObject; if (!seen.Add(o) || !o.activeInHierarchy) continue;
                var b = VisualAudit.GeometryBounds(o); Vector3 eye;
                var tm = o.GetComponent<TextMesh>();
                if (tm) eye = b.center - tm.transform.forward * 2.4f;
                else { var toward = RoomCentre(b.center) - b.center; toward.y = 0; eye = b.center + toward.normalized * 1.4f + Vector3.up * .7f; }
                cam.transform.position = eye; cam.transform.rotation = Quaternion.LookRotation(b.center - eye);
                yield return Shoot($"{k++:00}-{it.actionId}");
            }
            // 5. Looking up: the tear, then the sky. 6. Lever 41 and the rewritten walls.
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            void Set(object o, string f, object v) => o.GetType().GetField(f, flags).SetValue(o, v);
            void Call(object o, string m) => o.GetType().GetMethod(m, flags).Invoke(o, null);
            g.horror.enabled = true; g.Begin(); Call(g, "StartWalking"); Call(g, "OpenWard"); Set(g, "fade", 0f);
            g.player.Teleport(new Vector3(0, .06f, 21), 0); cam.fieldOfView = 67;
            cam.transform.localPosition = new Vector3(0, 1.62f, 0);
            yield return new WaitForSeconds(.5f);
            Set(g.player, "pitch", -84f);
            yield return new WaitForSeconds(.32f); yield return Shoot("sky-0-tear");
            yield return new WaitForSeconds(1.5f); yield return Shoot("sky-1");
            foreach (var vp in new[] { new Vector2(.1f, .5f), new Vector2(.25f, .5f), new Vector2(.5f, .5f), new Vector2(.25f, .1f), new Vector2(.25f, .9f) })
            {
                var ray = cam.ViewportPointToRay(vp); var hits = Physics.RaycastAll(ray, 400);
                string names = ""; foreach (var h in hits) names += h.collider.name + "@" + h.distance.ToString("F1") + " ";
                report.Add($"SKYRAY {vp} dir={ray.direction:F2} {names}");
            }
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && r.isVisible && r.bounds.size.magnitude > 3 && Vector3.Distance(r.bounds.ClosestPoint(cam.transform.position), cam.transform.position) < 8) report.Add("SKYNEAR " + r.name + " " + r.bounds);
            Set(g.player, "pitch", -66f); yield return new WaitForSeconds(.6f); yield return Shoot("sky-2-angle");
            g.player.transform.Rotate(0, 120, 0); yield return new WaitForSeconds(.6f); yield return Shoot("sky-3-turn");
            report.Add("SKY inSky=" + g.heaven.InSky);
            yield return new WaitForSeconds(2.5f); yield return Shoot("sky-4-prayer");
            var tongueSrc = (AudioSource)g.heaven.GetType().GetField("tongue", flags).GetValue(g.heaven);
            float before = tongueSrc.time; string sub = g.heaven.Subtitle;
            report.Add($"SKY tongue playing={tongueSrc.isPlaying} t={before:F2} subtitle={sub}");
            Set(g.player, "pitch", 0f); yield return new WaitForSeconds(1.5f);
            report.Add($"SKY away tongue playing={tongueSrc.isPlaying} t={tongueSrc.time:F2}");
            Set(g.player, "pitch", -84f); yield return new WaitForSeconds(1.0f);
            report.Add($"SKY back tongue t={tongueSrc.time:F2} (resumed from {before:F2}) subtitle={g.heaven.Subtitle}");
            DemoGame.HideInterface = false; yield return Shoot("sky-5-subtitle"); DemoGame.HideInterface = true;
            Set(g.player, "pitch", 0f); yield return new WaitForSeconds(.8f);
            report.Add("SKY after looking down inSky=" + g.heaven.InSky + " fog=" + RenderSettings.fog);
            Call(g.horror, "PullLever"); yield return new WaitForSeconds(3);
            g.player.Teleport(new Vector3(.6f, .06f, 13.6f), 235); yield return new WaitForSeconds(.5f); yield return Shoot("lever-1-corridor");
            g.player.Teleport(new Vector3(-.2f, .06f, 12.7f), 90); yield return new WaitForSeconds(.5f); yield return Shoot("lever-2-risen");
            report.Add("LEVER doctor active=" + g.world.doctorNpc.activeSelf + " risen=" + g.world.WritingText("graffiti-risen"));
            g.Interact("radio"); yield return new WaitForSeconds(.5f);
            var voiceSrc = (AudioSource)g.GetType().GetField("voice", flags).GetValue(g);
            report.Add("VOICE radio playing=" + voiceSrc.isPlaying + " clip=" + (voiceSrc.clip ? voiceSrc.clip.name + " " + voiceSrc.clip.length.ToString("F1") + "s" : "none"));
            g.CloseDialog();
            File.WriteAllLines(Dir + "report.txt", report);
            Debug.Log("LAYOUT_AUDIT_DONE"); Application.Quit();
        }

        static Vector3 RoomCentre(Vector3 p)
        {
            if (p.x > 2.1f) return new Vector3(6, 0, p.z < 13.5f ? 10 : 10);
            if (p.x < -2.1f && p.z > 10.9f && p.z < 17.1f) return new Vector3(-6, 0, 14);
            if (p.z < 5) return new Vector3(0, 0, 0);
            if (p.z > 17) return new Vector3(0, 0, 22);
            return new Vector3(0, 0, p.z);
        }

        static float Gap(GameObject o, Bounds b)
        {
            float best = float.PositiveInfinity;
            foreach (var dx in new[] { -.3f, 0, .3f }) foreach (var dz in new[] { -.3f, 0, .3f })
            {
                var start = new Vector3(b.center.x + dx * b.size.x, b.min.y + .01f, b.center.z + dz * b.size.z);
                foreach (var h in Physics.RaycastAll(start, Vector3.down, 3))
                    if (!h.collider.transform.IsChildOf(o.transform)) best = Mathf.Min(best, h.distance - .01f);
            }
            return best;
        }

        IEnumerator Shoot(string name)
        {
            var g = DemoGame.Instance;
            for (int raw = 0; raw < 2; raw++)
            {
                g.Ink.enabledEffect = raw == 0;
                yield return null; yield return new WaitForEndOfFrame();
                var img = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Dir + name + (raw == 1 ? "-raw" : "-ascii") + ".png", img.EncodeToJPG(80)); Destroy(img);
            }
            g.Ink.enabledEffect = true;
        }
    }
}
