using System.Collections.Generic;
namespace ABI_RC.Systems.ChatBox {
 public class ChatBoxBubbleBehavior:UnityEngine.Component {
  public static Dictionary<string,ChatBoxBubbleBehavior> ChatBoxes=new Dictionary<string,ChatBoxBubbleBehavior>();
  public TMPro.TMP_Text textBubbleOutputTMP;
  public UnityEngine.UI.Image textBubbleHexagonImg,textBubbleRoundImg;
  public UnityEngine.Transform textBubbleTransform,typingTransform;
  public UnityEngine.UI.Image typingBackground;
  public List<UnityEngine.GameObject> typingGoChildren=new List<UnityEngine.GameObject>();
  public bool IsChatBubbleActive=true;
  public bool IsTypingIndicatorActive;
  public void Start(){} public void OnMessage(){} public void SetColor(){} public void OnDestroy(){}
  public void OnTyping(){} public void StopTyping(){} public void UpdateChatBox(){}
 }
}
