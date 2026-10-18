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
            Console.WriteLine("TOTAL " + passed + " checks; voice-manager stub and delivery policy, not actual game audio"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
