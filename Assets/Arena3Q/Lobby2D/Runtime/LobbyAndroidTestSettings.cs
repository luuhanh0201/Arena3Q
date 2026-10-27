using UnityEngine;

namespace Arena3Q.Lobby2D
{
    [CreateAssetMenu(menuName="Arena3Q/Lobby 2D/Android Test Settings")]
    public sealed class LobbyAndroidTestSettings : ScriptableObject
    {
        public string gameScene="Frontend";
        public string battleDestination="Xuất Chiến";
        public string[] motionLabels={"Đứng","Chạy","Tấn công"};
        public Vector2 controlsPosition=new Vector2(0,-158);
        public Vector2 controlSize=new Vector2(105,42);
        public float controlGap=8, fontSize=20, messageSeconds=2.5f;
        public string unavailableMessage="Chức năng sẽ mở trong bản tiếp theo";
        public bool showFrameRate=true;
        public float frameRateWindow=2;
    }
}
