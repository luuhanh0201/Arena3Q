using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Arena3Q.Lobby2D
{
    // Separate test entry. Does not alter profiles, campaign rules or the production Frontend flow.
    [DefaultExecutionOrder(-100)]
    public sealed class LobbyAndroidTestHost : MonoBehaviour
    {
        public LobbyArtPreview view;
        public LobbyAndroidTestSettings settings;
        private LobbyArtCatalog _catalog;
        private LuBuCutoutView _hero;
        private TextMeshProUGUI _message,_fps;
        private float _messageUntil,_frameSeconds;
        private int _frames;
        private bool _enteringGame;

        private void Awake()
        {
            _catalog=Instantiate(view.catalog);view.catalog=_catalog;
        }
        private void Start()
        {
            view.Build();view.NavigationRequested+=Navigate;
            _hero=view.GetComponentInChildren<LuBuCutoutView>();
            var parent=view.SafeRoot;
            var count=Mathf.Min(settings.motionLabels.Length,_hero.definition.clips.Length);
            var width=count*settings.controlSize.x+(count-1)*settings.controlGap;
            for(var i=0;i<count;i++) {
                var index=i;var position=settings.controlsPosition+Vector2.right*((settings.controlSize.x+settings.controlGap)*i-width*.5f+settings.controlSize.x*.5f);
                var rect=Rect("Motion"+i,parent,new Vector2(.5f,1),position,settings.controlSize);
                var image=rect.gameObject.AddComponent<Image>();image.sprite=_catalog.Sprite("Secondary_Normal");image.type=Image.Type.Sliced;
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>_hero.Play(index));
                Label("Label",rect,settings.motionLabels[i],Vector2.one*.5f,Vector2.zero,settings.controlSize);
            }
            _message=Label("TestMessage",parent,"",new Vector2(.5f,0),new Vector2(0,175),new Vector2(900,58));
            _message.gameObject.SetActive(false);
            if(settings.showFrameRate)_fps=Label("FrameRate",parent,"",new Vector2(.5f,1),settings.controlsPosition+new Vector2(width*.5f+85,0),new Vector2(160,42));
            Debug.Log("[LobbyAndroidTest] Ready; full game scene="+settings.gameScene+"; rig="+_hero.definition.name);
        }
        private void Navigate(string destination)
        {
            if(destination==settings.battleDestination) {
                if(_enteringGame)return;
                if(!Application.CanStreamedLevelBeLoaded(settings.gameScene)){ShowMessage("Không tìm thấy màn hình vào game");return;}
                _enteringGame=true;SceneManager.LoadSceneAsync(settings.gameScene);return;
            }
            ShowMessage(settings.unavailableMessage);
        }
        private void ShowMessage(string message){_message.text=message;_message.gameObject.SetActive(true);_messageUntil=Time.unscaledTime+settings.messageSeconds;}
        private void Update()
        {
            if(_message&&_message.gameObject.activeSelf&&Time.unscaledTime>=_messageUntil)_message.gameObject.SetActive(false);
            if(!_fps||!Application.isFocused)return;
            _frameSeconds+=Time.unscaledDeltaTime;_frames++;
            if(_frameSeconds<settings.frameRateWindow)return;
            _fps.text=Mathf.RoundToInt(_frames/_frameSeconds)+" FPS";_frameSeconds=0;_frames=0;
        }
        private void OnDestroy(){if(view)view.NavigationRequested-=Navigate;if(_catalog)Destroy(_catalog);}
        private static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size){
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        private TextMeshProUGUI Label(string name,Transform parent,string value,Vector2 anchor,Vector2 position,Vector2 size){
            var text=Rect(name,parent,anchor,position,size).gameObject.AddComponent<TextMeshProUGUI>();text.font=_catalog.font;
            text.text=value;text.fontSize=settings.fontSize;text.color=new Color(1,.82f,.4f);text.alignment=TextAlignmentOptions.Center;
            text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;return text;
        }
    }
}
