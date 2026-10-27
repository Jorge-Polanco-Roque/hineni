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
#if UNITY_WEBGL
            // In a browser the same audit runs from the URL (?qa=press); the page takes the screenshots.
            var url = Application.absoluteURL; int q = url.IndexOf("qa=");
            if (q >= 0)
            {
                var v = url.Substring(q + 3).Split('&')[0];
                arg = v.StartsWith("ending-") ? v.Substring(7) : v;   // press · tear · ending-<id>
                new GameObject("Ending audit").AddComponent<EndingAudit>(); return;
            }
#endif
            if (i >= 0 && i + 1 < a.Length) arg = a[i + 1]; else if (j >= 0) arg = "acts"; else if (Array.IndexOf(a, "--sky-qa") >= 0) arg = "sky"; else if (Array.IndexOf(a, "--lever-qa") >= 0) arg = "lever"; else if (Array.IndexOf(a, "--press-kit") >= 0) arg = "press"; else if (Array.IndexOf(a, "--touch-qa") >= 0) arg = "touch"; else return;
            Application.runInBackground = true; new GameObject("Ending audit").AddComponent<EndingAudit>();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            yield return new WaitForSeconds(2.5f);
            var g = DemoGame.Instance;
#if UNITY_WEBGL
            if (arg == "tear")
            {
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                g.Begin(); typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                typeof(DemoGame).GetField("fade", f).SetValue(g, 0f); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f); DemoGame.HideInterface = true;
                g.player.Teleport(new Vector3(.5f, .06f, 21), 20); yield return new WaitForSeconds(2);
                Debug.Log("QA_TEAR_START"); yield return new WaitForSeconds(.4f);
                typeof(FirstPerson).GetField("pitch", f).SetValue(g.player, -80f); yield break;
            }
            if (arg == "gestures")
            {
                // Raw (no ASCII) frames of people moving, to check arms for twisting or passing through walls.
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                g.Begin(); typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                typeof(DemoGame).GetField("fade", f).SetValue(g, 0f); DemoGame.HideInterface = true; g.Ink.enabledEffect = false; g.horror.Data.lightOn = true;
                var pitch = typeof(FirstPerson).GetField("pitch", f);
                g.player.Teleport(new Vector3(.25f, .06f, -2.0f), 27); pitch.SetValue(g.player, 6f);
                for (int i = 0; i < 12; i++) { yield return new WaitForSeconds(1.6f); Debug.Log("QA_SNAP g-ines-" + i.ToString("00")); }
                g.player.Teleport(new Vector3(1.2f, .06f, 22.7f), 90); pitch.SetValue(g.player, 10f); yield return new WaitForSeconds(2); Debug.Log("QA_SNAP g-paciente");
                g.player.Teleport(new Vector3(-.4f, .06f, 11.2f), -70); pitch.SetValue(g.player, 4f); yield return new WaitForSeconds(2); Debug.Log("QA_SNAP g-medico");
                g.player.Teleport(new Vector3(-2.7f, .06f, 24.7f), -45); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(2); Debug.Log("QA_SNAP g-esquina");
                g.player.Teleport(new Vector3(1.35f, .06f, 7.4f), 0); pitch.SetValue(g.player, 12f); yield return new WaitForSeconds(2); Debug.Log("QA_SNAP g-mujer");
                yield return new WaitForSeconds(1.5f); Debug.Log("QA_SNAP g-done"); yield break;
            }
            if (arg != "press" && arg != "store" && arg != "sky" && arg != "lever" && arg != "acts" && arg != "gestures")
            {
                g.Begin(); yield return null; g.EndWith(arg); yield return new WaitForSeconds(11); Debug.Log("QA_SNAP ending-" + arg);
                yield return new WaitForSeconds(16); Debug.Log("QA_SNAP ending-" + arg + "-full"); yield break;   // once every line is written
            }
