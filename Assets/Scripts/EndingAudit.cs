using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace LosQueQuedan
{
    // --ending-qa <id>: plays one ending and photographs it; --acts-qa: lights the seven candles through real interactions.
    public sealed class EndingAudit : MonoBehaviour
    {
        const string Dir = "/tmp/spooky-layout-qa/";
        static string arg;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--ending-qa"); int j = Array.IndexOf(a, "--acts-qa");
            if (i >= 0 && i + 1 < a.Length) arg = a[i + 1]; else if (j >= 0) arg = "acts"; else if (Array.IndexOf(a, "--sky-qa") >= 0) arg = "sky"; else if (Array.IndexOf(a, "--lever-qa") >= 0) arg = "lever"; else if (Array.IndexOf(a, "--press-kit") >= 0) arg = "press"; else if (Array.IndexOf(a, "--touch-qa") >= 0) arg = "touch"; else return;
            Application.runInBackground = true; new GameObject("Ending audit").AddComponent<EndingAudit>();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            yield return new WaitForSeconds(2.5f);
            var g = DemoGame.Instance;
            if (arg == "touch")
            {
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                g.Begin(); typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                typeof(DemoGame).GetField("fade", f).SetValue(g, 0f); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                g.player.Teleport(new Vector3(0, .06f, 6.5f), 0); yield return new WaitForSeconds(1.5f); Shot("touch-1");
                var c = FindFirstObjectByType<Controls>(); typeof(Controls).GetField("panel", f).SetValue(c, true);
                yield return new WaitForSeconds(.5f); Shot("touch-2-panel");
                Screen.SetResolution(900, 1600, false); yield return new WaitForSeconds(1.5f); Shot("touch-3-portrait");
                Screen.SetResolution(2880, 1800, false); yield return new WaitForSeconds(1f);   // do not leave a portrait window in the prefs
                Application.Quit(); yield break;
            }
            if (arg == "press")
            {
                // Curated screenshots for the README: /tmp/spooky-press
                const string P = "/tmp/spooky-press/"; Directory.CreateDirectory(P);
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var pitch = typeof(FirstPerson).GetField("pitch", f);
                void Call(string m) => typeof(DemoGame).GetMethod(m, f).Invoke(g, null);
                IEnumerator Snap(string name) { yield return new WaitForEndOfFrame(); var img = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(P + name + ".png", img.EncodeToPNG()); Destroy(img); }
                yield return new WaitForSeconds(3); yield return Snap("01-menu");
                g.Begin(); typeof(DemoGame).GetField("fade", f).SetValue(g, 0f); yield return new WaitForSeconds(5.5f); yield return Snap("02-ines");
                Call("StartWalking"); Call("OpenWard"); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                g.player.Teleport(new Vector3(.2f, .06f, 6.2f), 4); pitch.SetValue(g.player, 4f); yield return new WaitForSeconds(1.2f); yield return Snap("03-pasillo");
                g.Interact("mujer"); yield return new WaitForSeconds(5); yield return Snap("04-mujer"); g.CloseDialog();
                g.player.Teleport(new Vector3(6.5f, .06f, 9.6f), 180); pitch.SetValue(g.player, -12f); yield return new WaitForSeconds(1.2f); yield return Snap("05-pintada");
                g.player.Teleport(new Vector3(-7.4f, .06f, 13.4f), -100); pitch.SetValue(g.player, 6f); yield return new WaitForSeconds(1.2f); yield return Snap("06-sala-electrica");
                g.player.Teleport(new Vector3(-1.2f, .06f, 24.2f), -40); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(1); g.Interact("esquina"); yield return new WaitForSeconds(5); yield return Snap("07-esquina"); g.CloseDialog();
                g.player.Teleport(new Vector3(.5f, .06f, 21), 20); pitch.SetValue(g.player, 0f); yield return new WaitForSeconds(1.5f);
                pitch.SetValue(g.player, -80f);
                for (int i = 0; i < 48; i++) { yield return Snap("tear-" + i.ToString("00")); }
                yield return new WaitForSeconds(3); yield return Snap("08-cielo");
                pitch.SetValue(g.player, -62f); yield return new WaitForSeconds(4); yield return Snap("09-cielo-oblicuo");
                Application.Quit(); yield break;
            }
            if (arg == "lever")
            {
                // The player's own path: stand in the service room, look at the lever, use it, choose to pull it.
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var log = new System.Text.StringBuilder();
                void Check(bool ok, string what) => log.AppendLine((ok ? "PASS " : "FAIL ") + what);
                g.Begin(); typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                typeof(DemoGame).GetField("fade", f).SetValue(g, 0f);
                var lever = GameObject.Find("Palanca 41");
                Check(lever, "Lever 41 exists (service room, west wall)");
                log.AppendLine("  at " + lever.transform.position.ToString("F2"));
                g.player.Teleport(new Vector3(-8.6f, .06f, 12.2f), -90);
                var pitch = typeof(FirstPerson).GetField("pitch", f);
                var eye = g.player.view.transform.position; var dir = lever.transform.position - eye;
                pitch.SetValue(g.player, -Mathf.Asin(dir.normalized.y) * Mathf.Rad2Deg);
                yield return new WaitForSeconds(.6f);
                Check(g.player.Focus && g.player.Focus.actionId == "lever-41", "Crosshair finds the lever (" + (g.player.Focus ? g.player.Focus.actionId : "nothing") + ")");
                Shot("lever-0-look");
                var doctor = g.world.doctorNpc;
                Check(doctor.activeSelf, "Doctor is alive before");
                string before = g.world.WritingText("graffiti-risen");
                g.Interact(g.player.Focus ? g.player.Focus.actionId : "lever-41");
                yield return new WaitForSeconds(1.5f);
                Check(g.DialogueOpen, "Using it asks first (Bajar / Dejar)");
                Shot("lever-1-ask");
                // Choose option 2 first: nothing happens.
                var callbacks = (Action[])typeof(DemoGame).GetField("callbacks", f).GetValue(g);
                callbacks[1](); yield return new WaitForSeconds(.3f);
                Check(!g.DialogueOpen && doctor.activeSelf && !g.horror.Data.lever, "Leaving it changes nothing");
                g.Interact("lever-41"); yield return new WaitForSeconds(.3f);
                callbacks = (Action[])typeof(DemoGame).GetField("callbacks", f).GetValue(g);
                callbacks[0](); yield return new WaitForSeconds(2.5f);
                Check(!doctor.activeSelf, "Pulling it bursts the doctor");
                Check(GameObject.Find("Resto"), "Remains are scattered");
                string after = g.world.WritingText("graffiti-risen");
                Check(after != before, "Walls rewrite themselves («" + after + "»)");
                Check(g.world.WritingText("graffiti-father").StartsWith("CUARENTA"), "Chapel graffiti changes too («" + g.world.WritingText("graffiti-father") + "»)");
                var forehead = GameObject.Find("Pintada · graffiti-forehead");
                Check(forehead && forehead.activeInHierarchy, "New writing where he leaned");
                Check(g.horror.Data.lever, "State recorded for saving");
                g.Interact("lever-41"); yield return new WaitForSeconds(.2f);
                Check(!g.DialogueOpen, "Lever cannot be pulled twice");
                g.player.Teleport(new Vector3(.6f, .06f, 13.4f), 235); pitch.SetValue(g.player, 8f); yield return new WaitForSeconds(.8f); Shot("lever-2-corridor");
                g.player.Teleport(new Vector3(-.3f, .06f, 12.7f), 90); pitch.SetValue(g.player, -10f); yield return new WaitForSeconds(.8f); Shot("lever-3-wall");
                g.Save(false);
                Check(System.IO.File.ReadAllText(typeof(DemoGame).GetProperty("SavePath", f).GetValue(g).ToString()).Contains("\"lever\": true"), "Save file contains the pulled lever");
                g.EndWith("quedarse"); yield return new WaitForSeconds(14);
                Check(true, "Ending epilogue adds: " + Endings.Find("quedarse").afterLever);
                Shot("lever-4-ending");
                File.WriteAllText(Dir + "lever.txt", log.ToString());
                Application.Quit(); yield break;
            }
            if (arg == "sky")
            {
                // As a player would: a normal game, torch on, looking up in three rooms at two angles.
                g.Begin(); var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                typeof(DemoGame).GetField("fade", f).SetValue(g, 0f);
                var pitch = typeof(FirstPerson).GetField("pitch", f);
                Vector3[] spots = { new Vector3(0, .06f, -1), new Vector3(0, .06f, 11), new Vector3(1, .06f, 22) };
                for (int s = 0; s < spots.Length; s++)
                {
                    g.player.Teleport(spots[s], 30); pitch.SetValue(g.player, 0f); yield return new WaitForSeconds(.6f);
                    foreach (float p in new[] { -68f, -84f })
                    {
                        pitch.SetValue(g.player, p); yield return new WaitForSeconds(2.2f);
                        File.AppendAllText(Dir + "sky.txt", $"spot{s} pitch{p} inSky={g.heaven.InSky} up={g.player.view.transform.forward.y:F2}\n");
                        Shot($"skyplay-{s}{(p < -80 ? "b" : "a")}");
                    }
                }
                Application.Quit(); yield break;
            }
            if (arg == "acts")
            {
                g.Begin(); var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                for (int i = 0; i < 7; i++) { g.Interact("candle-" + i); yield return null; }
                File.AppendAllText(Dir + "acts.txt", $"candles ended={g.Ended} ending={(g.endings.Current != null ? g.endings.Current.id : "-")} remaining={g.Remaining}\n");
                yield return new WaitForSeconds(5); Shot("acts-velas");
            }
            else
            {
                g.Begin(); yield return null; g.EndWith(arg);
                yield return new WaitForSeconds(4); Shot("ending-" + arg + "-1");
                yield return new WaitForSeconds(9); Shot("ending-" + arg + "-2");
                yield return new WaitForSeconds(9); Shot("ending-" + arg + "-3");
            }
            Application.Quit();
        }
        static void Shot(string name)
        {
            var img = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Dir + name + ".jpg", img.EncodeToJPG(80)); Destroy(img);
        }
    }
}
