using Arena3Q.Data;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Editor
{
    /// <summary>One editable release manifest for mobile and web; source rules stay in their original assets.</summary>
    public sealed class GameReleaseSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Arena3Q/Scripts/Editor/GameRelease.asset";
        public FrontendConfig frontend;
        public SceneAsset[] scenes;
        public StageConfig tutorialSource, attackSource, defenseSource;
        public StageConfig attackLayout, defenseLayout;
        public string campaignFolder = "Assets/Arena3Q/Content/Battlefield/Campaign";
    }
}
