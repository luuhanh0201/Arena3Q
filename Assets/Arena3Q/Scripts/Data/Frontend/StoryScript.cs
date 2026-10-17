using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>A story told with picture panels and a dialogue box (docs/KeHoach/VaoGame.md §10).</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Story Script", fileName = "Story")]
    public sealed class StoryScript : ScriptableObject
    {
        [Serializable]
        public sealed class Speaker
        {
            public string speakerId = "narrator";
            public string displayName = "";
            [Tooltip("Optional: a hero whose display name and colour the speaker uses (stays right when the hero is renamed).")]
            public HeroDefinition hero;
            [Tooltip("The player's lord: shows the player's name and the lord's portrait.")]
            public bool isPlayerLord;
            [Tooltip("Narration box without portrait or name plate.")]
            public bool isNarrator;
            [Tooltip("Enemies talk from the right side of the box, allies from the left.")]
            public bool rightSide;
            public Sprite portrait;
            public Color color = Color.white;
        }

        [Serializable]
        public sealed class Line
        {
            public string speakerId = "narrator";
            [Tooltip("{Tên} is replaced with the player's name.")]
            [TextArea] public string text = "";
        }

        [Serializable]
        public sealed class LordImage
        {
            public string lordHeroId = "";
            public Sprite image;
        }

        [Serializable]
        public sealed class Panel
        {
            public string panelId = "panel";
            public Sprite image;
            [Tooltip("A different picture for each lord (the panel that shows the player's lord).")]
            public LordImage[] lordImages = Array.Empty<LordImage>();
            [Tooltip("Placeholder while there is no picture.")]
            public Color placeholderColor = new Color(0.2f, 0.18f, 0.16f);
            [TextArea] public string placeholderCaption = "";
            public Line[] lines = Array.Empty<Line>();

            public Sprite ImageFor(string lordHeroId)
            {
                if (lordImages != null)
                {
                    foreach (var variant in lordImages)
                    {
                        if (variant != null && variant.lordHeroId == lordHeroId && variant.image != null)
                            return variant.image;
                    }
                }
                return image;
            }
        }

        [SerializeField] private Speaker[] speakers = Array.Empty<Speaker>();
        [SerializeField] private Panel[] panels = Array.Empty<Panel>();

        public IReadOnlyList<Panel> Panels => panels;

        public Speaker FindSpeaker(string speakerId)
        {
            foreach (var speaker in speakers)
            {
                if (speaker != null && speaker.speakerId == speakerId)
                    return speaker;
            }
            return null;
        }

        public IReadOnlyList<int> LinesPerPanel()
        {
            var counts = new List<int>(panels.Length);
            foreach (var panel in panels)
                counts.Add(panel?.lines?.Length ?? 0);
            return counts;
        }

        public void Validate(List<string> errors)
        {
            var owner = $"Story '{name}'";
            if (panels == null || panels.Length == 0)
                errors.Add($"{owner}: no panels.");
            else
            {
                foreach (var panel in panels)
                {
                    if (panel?.lines == null)
                        continue;
                    foreach (var line in panel.lines)
                    {
                        if (line != null && FindSpeaker(line.speakerId) == null)
                            errors.Add($"{owner}: panel '{panel.panelId}' uses unknown speaker '{line.speakerId}'.");
                    }
                }
            }
        }
    }
}
