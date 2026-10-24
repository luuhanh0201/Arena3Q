using System.Linq;
using Arena3Q.Core;
using Arena3Q.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arena3Q.Tests.EditMode
{
    public sealed class StartupUiContentTests
    {
        private static StartupUiConfig Config => AssetDatabase.LoadAssetAtPath<StartupUiConfig>("Assets/Arena3Q/Content/Frontend/StartupUi.asset");
        [Test]
        public void SpriteBorders_MatchHandoff_AndEverySpriteIsSingle()
        {
            foreach (var pair in new[] { (Config.goldButton, new Vector4(72, 37, 72, 38)), (Config.blueButton, new Vector4(44, 24, 44, 24)),
                (Config.redButton, new Vector4(44, 25, 44, 26)), (Config.panel, Vector4.one * 44), (Config.largePanel, Vector4.one * 56) })
            {
                Assert.That(pair.Item1, Is.Not.Null);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(pair.Item1));
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.spriteBorder, Is.EqualTo(pair.Item2), pair.Item1.name);
                Assert.That(importer.mipmapEnabled, Is.False);
            }
        }
        [Test]
        public void CopyAndTips_HaveVietnamese_AndNoForbiddenUiCharacters()
        {
            Assert.That(Config.copy.Select(x => x.key).Distinct().Count(), Is.EqualTo(Config.copy.Length));
            foreach (var value in Config.copy.Select(x => x.value).Concat(Config.tips))
            {
                Assert.That(value.Contains('\u2013') || value.Contains('\u2014'), Is.False, value);
                Assert.That(value.Any(char.IsSurrogate), Is.False, value);
                Assert.That(value.IndexOfAny(new[] { '\u2605', '\u2713', '\u26a0' }), Is.EqualTo(-1), value);
            }
            Assert.That(Config.tips.Any(x => x.Contains("Kim, Mộc")), Is.False, "no unsupported elemental gameplay tip");
        }
        [Test]
        public void FontSources_IncludeVietnameseInAllFourWeights()
        {
            foreach (var font in new[] { Config.regular, Config.semiBold, Config.bold, Config.extraBold })
            {
                Assert.That(font, Is.Not.Null);
                Assert.That(font.sourceFontFile, Is.Not.Null);
                foreach (var c in "Đăng nhập Chọn máy chủ Tốt Đông Đầy Bảo trì ẮẰẶẤẦẬẾỀỆỐỒỘỚỜỢỨỪỰỸ₫")
                    Assert.That(font.sourceFontFile.HasCharacter(c), Is.True, font.name + ": " + c);
            }
        }
        [Test]
        public void Music_IsBoundQuietAndStreamed_AndFlowKeepsSerializedStepValues()
        {
            Assert.That(Config.music, Is.Not.Null, "restore the user's local OGG before testing/building");
            Assert.That(Config.musicVolume, Is.InRange(.01f, .2f));
            Assert.That(Config.music.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
            var flow = AssetDatabase.LoadAssetAtPath<FrontendConfig>("Assets/Arena3Q/Content/Frontend/FrontendConfig.asset");
            Assert.That(flow.Steps.Take(3), Is.EqualTo(new[] { FrontendStep.Intro, FrontendStep.Loading, FrontendStep.Login }));
            Assert.That((int)FrontendStep.Login, Is.EqualTo(1));
        }
    }
}
