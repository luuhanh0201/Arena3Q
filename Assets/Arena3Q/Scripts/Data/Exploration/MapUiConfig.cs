using UnityEngine;

namespace Arena3Q.Data
{
    [CreateAssetMenu(menuName = "Arena3Q/Exploration/Map UI")]
    public sealed class MapUiConfig : ScriptableObject
    {
        public Vector2 referenceSize = new Vector2(1672, 941);
        public Rect back = new Rect(16, 10, 84, 54), title = new Rect(110, 8, 440, 54), info = new Rect(550, 12, 48, 48);
        public Rect introduction = new Rect(33, 78, 480, 82), rewards = new Rect(25, 165, 420, 80);
        public Rect quest = new Rect(25, 290, 340, 110), resource = new Rect(-510, 8, 240, 52);
        public Vector2 resourceStep = new Vector2(255, 0);
        public string[] resourceIds = { "Stamina", "Silver" };
        public Vector2 modalSize = new Vector2(570, 340);
        public float titleFont = 30, bodyFont = 18, markerSize = 56, bossScale = 1.25f, stageSize = 56, markerLift = 30, nameLift = 85;
        public float minimumTapDp = 56, nameWidth = 220, nameHeight = 36, occludedAlpha = .4f;
        public float minimumNameScale = .85f, maximumNameScale = 1.15f, markerStartScale = .8f;
        public float stagePulseScale = .04f, messageSeconds = 4;
        public int textWeight = 400;
        public Rect questText = new Rect(14, 12, 258, 86), questArrow = new Rect(284, 35, 34, 34);
        public Color gold = new Color(.95f, .81f, .59f), ink = new Color(.22f, .12f, .06f), boss = new Color(.65f, .19f, .11f), locked = Color.gray;
        public Sprite question, swords, backIcon, infoIcon, lockIcon, checkIcon, medallion, bossMedallion, nameplate;
        public Sprite[] chests = new Sprite[3];
        public Rect rewardBadge = new Rect(-2, -1, 84, 84), rewardHeading = new Rect(10, 20, 64, 44);
        public Rect rewardButton = new Rect(94, 0, 94, 80), rewardArt = new Rect(17, 4, 60, 48);
        public Rect rewardStar = new Rect(7, 53, 20, 20), rewardCount = new Rect(30, 50, 60, 26);
        public float rewardStep = 100;
        public float rewardHeadingFont = 15;
        public Material routeMaterial;
        public float routeWidth = .34f, routeLift = .06f, routeDashLength = 1.35f, routeDashFill = .62f;
        public float routeRayHeight = 80;
        public int routeCurveSteps = 8;
        public Color routeColor = new Color(1, .78f, .32f), routeOutline = new Color(.17f, .09f, .035f);
        public string unavailable = "Chức năng đang phát triển", lockedMessage = "Hoàn thành ải trước để mở khu vực này.", stuckMessage = "Chưa tìm được đường. Hãy chọn một vị trí khác.";
    }
}
