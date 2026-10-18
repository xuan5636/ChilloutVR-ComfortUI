using System;
using System.Reflection;
using ComfortUI;
using ABI_RC.Core.AudioEffects;
using ABI_RC.Systems.Communications.Settings;
using UnityEngine;

namespace HarmonyLib
{
    public sealed class Harmony
    {
        public MethodInfo Target;
        public void Patch(MethodInfo method, HarmonyMethod prefix) { Target = method; }
    }
    public sealed class HarmonyMethod { public HarmonyMethod(Type type, string name) { } }
    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name, Type[] args) { return type.GetMethod(name, args); }
    }
}
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) { return !ReferenceEquals(value, null) && !value.Destroyed; }
        public static void DontDestroyOnLoad(Object value) { ((GameObject)value).Persistent = true; }
        public static void Destroy(Object value) { value.Destroyed = true; }
    }
    public sealed class GameObject : Object
    {
        public bool Persistent, activeSelf = true;
        public string Name;
        public GameObject(string name) { Name = name; }
        public T AddComponent<T>() where T : AudioSource, new() { var value = new T(); value.gameObject = this; return value; }
    }
    public sealed class AudioClip : Object { }
    public class AudioSource : Object
    {
        public static AudioSource Last;
        public static int Created;
        public static bool Throw;
        public GameObject gameObject;
        public bool playOnAwake = true, loop = true, spatialize = true, mute, ignoreListenerPause;
        public float spatialBlend = 1, dopplerLevel = 1, volume = 1, pitch = 1;
        public int priority, Plays, Stops;
        public object outputAudioMixerGroup;
        public AudioClip Clip;
        public AudioSource() { Last = this; Created++; }
        public void Stop() { Stops++; }
        public void PlayOneShot(AudioClip clip) { if (Throw) throw new Exception("audio unavailable"); Clip = clip; Plays++; }
    }
}
namespace ABI_RC.Systems.Communications.Settings
{
    public static class Comms_SettingsHandler { public static bool PlaySoundOnMute = true, PlaySoundOnUnmute = true; }
}
namespace ABI_RC.Core.AudioEffects
{
    public enum AudioClipField { Click, MicMute, MicUnmute }
    public sealed class InterfaceAudio : UnityEngine.Object
    {
        public static InterfaceAudio Instance;
        public AudioSource UserInterfaceAudio;
        public AudioClip MuteClip = new AudioClip(), UnmuteClip = new AudioClip();
        public bool Available = true;
        public bool TryGetClip(AudioClipField field, out AudioClip clip) { clip = Available ? (field == AudioClipField.MicMute ? MuteClip : UnmuteClip) : null; return Available; }
        public static void Play(AudioClipField field) { }
    }
}
class MicrophoneSoundTests034
{
    static int passed;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    static int Main()
    {
        try
        {
            var harmony = new HarmonyLib.Harmony(); MicrophoneSound.Install(harmony);
            Check(harmony.Target != null && harmony.Target.Name == "Play", "Hooks only the native clip playback entry point");
            Check(AudioSource.Created == 0, "Installing feedback does not play a startup cue or create a source");
            Check(MicrophoneSound.BeforePlay(AudioClipField.Click) && AudioSource.Created == 0, "Other interface sounds retain the original playback path");
            Check(MicrophoneSound.BeforePlay(AudioClipField.MicMute), "Unavailable interface retains native audio fallback");
            var original = new AudioSource { volume = .27f, pitch = .95f, priority = 42, outputAudioMixerGroup = new object(), gameObject = new GameObject("Native UI") { activeSelf = false } };
            var audio = new InterfaceAudio { UserInterfaceAudio = original }; InterfaceAudio.Instance = audio;
            Comms_SettingsHandler.PlaySoundOnMute = false;
            Check(MicrophoneSound.BeforePlay(AudioClipField.MicMute) && AudioSource.Created == 1, "Disabled native mute cue is never forced on");
            Comms_SettingsHandler.PlaySoundOnMute = true; Comms_SettingsHandler.PlaySoundOnUnmute = false;
            Check(MicrophoneSound.BeforePlay(AudioClipField.MicUnmute) && AudioSource.Created == 1, "Unmute sound preference is independent");
            Comms_SettingsHandler.PlaySoundOnUnmute = true;
            Check(!MicrophoneSound.BeforePlay(AudioClipField.MicMute), "A handled cue suppresses duplicate playback by the original method");
            var owned = AudioSource.Last;
            Check(owned != original && owned.gameObject.Persistent && owned.gameObject.activeSelf, "Output survives world changes and a hidden native UI hierarchy");
            Check(owned.spatialBlend == 0 && !owned.spatialize && owned.dopplerLevel == 0, "Mic confirmation is a non-spatial 2D cue");
            Check(!owned.playOnAwake && !owned.loop, "New source cannot loop or play on creation");
            Check(ReferenceEquals(owned.outputAudioMixerGroup, original.outputAudioMixerGroup), "Native UI mixer group and its volume controls are preserved");
            Check(owned.volume == original.volume && owned.pitch == original.pitch && owned.priority == 42, "Native source loudness, pitch and priority are preserved");
            Check(owned.Plays == 1 && ReferenceEquals(owned.Clip, audio.MuteClip), "Uses the original/custom selected mute clip without redistributing game assets");
            Check(!MicrophoneSound.BeforePlay(AudioClipField.MicUnmute) && ReferenceEquals(AudioSource.Last, owned) && owned.Plays == 2 && ReferenceEquals(owned.Clip, audio.UnmuteClip), "Unmute reuses one source with its distinct native clip");
            Check(owned.Stops == 2, "Rapid state changes stop the preceding cue instead of stacking sounds");
            original.mute = true; original.volume = .1f; original.ignoreListenerPause = true;
            MicrophoneSound.BeforePlay(AudioClipField.MicMute);
            Check(owned.mute && owned.volume == .1f && owned.ignoreListenerPause, "Later native mute, volume and listener settings are copied without overriding them");
            audio.Available = false; var plays = owned.Plays;
            Check(MicrophoneSound.BeforePlay(AudioClipField.MicMute) && owned.Plays == plays, "Missing game clips fail open without synthesized replacement sounds");
            audio.Available = true; audio.UserInterfaceAudio = null;
            Check(MicrophoneSound.BeforePlay(AudioClipField.MicUnmute), "Missing native source cannot break a microphone action");
            audio.UserInterfaceAudio = original; AudioSource.Throw = true;
            Check(MicrophoneSound.BeforePlay(AudioClipField.MicMute), "Audio exceptions return to the native path rather than escaping into mute logic");
            AudioSource.Throw = false; MicrophoneSound.Reset();
            Check(owned.gameObject.Destroyed && !original.Destroyed, "Unload destroys only the mod-owned output");
            MicrophoneSound.BeforePlay(AudioClipField.MicUnmute);
            Check(!ReferenceEquals(AudioSource.Last, owned), "A later initialization creates a fresh output instead of reusing a stale owner");
            MicrophoneSound.Reset();
            Console.WriteLine("TOTAL " + passed + " checks; isolated audio/Harmony stand-ins, no actual sound playback"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
