#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Arena3Q.Core;
using Arena3Q.Frontend;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace Arena3Q.Lobby2D.Tests
{
    public sealed class LobbyAndroidSmokeTests
    {
        private LobbyAndroidTestHost _host;
        private SpriteAtlas _atlas;private bool _included;
        [UnitySetUp] public IEnumerator Load()
        {
            FrontendServiceLocator.Override(profiles:new InMemoryProfileStore());
            const string atlasPath="Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LegacyLobbyReview.spriteatlas";
            _atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if(_atlas==null)
            {
                AssetDatabase.CopyAsset("Assets/Arena3Q/Lobby2D/Unity/SpriteAtlases/LobbyUI.spriteatlas",atlasPath);
                _atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath); _atlas.SetIncludeInBuild(false);
            }
            _included=new SerializedObject(_atlas).FindProperty("m_EditorData.bindAsDefault").boolValue;_atlas.SetIncludeInBuild(true);
            SpriteAtlasUtility.PackAtlases(new[]{_atlas},EditorUserBuildSettings.activeBuildTarget);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Arena3Q/Lobby2D/Unity/LobbyAndroidTest.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            _host=Object.FindAnyObjectByType<LobbyAndroidTestHost>();Assert.That(_host,Is.Not.Null);
            Assert.That(_host.view.SafeRoot,Is.Not.Null);Assert.That(Application.CanStreamedLevelBeLoaded(_host.settings.gameScene),Is.True);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            FrontendServiceLocator.Reset();FrontendController.ResumeAt=null;
            var transition=Object.FindAnyObjectByType<Arena3Q.UI.SceneTransition>();if(transition)Object.Destroy(transition.gameObject);
            var current=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("LobbyTestCleanup");SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(current);
            _atlas.SetIncludeInBuild(_included);
        }
        private void Tap(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var point=RectTransformUtility.WorldToScreenPoint(_host.view.previewCamera,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(button),"No overlay intercepts the button");
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator MotionButtonsAdvanceAndAttackRecoversOnce()
        {
            var hero=_host.view.GetComponentInChildren<LuBuCutoutView>();var impacts=0;hero.Impact+=()=>impacts++;
            Tap(_host.view.SafeRoot.Find("Motion1").GetComponent<Button>());yield return new WaitForSecondsRealtime(.25f);
            Assert.That(hero.clipIndex,Is.EqualTo(1));Assert.That(hero.clock,Is.GreaterThan(0));
            Tap(_host.view.SafeRoot.Find("Motion2").GetComponent<Button>());yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(hero.clipIndex,Is.EqualTo(0));Assert.That(impacts,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator TimeButtonsAndNativeControlsRender()
        {
            Tap(_host.view.SafeRoot.Find("Time4").GetComponent<Button>());Assert.That(_host.view.TimeIndex,Is.EqualTo(4));
            Tap(_host.view.SafeRoot.Find("Time0").GetComponent<Button>());yield return null;
            Assert.That(_host.view.SafeRoot.Find("FrameRate"),Is.Not.Null);
            var hero=_host.view.GetComponentInChildren<LuBuCutoutView>();hero.animate=false;hero.Sample(0,0);
            Directory.CreateDirectory("NghiemThu/UI/Lobby2D_Android_20261027");
            var camera=_host.view.previewCamera;var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
            try{camera.targetTexture=rt;camera.aspect=16f/9;_host.view.SafeAreaOverride=new Rect(0,0,1920,1080);var scaler=_host.view.GetComponent<CanvasScaler>();scaler.enabled=false;scaler.enabled=true;Canvas.ForceUpdateCanvases();_host.view.ApplySafeArea();_host.view.AlignBackgrounds();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes("NghiemThu/UI/Lobby2D_Android_20261027/NativeControls.png",image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.Destroy(image);}
        }
        [UnityTest] public IEnumerator BattleButtonLoadsActualFrontend()
        {
            var expected=_host.settings.gameScene;Tap(_host.view.SafeRoot.Find("Battle").GetComponent<Button>());
            var until=Time.realtimeSinceStartup+60;while(SceneManager.GetActiveScene().name!=expected&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(expected));yield return null;
            Assert.That(Object.FindAnyObjectByType<FrontendController>(),Is.Not.Null);
        }
    }
}
#endif
