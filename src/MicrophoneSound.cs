using System;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Systems.Communications.Settings;
using HarmonyLib;
using UnityEngine;

namespace ComfortUI
{
    internal static class MicrophoneSound
    {
        private static AudioSource source;

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            var play = AccessTools.Method(typeof(InterfaceAudio), nameof(InterfaceAudio.Play), new[] { typeof(AudioClipField) });
            if (play == null) throw new MissingMethodException("InterfaceAudio.Play(AudioClipField)");
            harmony.Patch(play, prefix: new HarmonyMethod(typeof(MicrophoneSound), nameof(BeforePlay)));
        }

        // Keep the native state-change trigger and its settings. Only the output is
        // moved to a persistent 2D source, independent of hidden/world-space UI.
        internal static bool BeforePlay(AudioClipField __0)
        {
            if (__0 != AudioClipField.MicMute && __0 != AudioClipField.MicUnmute) return true;
            if (__0 == AudioClipField.MicMute ? !Comms_SettingsHandler.PlaySoundOnMute : !Comms_SettingsHandler.PlaySoundOnUnmute) return true;
            try
            {
                var audio = InterfaceAudio.Instance;
                if (!audio || !audio.UserInterfaceAudio) return true;
                AudioClip clip;
                if (!audio.TryGetClip(__0, out clip) || !clip) return true;
                if (!source)
                {
                    var owner = new GameObject("ComfortUI Microphone Feedback");
                    UnityEngine.Object.DontDestroyOnLoad(owner);
                    source = owner.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = false;
                    source.spatialBlend = 0;
                    source.spatialize = false;
                    source.dopplerLevel = 0;
                }
                var original = audio.UserInterfaceAudio;
                source.outputAudioMixerGroup = original.outputAudioMixerGroup;
                source.volume = original.volume;
                source.mute = original.mute;
                source.pitch = original.pitch;
                source.ignoreListenerPause = original.ignoreListenerPause;
                source.priority = original.priority;
                source.Stop();
                source.PlayOneShot(clip);
                return false; // One cue, with no second playback by the original method.
            }
            catch
            {
                // Fail open to the game's own audio path; never break a mute action.
                return true;
            }
        }

        internal static void Reset()
        {
            if (source) UnityEngine.Object.Destroy(source.gameObject);
            source = null;
        }
    }
}
