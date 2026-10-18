using System;
using ABI_RC.Systems.Communications;

namespace ComfortUI
{
    // Read-only voice state. Do not infer mute from an old web document or the input binding.
    internal sealed class HudMicrophoneSync
    {
        private string delivered;
        private float nextSnapshot;
        private int deliveredLevel = -1;
        private float nextLevelUpdate, nextLevelSnapshot;
        private static float sampledLevel, sampledAt = float.NegativeInfinity;

        internal static string Read()
        {
            if (!Comms_Manager.CommsManagerReady) return null;
            // Match native UpdateMuteState: active TTS has its own indicator even with mic muted.
            return Comms_Manager.IsTTSActive ? "tts" : Comms_Manager.IsMicMuted ? "muted" : "live";
        }

        internal static void Sample(float level, float now)
        {
            // Observe the native SetMicValue input after native processing/smoothing.
            sampledLevel = float.IsNaN(level) || float.IsInfinity(level) ? 0 : Math.Max(0, Math.Min(1, level));
            sampledAt = now;
        }

        internal static float ReadLevel(string state, float now)
        {
            // Missing callbacks/teardown must not keep an old speaking indication alive.
            if (state != "live" || !Comms_Manager.CommsManagerReady || now < sampledAt || now - sampledAt > .3f) return 0;
            return sampledLevel;
        }

        internal bool DeliverLevel(string state, float level, float now, Func<string, bool> execute)
        {
            var percent = state == "live" && !float.IsNaN(level) && !float.IsInfinity(level) ? (int)Math.Round(Math.Max(0, Math.Min(1, level)) * 100) : 0;
            if (percent == deliveredLevel && now < nextLevelSnapshot) return false;
            // Limit changing meters to 20 updates/sec; silence/mute clear immediately.
            if (percent != 0 && deliveredLevel >= 0 && now < nextLevelUpdate) return false;
            var accepted = execute("if(window.ComfortUI&&window.ComfortUI.hudMicrophoneLevel)window.ComfortUI.hudMicrophoneLevel(" + percent + ");");
            if (accepted)
            {
                deliveredLevel = percent;
                nextLevelUpdate = now + .05f;
                nextLevelSnapshot = now + 1f;
            }
            return accepted;
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
