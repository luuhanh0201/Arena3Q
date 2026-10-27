using System;
using Arena3Q.Frontend;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Arena3Q.Editor
{
    // Editor capture lighting must not inherit the sun or shadow casters of the currently open battle scene.
    internal sealed class LobbyCaptureStage : IDisposable
    {
        private readonly Scene _scene;
        public LobbyCaptureStage(LobbyLordView view)
        {
            _scene = EditorSceneManager.NewPreviewScene();
            SceneManager.MoveGameObjectToScene(view.Model.transform.root.gameObject, _scene);
            view.CaptureCamera.scene = _scene;
        }
        public void Dispose()
        {
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        }
    }
}
