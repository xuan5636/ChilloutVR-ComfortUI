using System;
using System.Linq;
using System.Reflection;
using ABI_RC.Systems.ChatBox;
using ComfortUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

class ChatTypingTests040
{
    static int count;
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        count++; Console.WriteLine("PASS " + name);
    }
    static bool Near(float a, float b) { return Math.Abs(a - b) < .001f; }
    static Image Image(string name, Transform parent, float width, float height)
    {
        var image = new GameObject(name, typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false); image.gameObject.layer = 8;
        image.rectTransform.sizeDelta = new Vector2(width, height);
        image.color = new Color(0, 1, 0, .75f); return image;
    }
    static ChatBoxBubbleBehavior Bubble()
    {
        var b = new GameObject("native-overhead", typeof(ChatBoxBubbleBehavior)).GetComponent<ChatBoxBubbleBehavior>();
        b.textBubbleTransform = new GameObject("native-message").transform;
        b.textBubbleTransform.SetParent(b.transform, false);
        b.textBubbleTransform.localPosition = new Vector3(0, 160, 0);
        b.textBubbleHexagonImg = Image("native-hexagon", b.textBubbleTransform, 810, 213);
        b.textBubbleRoundImg = Image("native-round", b.textBubbleTransform, 810, 213);
        b.textBubbleRoundImg.gameObject.SetActive(false);
        b.textBubbleOutputTMP = new GameObject("native-output", typeof(TMP_Text)).GetComponent<TMP_Text>();
        b.textBubbleOutputTMP.transform.SetParent(b.textBubbleTransform, false);
        b.textBubbleOutputTMP.text = "Hello";
        b.textBubbleOutputTMP.margin = new Vector4(-242, -71, -240, -72);
        b.typingTransform = new GameObject("native-typing").transform;
        b.typingTransform.SetParent(b.transform, false);
        b.typingTransform.localPosition = new Vector3(150, -65, 0);
        b.typingTransform.localScale = new Vector3(.2f, .2f, .2f);
        b.typingBackground = Image("native-typing-background", b.typingTransform, 405, 300);
        for (var i = 0; i < 6; i++)
        {
            var frame = Image("native-typing-frame-" + i, b.typingTransform, 308, 88);
            frame.rectTransform.anchoredPosition = new Vector2(0, 42.6f);
            frame.gameObject.SetActive(i == 2); b.typingGoChildren.Add(frame.gameObject);
        }
        b.IsChatBubbleActive = false; b.textBubbleTransform.gameObject.SetActive(false);
        b.IsTypingIndicatorActive = true;
        ChatBoxBubbleBehavior.ChatBoxes[Guid.NewGuid().ToString()] = b; return b;
    }
    static Image Arrow(Image parent)
    {
        return GameObject.Created.Where(g => !g.Destroyed && g.transform.parent == parent.transform)
            .Select(g => g.GetComponent<Image>()).SingleOrDefault(i => i != null);
    }
    static float Tip(ChatBoxBubbleBehavior b)
    {
        var a = Arrow(b.textBubbleHexagonImg);
        return b.textBubbleTransform.localPosition.y +
            (a.rectTransform.anchoredPosition.y - a.rectTransform.sizeDelta.y / 2) * b.textBubbleTransform.localScale.y;
    }
    static void NativeLayoutChanged(ChatBoxBubbleBehavior b)
    {
        typeof(ChatBubbleUI).GetMethod("AfterNativeLayout", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { b });
    }
    static void Main()
    {
        var p = new ComfortMod.Preferences();
        var harmony = new HarmonyLib.Harmony(); ChatBubbleUI.Install(harmony, () => p);
        var b = Bubble(); var t = b.textBubbleOutputTMP; var typing = b.typingBackground;
        var frames = b.typingGoChildren.Select(g => g.GetComponent<Image>()).ToArray();
        var nativeSprites = frames.Select(i => i.sprite).ToArray(); var nativeTyping = typing.sprite;
        var nativeMaterials = frames.Select(i => i.material).ToArray(); var typingMaterial = typing.material;
        var rotation = b.typingTransform.localRotation; var parent = b.typingTransform.parent;
        ChatBubbleUI.Tick(p);
        Check(typing.sprite != nativeTyping && typing.type == UnityEngine.UI.Image.Type.Sliced,
            "Typing indicator uses the modern rounded card rather than the original green bubble");
        Check(typing.sprite == b.textBubbleHexagonImg.sprite, "Typing and messages share the same rounded card");
        Check(typing.color.g < .3f && frames.All(i => i.color.r > .9f), "Typing dots are legible on a quiet theme background");
        Check(frames.Select((i, n) => i.sprite != nativeSprites[n]).All(x => x), "All six native typing frames are restyled");
        Check(frames.All(i => i.rectTransform.anchoredPosition.y == 0 && i.rectTransform.sizeDelta.x < typing.rectTransform.sizeDelta.x),
            "Dots are centered and fit inside the typing card");
        Check(frames.Select((i, n) => ReferenceEquals(i.material, nativeMaterials[n])).All(x => x) && ReferenceEquals(typing.material, typingMaterial),
            "Typing retains native billboard and opacity materials");
        Check(ReferenceEquals(parent, b.typingTransform.parent) && Near(rotation.w, b.typingTransform.localRotation.w),
            "Typing stays on the native overhead canvas with native orientation");
        Check(b.IsTypingIndicatorActive && !b.IsChatBubbleActive && !b.textBubbleTransform.gameObject.activeSelf && b.typingTransform.gameObject.activeSelf,
            "Styling does not change native typing or message visibility flags");
        Check(frames.Count(i => i.gameObject.activeSelf) == 1 && frames[2].gameObject.activeSelf,
            "Native animation keeps control of which typing frame is visible");
        Check(Near(b.typingTransform.localPosition.x, 0) && b.typingTransform.localPosition.y > 0,
            "Typing sits above the player center rather than beside or below the nameplate");
        var arrow = Arrow(b.textBubbleHexagonImg); var typingArrow = Arrow(typing);
        Check(arrow != null && Arrow(b.textBubbleRoundImg) != null && typingArrow != null,
            "Both native message shapes and the typing card have a pointer");
        Check(Near(arrow.rectTransform.anchoredPosition.x, 0) && arrow.rectTransform.anchoredPosition.y < -b.textBubbleHexagonImg.rectTransform.sizeDelta.y / 2,
            "Message pointer is centered underneath the card towards the nameplate");
        Check(typingArrow.gameObject.activeSelf && typingArrow.rectTransform.anchoredPosition.y < -typing.rectTransform.sizeDelta.y / 2,
            "A standalone typing indicator also points down towards its player");
        Check(ReferenceEquals(arrow.material, b.textBubbleHexagonImg.material) && arrow.gameObject.layer == b.textBubbleHexagonImg.gameObject.layer && !arrow.raycastTarget,
            "Pointer inherits native rendering settings and cannot intercept input");
        Check(Near(arrow.color.r, b.textBubbleHexagonImg.color.r) && Near(arrow.color.a, b.textBubbleHexagonImg.color.a),
            "Pointer and card have matching theme and opacity");
        var tex = arrow.sprite.texture;
        var bottom = tex.Pixels.Take(tex.width).Count(c => c.a > 100);
        var top = tex.Pixels.Skip((tex.height - 1) * tex.width).Count(c => c.a > 100);
        Check(bottom < top / 4 && top > tex.width * .8f, "Generated pointer has a broad top and a downward tip");
        Check(frames.Select(i => i.sprite).Distinct().Count() == 6, "Comfort motion uses six gradual dot frames");
        var dots = frames[0].sprite.texture;
        Check(new[] { 28, 60, 92 }.All(x => dots.Pixels[(dots.height / 2) * dots.width + x].a > 20),
            "Each typing frame contains three visible dots");
        Check(dots.Pixels[0].a == 0 && dots.Pixels[dots.width - 1].a == 0, "Typing dots have transparent margins");
        var shortTip = Tip(b); b.IsChatBubbleActive = true; b.textBubbleTransform.gameObject.SetActive(true);
        t.text = new string('W', 220); ChatBubbleUI.Refresh(b);
        Check(Near(Tip(b), shortTip), "Long messages grow upward and keep the pointer clear of the nameplate");
        var messageTop = b.textBubbleTransform.localPosition.y + b.textBubbleHexagonImg.rectTransform.sizeDelta.y / 2;
        Check(b.typingTransform.localPosition.y - typing.rectTransform.sizeDelta.y / 2 > messageTop,
            "Typing and a visible message do not overlap");
        Check(!typingArrow.gameObject.activeSelf, "Only the message pointer is shown when typing and a message coexist");
        b.IsChatBubbleActive = false; b.textBubbleTransform.gameObject.SetActive(false); ChatBubbleUI.Refresh(b);
        Check(typingArrow.gameObject.activeSelf && Near(b.typingTransform.localPosition.y, 160),
            "When the message expires typing returns to its own centered position and pointer");
        b.IsTypingIndicatorActive = false; b.typingTransform.gameObject.SetActive(false); ChatBubbleUI.Refresh(b);
        Check(!b.typingTransform.gameObject.activeSelf && !b.IsTypingIndicatorActive, "Native typing stop remains hidden");
        var measurements = t.Measurements; var created = GameObject.Created.Count;
        for (var i = 0; i < 80; i++) { Time.realtimeSinceStartup += .3f; ChatBubbleUI.Tick(p); }
        Check(t.Measurements == measurements && GameObject.Created.Count == created, "Idle scans do not remeasure or create more UI objects");
        frames[2].gameObject.SetActive(false); frames[4].gameObject.SetActive(true); ChatBubbleUI.Refresh(b);
        Check(frames[4].gameObject.activeSelf && frames.Count(i => i.gameObject.activeSelf) == 1 && t.Measurements == measurements,
            "Native animation frames advance without rebuilding text layout");
        p = new ComfortMod.Preferences { reducedMotion = true }; ChatBubbleUI.Tick(p);
        Check(frames.Select(i => i.sprite).Distinct().Count() == 1, "Motion off uses a steady three dot hint for every native frame");
        Check(frames[4].gameObject.activeSelf, "Motion off preserves current native input status and visible frame");
        Check(GameObject.Created.Count == created, "Changing motion settings reuses the existing pointers");
        p = new ComfortMod.Preferences { palette = "custom", background = "#f0f2f4" }; ChatBubbleUI.Tick(p);
        Check(typing.color.r > .9f && frames.All(i => i.color.r < .2f) && typingArrow.color.r > .9f,
            "Light theme gives typing dark dots and a matching light pointer");
        var other = Bubble(); ChatBubbleUI.Refresh(other);
        Check(Arrow(other.typingBackground).sprite == typingArrow.sprite && other.typingBackground.sprite == typing.sprite,
            "Different players reuse shared sprites without sharing their native state");
        b.textBubbleTransform.localScale = new Vector3(2, 2, 2);
        b.textBubbleTransform.localPosition = new Vector3(0, 270, 0); NativeLayoutChanged(b);
        Check(Near(b.typingTransform.localScale.y, 2), "Typing follows changes to the native bubble size setting");
        b.IsChatBubbleActive = true; ChatBubbleUI.Refresh(b);
        Check(Near(Tip(b), 270 - 68 * 2), "Scaled long message pointer retains a consistent nameplate gap");
        var sharedPointer = arrow.sprite; var sharedDots = frames[0].sprite;
        p = new ComfortMod.Preferences { chatBubbleTheme = false }; ChatBubbleUI.Tick(p);
        Check(typing.sprite == nativeTyping && Near(typing.rectTransform.sizeDelta.x, 405) && typing.raycastTarget,
            "Original bubble style restores typing background and interaction flags");
        Check(frames.Select((i, n) => i.sprite == nativeSprites[n]).All(x => x) && frames.All(i => Near(i.rectTransform.anchoredPosition.y, 42.6f)),
            "Original style restores all typing sprites and positions");
        Check(Near(b.typingTransform.localPosition.x, 150) && Near(b.typingTransform.localPosition.y, -65) && Near(b.typingTransform.localScale.y, .2f),
            "Original style restores the native typing transform");
        Check(Near(b.textBubbleTransform.localPosition.y, 270) && Near(b.textBubbleTransform.localScale.y, 2),
            "Restoring style keeps the latest native message position and size setting");
        Check(Arrow(typing) == null && Arrow(b.textBubbleHexagonImg) == null && !sharedPointer.Destroyed,
            "Restoring removes only owned pointer objects and retains shared assets");
        Check(frames[4].gameObject.activeSelf && !b.typingTransform.gameObject.activeSelf,
            "Restoring style does not restart stopped typing or switch animation frames");
        p = new ComfortMod.Preferences(); ChatBubbleUI.Tick(p);
        Check(Arrow(typing) != null && GameObject.Created.Count(g => !g.Destroyed && g.transform.parent == typing.transform) == 1,
            "Repeated style switching creates exactly one pointer per background");
        ChatBubbleUI.Reset();
        Check(sharedPointer.Destroyed && sharedPointer.texture.Destroyed && sharedDots.Destroyed && sharedDots.texture.Destroyed,
            "Unload frees shared pointer and typing assets");
        Check(typing.sprite == nativeTyping && Arrow(typing) == null, "Unload restores native typing UI and removes owned objects");
        Console.WriteLine("TOTAL " + count);
    }
}