#endif
            if (arg == "touch")
            {
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                Screen.SetResolution(1266, 585, false); yield return new WaitForSeconds(2); Shot("phone-0-menu");
                typeof(DemoGame).GetField("gallery", f).SetValue(g, true); yield return new WaitForSeconds(.5f); Shot("phone-1-gallery");
                typeof(DemoGame).GetField("gallery", f).SetValue(g, false);
                g.Begin(); typeof(DemoGame).GetMethod("StartWalking", f).Invoke(g, null); typeof(DemoGame).GetMethod("OpenWard", f).Invoke(g, null);
                typeof(DemoGame).GetField("fade", f).SetValue(g, 0f); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                g.player.Teleport(new Vector3(0, .06f, 6.5f), 0); yield return new WaitForSeconds(1.5f); Shot("touch-1");
                g.player.Teleport(new Vector3(0, .06f, 2.4f), 0); yield return new WaitForSeconds(1f); Shot("phone-2-door");
                var c = FindFirstObjectByType<Controls>(); typeof(Controls).GetField("panel", f).SetValue(c, true);
                yield return new WaitForSeconds(.5f); Shot("touch-2-panel");
                Screen.SetResolution(900, 1600, false); yield return new WaitForSeconds(1.5f); Shot("touch-3-portrait");
                Screen.SetResolution(2880, 1800, false); yield return new WaitForSeconds(1f);   // do not leave a portrait window in the prefs
                Application.Quit(); yield break;
            }
            if (arg == "press" || arg == "store")   // store: the same tour for the itch.io page, the world shots without the HUD
            {
                // Curated screenshots for the README: /tmp/spooky-press
                const string P = "/tmp/spooky-press/"; Directory.CreateDirectory(P);
                var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var pitch = typeof(FirstPerson).GetField("pitch", f);
                void Call(string m) => typeof(DemoGame).GetMethod(m, f).Invoke(g, null);
                IEnumerator Snap(string name)
                {
                    bool clean = arg == "store" && !(name.StartsWith("00") || name.StartsWith("01") || name == "02-ines" || name == "04-mujer" || name == "07-esquina");
                    if (clean) { DemoGame.HideInterface = true; typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f); yield return null; }
#if UNITY_WEBGL
                    Debug.Log("QA_SNAP " + name); yield return new WaitForSecondsRealtime(1.2f);   // the page photographs on this line
#else
                    yield return new WaitForEndOfFrame(); var img = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(P + name + ".png", img.EncodeToPNG()); Destroy(img);
#endif
                    if (clean) DemoGame.HideInterface = false;
                }
                yield return new WaitForSeconds(3); yield return Snap("01-menu");
                // The opening passage, early and once the blood has run.
                g.Begin(true); yield return new WaitForSeconds(5); yield return Snap("00a-prologo"); yield return new WaitForSeconds(16); yield return Snap("00b-prologo");
                typeof(DemoGame).GetField("prologue", f).SetValue(g, false); g.Ink.glitch = 0; g.Ink.largeText = false;
                g.Begin(); typeof(DemoGame).GetField("fade", f).SetValue(g, 0f); yield return new WaitForSeconds(5.5f); yield return Snap("02-ines");
                Call("StartWalking"); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                g.player.Teleport(new Vector3(.3f, .06f, 1.6f), 0); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(1.2f); yield return Snap("02b-puerta");
                Call("OpenWard"); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                g.player.Teleport(new Vector3(.2f, .06f, 6.2f), 4); pitch.SetValue(g.player, 4f); yield return new WaitForSeconds(1.2f); yield return Snap("03-pasillo");
                g.Interact("mujer"); yield return new WaitForSeconds(5); yield return Snap("04-mujer"); g.CloseDialog();
                g.player.Teleport(new Vector3(6.5f, .06f, 9.6f), 180); pitch.SetValue(g.player, -12f); yield return new WaitForSeconds(1.2f); yield return Snap("05-pintada");
                // The Witness: take the fuse, light it, look at it (light + gaze hold it in place).
                g.horror.Data.lightOn = true; g.horror.TryInteract("fuse"); typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                var w = g.world.witness.transform; w.position = new Vector3(7.4f, 0, 10.6f);
                typeof(HorrorDirector).GetField("grace", f).SetValue(g.horror, 999f);   // keep it still for the photographs
                g.player.Teleport(new Vector3(5.2f, .06f, 10.4f), 87); pitch.SetValue(g.player, -16f); yield return new WaitForSeconds(4); yield return Snap("05b-testigo");
                g.player.Teleport(new Vector3(6.1f, .06f, 10.5f), 86); pitch.SetValue(g.player, -34f); yield return new WaitForSeconds(5); yield return Snap("05c-testigo-rostro"); g.Ink.enabledEffect = false; yield return Snap("05d-testigo-raw"); g.Ink.enabledEffect = true;
                typeof(HorrorDirector).GetField("grace", f).SetValue(g.horror, 0f);
                g.player.Teleport(new Vector3(-7.4f, .06f, 13.4f), -100); pitch.SetValue(g.player, 6f); yield return new WaitForSeconds(1.2f); yield return Snap("06-sala-electrica");
                // Power: the corridor during the blackout, then after the breakers are thrown in order (3, 1, 2).
                g.horror.Data.lightOn = false;
                g.player.Teleport(new Vector3(0, .06f, 16.2f), 180); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(1.2f); yield return Snap("10a-apagon");
                g.horror.TryInteract("fuse-slot"); g.horror.PressBreaker(3); g.horror.PressBreaker(1); g.horror.PressBreaker(2);
                typeof(DemoGame).GetField("toastTime", f).SetValue(g, 0f);
                yield return new WaitForSeconds(.7f); yield return Snap("10b-luz"); yield return new WaitForSeconds(2.5f); yield return Snap("10c-luz-cae");
                                g.player.Teleport(new Vector3(-1.2f, .06f, 24.2f), -40); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(1); g.Interact("esquina"); yield return new WaitForSeconds(5); yield return Snap("07-esquina"); g.CloseDialog();
                g.player.Teleport(new Vector3(.5f, .06f, 21), 20); pitch.SetValue(g.player, 0f); yield return new WaitForSeconds(1.5f);
                pitch.SetValue(g.player, -80f);
