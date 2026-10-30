#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Arena3Q.Data;
using Arena3Q.Frontend;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Arena3Q.Exploration.Tests
{
    public sealed partial class ChapterMapFlowTests
    {
        [UnityTest] public IEnumerator MapReviewRewardsTextAndModalAreReadableAndHaveTouchTargets()
        {
            var canvas=_map.Hud.GetComponentInChildren<Canvas>();
            var rewards=canvas.transform.Find("SafeArea/ChapterRewards");
            foreach(int total in new[]{0,7,21})
            {
                int remaining=total;
                foreach(var stage in _map.Config.stages){int stars=Mathf.Min(3,remaining);_map.Session.Progress.Get(stage.id).stars=stars;remaining-=stars;}
                _map.Hud.Refresh();
                for(int i=0;i<_map.Config.rewards.Length;i++)Assert.AreEqual(Mathf.Min(total,_map.Config.rewards[i].stars)+"/"+_map.Config.rewards[i].stars,rewards.Find("Reward_"+_map.Config.rewards[i].id+"/Stars").GetComponent<TMP_Text>().text);
            }
            var introduction=canvas.transform.Find("SafeArea/Introduction").GetComponent<TMP_Text>();
            Assert.Greater(introduction.fontSharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth),0);
            Assert.True(introduction.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON"));Assert.True(introduction.fontSharedMaterial.IsKeywordEnabled("UNDERLAY_ON"));
            Assert.AreNotSame(introduction.font.material,introduction.fontSharedMaterial,"Map contrast must not mutate the shared font asset.");
            foreach(var stage in _map.Config.stages.Where(s=>s.useAltarBanner))
            {
                int index=System.Array.IndexOf(_map.Config.stages,stage);
                var name=canvas.transform.Find("Marker_"+index+"_-1/RegionName").GetComponent<Image>();
                Assert.NotNull(_map.Config.ui.altarBanner);Assert.AreSame(_map.Config.ui.altarBanner,name.sprite);
            }
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1800,900),new Vector2Int(2000,900),new Vector2Int(2340,1080)})
            {
                Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=size.x,height=size.y,dpi=395,frameRate=60,left=.034f,right=.02f}));
                yield return null;
                _map.Hud.ShowStage(6);yield return new WaitForSecondsRealtime(_map.Config.frontend.StartupUi.modalSeconds+.15f);
                var modal=canvas.transform.Find("SafeArea/ChapterModal");var group=modal.GetComponent<CanvasGroup>();
                Assert.AreEqual(1,group.alpha,.001);Assert.True(group.interactable);
                var close=modal.Find("Panel/Close").GetComponent<Button>();Assert.NotNull(close.GetComponent<GameButton>());Assert.NotNull(close.image.sprite);
                var rect=(RectTransform)close.transform;
                Assert.GreaterOrEqual(rect.rect.height*canvas.scaleFactor*160/DisplayMetrics.Dpi,48);
                var text=modal.Find("Panel/Rewards").GetComponent<TMP_Text>();text.ForceMeshUpdate();
                Assert.AreEqual(_map.Config.ui.stageRewardsHeading+"\n"+_map.Config.stages[6].rewardPreview,text.text);Assert.False(text.isTextOverflowing);
                Assert.GreaterOrEqual(modal.Find("Panel").GetComponent<LobbyFrameGraphic>().fill.a,.95);
                _map.Hud.CloseModal();yield return new WaitForSecondsRealtime(_map.Config.frontend.StartupUi.modalSeconds+.05f);
            }
            DisplayMetrics.Reset();
        }
        [UnityTest] public IEnumerator CaptureMapTextOnCloudTreesGroundAndUnlockedAltar()
        {
            var field=typeof(ChapterMapWorld).GetField("_visibility",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var visibility=(float[])field.GetValue(_map.World);var saved=(float[])visibility.Clone();var focus=_map.View.Focus;
            var markers=(bool[])typeof(ChapterMapWorld).GetField("_markers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(_map.World);
            var savedMarkers=(bool[])markers.Clone();for(int i=0;i<markers.Length;i++)markers[i]=true;
            bool batching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=false;
                foreach(var stage in _map.Config.stages)_map.Session.Progress.Get(stage.id).stars=3;
                _map.Hud.Refresh();
                foreach(int rate in new[]{30,60,120})
                {
                    Assert.True(DisplayMetrics.TrySetPreview(new DevicePreviewSettings{width=1672,height=941,dpi=160,frameRate=rate}));
                    for(int backdrop=0;backdrop<3;backdrop++)
                    {
                        for(int i=0;i<visibility.Length;i++)visibility[i]=backdrop==0?0:1;
                        _map.View.FocusOn(_map.Config.stages[backdrop==2?1:0].center);
                        yield return new WaitForSecondsRealtime(.5f);Capture("ui-introduction-"+new[]{"cloud","trees","ground"}[backdrop]+"-"+rate);
                    }
                    _map.View.FocusOn(_map.Config.stages[6].center);yield return new WaitForSecondsRealtime(.5f);Capture("ui-altar-"+rate);
                }
                _map.Hud.ShowStage(6);yield return new WaitForSecondsRealtime(_map.Config.frontend.StartupUi.modalSeconds+.15f);Capture("ui-stage-panel-ready");
                _map.Hud.CloseModal();yield return new WaitForSecondsRealtime(_map.Config.frontend.StartupUi.modalSeconds+.05f);
            }
            finally{savedMarkers.CopyTo(markers,0);saved.CopyTo(visibility,0);_map.View.FocusOn(focus);DisplayMetrics.Reset();UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching;}
        }
    }
}
#endif
