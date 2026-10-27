using System;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>Editable lobby geometry in the shared 1280 x 720 design space.</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Lobby Layout")]
    public sealed class LobbyLayoutConfig : ScriptableObject
    {
        [Serializable] public sealed class Element
        {
            public string id;
            public Rect bounds;
            public float fontSize;
            public Element(string id, Rect bounds, float fontSize = 0) { this.id = id; this.bounds = bounds; this.fontSize = fontSize; }
        }
        public Element[] elements =
        {
            new Element("EdgeGroup", new Rect(0, 0, 1280, 720)),
            new Element("Hero", new Rect(0, 0, 600, 600)),
            new Element("MissingModel", new Rect(140, 300, 320, 70), 18),
            new Element("LordNameplate", new Rect(0, 0, 112, 42)),
            new Element("LordHeading", new Rect(4, 3, 104, 36), 18),
            new Element("Player", new Rect(12, 3, 390, 204)),
            new Element("VipBackground", new Rect(-6, 90, 382, 160)),
            new Element("AvatarBorder", new Rect(0, 36, 102, 102)),
            new Element("AvatarMask", new Rect(3, 3, 96, 96)),
            new Element("LordAvatar", new Rect(0, 0, 96, 96)),
            new Element("PlayerName", new Rect(106, 72, 172, 36), 20),
            new Element("PlayerStats", new Rect(64, 118, 317, 29)),
            new Element("Level", new Rect(23, 2, 49, 25), 14),
            new Element("ExperienceBorder", new Rect(70, 9, 147, 10)),
            new Element("PowerRibbon", new Rect(56, 150, 319, 42)),
            new Element("Power", new Rect(64, 150, 303, 42), 18),
            new Element("VipBadge", new Rect(243, -1, 75, 31)),
            new Element("VipLabel", new Rect(5, 0, 65, 31), 14),
            new Element("Resource", new Rect(505, 12, 152, 40)),
            new Element("ResourceIcon", new Rect(-12, -5, 44, 44)),
            new Element("ResourceAmount", new Rect(29, 4, 90, 32), 17),
            new Element("ResourceHint", new Rect(-13, -5, 131, 45)),
            new Element("ResourceAdd", new Rect(120, 6, 27, 28)),
            new Element("ResourcePlus", new Rect(6, 6, 15, 16)),
            new Element("CornerButton", new Rect(1142, 34, 42, 42)),
            new Element("CornerIcon", new Rect(3, 3, 36, 36)),
            new Element("LeftNavigation", new Rect(14, 205, 73, 405)),
            new Element("LeftButton", new Rect(4, 8, 65, 74)),
            new Element("LeftIcon", new Rect(13, 0, 40, 42)),
            new Element("LeftCaption", new Rect(0, 42, 65, 25), 13),
            new Element("LeftDivider", new Rect(12, 81, 49, 1)),
            new Element("BottomNavigation", new Rect(30, 613, 735, 92)),
            new Element("BottomButton", new Rect(15, 6, 112, 78)),
            new Element("BottomSelected", new Rect(0, -3, 112, 83)),
            new Element("BottomIcon", new Rect(30, 0, 52, 52)),
            new Element("BottomSelectedIcon", new Rect(23, 0, 66, 52)),
            new Element("BottomCaption", new Rect(3, 51, 106, 25), 16),
            new Element("BottomDivider", new Rect(10, 41, 5, 5)),
            new Element("EnterBattle", new Rect(891, 545, 380, 169)),
            new Element("BattleCaption", new Rect(70, 81, 228, 62), 34),
            new Element("BattleChevron", new Rect(309, 101, 22, 30)),
            new Element("ResourceTooltip", new Rect(0, 0, 330, 58)),
            new Element("TooltipFrame", new Rect(0, 0, 330, 58)),
            new Element("TooltipText", new Rect(15, 7, 300, 44), 15),
            new Element("UnavailablePanel", new Rect(400, 230, 480, 240)),
            new Element("UnavailableTitle", new Rect(35, 35, 410, 40), 25),
            new Element("UnavailableBody", new Rect(35, 85, 410, 55), 18),
            new Element("UnavailableClose", new Rect(155, 164, 170, 50))
        };
        public Rect[] banners = { new Rect(908, 80, 355, 153), new Rect(908, 242, 355, 153), new Rect(908, 404, 173, 133), new Rect(1090, 404, 173, 133) };
        public Vector2 resourceStep = new Vector2(159, 0), cornerStep = new Vector2(55, 0), leftStep = new Vector2(0, 77), bottomStep = new Vector2(118, 0), starStep = new Vector2(0, 16);
        public Vector2 cornerNotification = new Vector2(31, -1), leftNotification = new Vector2(48, 0), bottomNotification = new Vector2(78, 1);
        public float notificationSize = 13, headingLineSpacing = 6, backdropAspect = 16f / 9;
        public float experienceInset = 1;
        public float playerNameMinimumFont = 14, playerFrameCorner = 10, playerFrameStroke = 1.2f;
        public float lordPlateCorner = 10, lordPlateStroke = 1.6f;
        public Vector2 lordPlateAnchor = new Vector2(.14f, .55f);
        public Vector2 backdropPivot = new Vector2(.5f, 0);
        public Vector2 iconShadowOffset = new Vector2(1.5f, -2), iconOutlineOffset = new Vector2(1.2f, 1.2f);
        public float navCorner = 32, navStroke = 2, selectedCorner = 26, selectedStroke = 2.5f, dividerRotation = 45;
        public float tooltipPadding = 8, tooltipGap = 7, tooltipFontMin = 12, tooltipPixelsPerUnit = 3.5f;
        public float unavailableDimAlpha = .6f;
        public float bannerOverlayInset = 3, bannerOverlayStart = .3f, bannerOverlayHeight = .68f;
        public Rect bannerTagBacking = new Rect(8, 5, 104, 25), bannerTag = new Rect(22, 5, 76, 25);
        public float bannerTextInset = 13, bannerTextRightInset = 40, bannerSubtitleRightInset = 43, bannerTitleBottom = 81, bannerTitleHeight = 43;
        public float eventTitleBottom = 112, eventTitleWidth = 225, eventTitleHeight = 74;
        public float bannerSubtitleBottom = 39, bannerSubtitleHeight = 32;
        public Vector2 bannerChevronSize = new Vector2(21, 25), bannerChevronInset = new Vector2(29, 34);
        public float bannerTagFont = 13, bannerTitleFont = 25, smallBannerTitleFont = 21, bannerSubtitleFont = 15, smallBannerSubtitleFont = 12;

        public Element Find(string id) => Array.Find(elements, element => element.id == id)
            ?? throw new InvalidOperationException($"Lobby layout '{name}' is missing '{id}'.");
        public Rect Bounds(string id, Vector2 offset = default)
        {
            var rect = Find(id).bounds; rect.position += offset; return rect;
        }
        public float Font(string id) => Find(id).fontSize;
    }
}