#if !UNITY_WEBGL
                for (int i = 0; i < 48; i++) { yield return Snap("tear-" + i.ToString("00")); }
#endif
                yield return new WaitForSeconds(3); yield return Snap("08-cielo");
                pitch.SetValue(g.player, -62f); yield return new WaitForSeconds(4); yield return Snap("09-cielo-oblicuo");
                // The window, the corridor floor, the breaker numbers, people moving their arms.
                pitch.SetValue(g.player, 0f); yield return new WaitForSeconds(1);
                g.player.Teleport(new Vector3(0, .06f, 24.4f), 0); pitch.SetValue(g.player, -4f); yield return new WaitForSeconds(3); yield return Snap("11-ventana"); g.Ink.enabledEffect = false; yield return Snap("11r-ventana-raw"); g.Ink.enabledEffect = true;
                g.player.Teleport(new Vector3(-1.2f, .06f, 22.3f), 12); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(2); yield return Snap("11b-ventana-lejos");
                g.player.Teleport(new Vector3(.2f, .06f, 13.4f), 200); pitch.SetValue(g.player, 38f); yield return new WaitForSeconds(1.5f); yield return Snap("12-jeringas");
                g.player.Teleport(new Vector3(-8.4f, .06f, 14), -90); pitch.SetValue(g.player, -8f); g.horror.Data.lightOn = true; yield return new WaitForSeconds(1.5f); yield return Snap("13-numeros");
                g.player.Teleport(new Vector3(.13f, .06f, -2.21f), 27); pitch.SetValue(g.player, 4f); yield return new WaitForSeconds(2); yield return Snap("14-ines-a");
                yield return new WaitForSeconds(3.5f); yield return Snap("14-ines-b");
                g.player.Teleport(new Vector3(-2.6f, .06f, 24.6f), -45); pitch.SetValue(g.player, 2f); yield return new WaitForSeconds(2); yield return Snap("15-esquina-a");
                yield return new WaitForSeconds(3); yield return Snap("15-esquina-b");
                // A vision near the end: wind the clock down and wait for the sky to flash in front of us.
                g.player.Teleport(new Vector3(0, .06f, 8), 0); pitch.SetValue(g.player, 0f);
                typeof(DemoGame).GetField("remaining", f).SetValue(g, 90f);
                var flashUntil = typeof(Heaven).GetField("flashUntil", f);
                for (float wait = 0; wait < 35; wait += .05f)
                {
                    if ((float)flashUntil.GetValue(g.heaven) > Time.unscaledTime + .1f) { yield return new WaitForSeconds(.08f); yield return Snap("16-vision"); break; }
                    yield return new WaitForSeconds(.05f);
                }
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
