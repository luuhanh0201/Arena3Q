using Arena3Q.Core;
using Arena3Q.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Arena3Q.UI
{
    /// <summary>Scene-local controls for testing the new battlefield on a phone.</summary>
    public sealed class BattlefieldTestControls : MonoBehaviour
    {
        public BattlefieldEnvironment environment;
        public BattleController battle;
        public Button GraphicsButton { get; private set; }
        public Button StageButton { get; private set; }
        private RectTransform _safe;
        private Rect _lastArea;
        private Text _graphicsLabel,_stageLabel;
        private const string Preference="Arena3Q.Battlefield.Graphics";
        private void Start()
        {
            if(environment==null||battle==null)return;
            UiFactory.EnsureEventSystem();
            var canvas=UiFactory.CreateCanvas(transform,"Battlefield test controls",12);
            _safe=UiFactory.Stretch("Safe area",canvas.transform);ApplySafeArea();
            GraphicsButton=UiFactory.CreateButton("Graphics preset",_safe,Vector2.one,new Vector2(-210f,-170f),new Vector2(360f,96f),"",new Color(.1f,.17f,.23f,.92f));
            StageButton=UiFactory.CreateButton("Switch battlefield stage",_safe,Vector2.one,new Vector2(-210f,-282f),new Vector2(360f,96f),"",new Color(.1f,.17f,.23f,.92f));
            _graphicsLabel=GraphicsButton.GetComponentInChildren<Text>();_stageLabel=StageButton.GetComponentInChildren<Text>();
            GraphicsButton.onClick.AddListener(CycleGraphics);StageButton.onClick.AddListener(SwitchStage);
            if(Application.isMobilePlatform)
            {
                environment.quality=(BattlefieldGraphics)Mathf.Clamp(PlayerPrefs.GetInt(Preference,(int)BattlefieldGraphics.Medium),0,2);
                environment.GetComponent<BattlefieldQuality>().Apply();
            }
            battle.BattleStarted+=Refresh;Refresh();
        }
        private void LateUpdate(){if(_safe!=null&&Screen.safeArea!=_lastArea)ApplySafeArea();}
        private void OnDestroy(){if(battle!=null)battle.BattleStarted-=Refresh;}
        private void ApplySafeArea()
        {
            _lastArea=Screen.safeArea;_safe.anchorMin=new Vector2(_lastArea.xMin/Mathf.Max(1,Screen.width),_lastArea.yMin/Mathf.Max(1,Screen.height));_safe.anchorMax=new Vector2(_lastArea.xMax/Mathf.Max(1,Screen.width),_lastArea.yMax/Mathf.Max(1,Screen.height));_safe.offsetMin=_safe.offsetMax=Vector2.zero;
        }
        private void CycleGraphics()
        {
            environment.quality=(BattlefieldGraphics)(((int)environment.quality+1)%3);environment.GetComponent<BattlefieldQuality>().Apply();
            if(Application.isMobilePlatform){PlayerPrefs.SetInt(Preference,(int)environment.quality);PlayerPrefs.Save();}Refresh();
        }
        private void SwitchStage(){if(battle.Stages.Count>1)battle.StartBattle((battle.StageIndex+1)%battle.Stages.Count);Refresh();}
        private void Refresh()
        {
            if(_graphicsLabel==null)return;
            _graphicsLabel.text="Đồ họa: "+(environment.quality==BattlefieldGraphics.High?"Cao":environment.quality==BattlefieldGraphics.Medium?"Vừa":"Thấp");
            _stageLabel.text=battle.Stage!=null&&battle.Stage.StageType==StageType.Attack?"Chuyển ải Thủ":"Chuyển ải Công";
        }
    }
}
