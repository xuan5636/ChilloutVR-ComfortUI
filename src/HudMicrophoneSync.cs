using System;
using ABI_RC.Systems.Communications;

namespace ComfortUI
{
    // Read-only voice state. Do not infer mute from an old web document or the input binding.
    internal sealed class HudMicrophoneSync
    {
        private string delivered;
        private float nextSnapshot;

        internal static string Read()
        {
            if (!Comms_Manager.CommsManagerReady) return null;
            // Match native UpdateMuteState: active TTS has its own indicator even with mic muted.
            return Comms_Manager.IsTTSActive ? "tts" : Comms_Manager.IsMicMuted ? "muted" : "live";
        }

        internal bool Deliver(string state, float now, Func<string, bool> execute)
        {
            if (state != "muted" && state != "live" && state != "tts") return false;
            if (state == delivered && now < nextSnapshot) return false;
            // Execute is the existing main-thread, lifetime-checked Cohtml target. A sparse
            // snapshot also hydrates a document whose script mounted after the first delivery.
            var accepted = execute("if(window.ComfortUI&&window.ComfortUI.hudMicrophone)window.ComfortUI.hudMicrophone(\"" + state + "\");");
            if (accepted) { delivered = state; nextSnapshot = now + 1f; }
            return accepted;
        }
    }
}
