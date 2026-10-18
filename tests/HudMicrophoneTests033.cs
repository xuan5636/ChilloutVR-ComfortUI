using System;
using ComfortUI;

namespace ABI_RC.Systems.Communications
{
    public static class Comms_Manager
    {
        public static bool CommsManagerReady, Muted, Tts;
        public static int VoiceReads;
        public static bool IsMicMuted { get { VoiceReads++; return Muted; } }
        public static bool IsTTSActive { get { VoiceReads++; return Tts; } }
    }
}
class HudMicrophoneTests033
{
    static int passed;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    static int Main()
    {
        try
        {
            Check(HudMicrophoneSync.Read() == null && ABI_RC.Systems.Communications.Comms_Manager.VoiceReads == 0, "Before voice readiness, do not invent an unmuted value or access the TTS handler");
            ABI_RC.Systems.Communications.Comms_Manager.CommsManagerReady = true;
            Check(HudMicrophoneSync.Read() == "live", "Online voice manager can report live even when the web HUD was initially muted");
            ABI_RC.Systems.Communications.Comms_Manager.Muted = true;
            Check(HudMicrophoneSync.Read() == "muted", "Join-mute and native toggle are read from the actual mute flag");
            ABI_RC.Systems.Communications.Comms_Manager.Tts = true;
            Check(HudMicrophoneSync.Read() == "tts", "TTS activity matches native HUD precedence even if microphone capture is muted");
            ABI_RC.Systems.Communications.Comms_Manager.Muted = false;
            Check(HudMicrophoneSync.Read() == "tts", "TTS remains explicit with an open microphone");
            ABI_RC.Systems.Communications.Comms_Manager.Tts = false;
            Check(HudMicrophoneSync.Read() == "live", "TTS completion returns to the actual live microphone state");
            var sync = new HudMicrophoneSync(); int calls = 0; string last = null;
            Func<string, bool> send = script => { calls++; last = script; return true; };
            Check(sync.Deliver("live", 0, send) && last.Contains("hudMicrophone(\"live\")"), "First ready HUD receives a voice-state snapshot without waiting for a mute transition");
            for (var i = 1; i < 1000; i++) sync.Deliver("live", i / 1000f, send);
            Check(calls == 1, "Stable frames do not issue repeated Cohtml calls");
            Check(sync.Deliver("muted", .999f, send) && calls == 2, "Native state change bypasses the sparse snapshot interval");
            Check(sync.Deliver("live", .9991f, send) && calls == 3, "Rapid toggle retargets immediately");
            Check(sync.Deliver("tts", .9992f, send) && calls == 4, "TTS changes use the same guarded delivery path");
            Check(!sync.Deliver(null, 1, send) && !sync.Deliver("unexpected", 1, send) && calls == 4, "Unavailable or invalid states are never sent to a page");
            Check(sync.Deliver("tts", 2.1f, send) && calls == 5, "Sparse snapshot recovers a page that mounted after an earlier script call");
            var replacement = new HudMicrophoneSync();
            Check(replacement.Deliver("tts", 2.1f, send) && calls == 6, "Replacement HUD document hydrates independently of the old document cache");
            var failed = new HudMicrophoneSync(); int rejected = 0;
            Check(!failed.Deliver("live", 0, script => { rejected++; return false; }) && failed.Deliver("live", .1f, send) && rejected == 1, "A lifetime-guarded failed send does not acknowledge delivery");
            ABI_RC.Systems.Communications.Comms_Manager.CommsManagerReady = false;
            Check(HudMicrophoneSync.Read() == null, "Voice teardown suppresses native snapshots until the replacement manager is ready");
            Check(HudMicrophoneSync.ReadLevel("live", 0) == 0, "Unready voice capture cannot leave an old speaking level");
            ABI_RC.Systems.Communications.Comms_Manager.CommsManagerReady = true;
            Check(HudMicrophoneSync.ReadLevel("live", 0) == 0, "Missing native level callback is initially silent");
            HudMicrophoneSync.Sample(.67f, 1);
            Check(HudMicrophoneSync.ReadLevel("live", 1.05f) == .67f, "Input feedback observes the actual native meter after its own gating and smoothing");
            Check(HudMicrophoneSync.ReadLevel("muted", 1.05f) == 0 && HudMicrophoneSync.ReadLevel("tts", 1.05f) == 0 && HudMicrophoneSync.ReadLevel(null, 1.05f) == 0, "Muted, TTS and disconnected states never claim microphone speech");
            Check(HudMicrophoneSync.ReadLevel("live", 1.31f) == 0, "Missing capture callbacks suppress a stale level after 300ms");
            Check(HudMicrophoneSync.ReadLevel("live", .9f) == 0, "A reset native clock cannot revive a future sample");
            ABI_RC.Systems.Communications.Comms_Manager.CommsManagerReady = false;
            Check(HudMicrophoneSync.ReadLevel("live", 1.1f) == 0, "Voice teardown clears a still-fresh capture value");
            ABI_RC.Systems.Communications.Comms_Manager.CommsManagerReady = true;
            foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f })
            {
                HudMicrophoneSync.Sample(invalid, 2);
                Check(HudMicrophoneSync.ReadLevel("live", 2.01f) == 0, "Invalid capture amplitude safely becomes silence: " + invalid);
            }
            HudMicrophoneSync.Sample(9, 2);
            Check(HudMicrophoneSync.ReadLevel("live", 2.01f) == 1, "Native amplitude above full scale is clamped");
            var meter = new HudMicrophoneSync(); calls = 0;
            Check(meter.DeliverLevel("live", .614f, 0, send) && last.Contains("hudMicrophoneLevel(61)"), "Level bridge emits bounded integer percentages without culture-dependent decimals");
            for (var i = 1; i < 50; i++) meter.DeliverLevel("live", .9f, i / 1000f, send);
            Check(calls == 1, "Changing capture samples are bounded to 20 native-to-web sends per second");
            Check(meter.DeliverLevel("live", .9f, .051f, send) && calls == 2, "Newest amplitude is sent when the paced interval expires");
            Check(meter.DeliverLevel("muted", .9f, .052f, send) && last.Contains("Level(0)"), "Mute bypasses meter pacing and clears feedback immediately");
            Check(!meter.DeliverLevel("live", .004f, .1f, send), "Sub-percent changes quantize to silence without redundant web writes");
            Check(meter.DeliverLevel(null, .9f, 1.1f, send) && last.Contains("Level(0)"), "Voice teardown sends a clear snapshot even though the mute state is unavailable");
            var idle = new HudMicrophoneSync(); calls = 0;
            for (var i = 0; i < 1000; i++) idle.DeliverLevel("live", .5f, i / 1000f, send);
            Check(calls == 1, "A constant level is deduplicated between sparse recovery snapshots");
            Check(idle.DeliverLevel("live", .5f, 1.1f, send), "Stable speech heartbeat keeps the displayed input alive");
            var retry = new HudMicrophoneSync();
            Check(!retry.DeliverLevel("live", .3f, 0, script => false) && retry.DeliverLevel("live", .3f, .01f, send), "Rejected lifecycle-guarded level sends remain eligible for immediate retry");
            Console.WriteLine("TOTAL " + passed + " checks; voice-manager stub and delivery policy, not actual game audio"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
