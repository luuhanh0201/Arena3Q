using Arena3Q.Core;
using Arena3Q.Data;
using TMPro;
using UnityEngine;

namespace Arena3Q.Frontend
{
    /// <summary>Prepares bundled UI resources. No network is required and no download speed is fabricated.</summary>
    public sealed class LocalStartupPreparation : IStartupPreparation
    {
        private readonly StartupUiConfig _config;
        private int _prepared; private float _elapsed; private bool _disposed;
        public LocalStartupPreparation(StartupUiConfig config) { _config = config; }
        public StartupProgress Current => new StartupProgress(_prepared / 4f, local: true, complete: _prepared == 4);
        public void Tick(float deltaTime)
        {
            if (_disposed || _prepared == 4) return;
            _elapsed += Mathf.Max(0, deltaTime);
            var budget = Mathf.FloorToInt(_elapsed / Mathf.Max(.01f, _config.localPreparationSeconds / 4));
            while (_prepared < Mathf.Min(4, budget))
            {
                TMP_FontAsset font = _prepared == 0 ? _config.regular : _prepared == 1 ? _config.semiBold : _prepared == 2 ? _config.bold : _config.extraBold;
                if (font != null) font.ReadFontAssetDefinition();
                _prepared++;
            }
        }
        public void Retry() { /* Completed bundled work is preserved. */ }
        public void Dispose() => _disposed = true;
    }
}
