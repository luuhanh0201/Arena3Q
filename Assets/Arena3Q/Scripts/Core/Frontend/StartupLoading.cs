using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    public enum StartupNetState { Good, Weak, Stalled, Lost, ServerTimeout, Retrying }

    [Serializable]
    public sealed class StartupNetworkSettings
    {
        public int weakPingMs = 150;
        public double weakBytesPerSecond = 300 * 1024;
        public float stalledSeconds = 3, requestTimeoutSeconds = 5;
        public float[] retrySeconds = { 2, 4, 8 };
    }

    public readonly struct StartupProgress
    {
        public StartupProgress(float progress, StartupNetState state = StartupNetState.Good,
            double bytesPerSecond = 0, int attempt = 0, bool local = false, bool complete = false)
        {
            Progress = Math.Max(0, Math.Min(1, progress)); State = state;
            BytesPerSecond = bytesPerSecond; Attempt = attempt; IsLocal = local; Complete = complete;
        }
        public float Progress { get; }
        public StartupNetState State { get; }
        public double BytesPerSecond { get; }
        public int Attempt { get; }
        public bool IsLocal { get; }
        public bool Complete { get; }
        public bool IsPaused => State != StartupNetState.Good && State != StartupNetState.Weak;
    }

    /// <summary>Telemetry rules only. A backend adapter supplies actual bytes/ping and owns resumable storage.</summary>
    public sealed class StartupNetworkMonitor
    {
        private readonly StartupNetworkSettings _settings;
        private readonly Queue<(double seconds, long bytes)> _samples = new Queue<(double, long)>();
        private double _clock, _lastByteTime, _retryClock, _sampleSeconds;
        private long _windowBytes;
        public StartupNetworkMonitor(StartupNetworkSettings settings) { _settings = settings ?? new StartupNetworkSettings(); }
        public StartupNetState State { get; private set; }
        public int Attempt { get; private set; }
        public double BytesPerSecond => _sampleSeconds > 0 ? _windowBytes / _sampleSeconds : 0;
        public bool RetryDue { get; private set; }

        public void Observe(float deltaTime, long receivedBytes, bool reachable, int pingMs, float requestAgeSeconds = 0)
        {
            var dt = Math.Max(0, deltaTime);
            _clock += dt; RetryDue = false;
            if (dt > 0)
            {
                var bytes = Math.Max(0, receivedBytes);
                _samples.Enqueue((dt, bytes)); _sampleSeconds += dt; _windowBytes += bytes;
                while (_samples.Count > 1 && _sampleSeconds - _samples.Peek().seconds >= 3)
                {
                    var old = _samples.Dequeue(); _sampleSeconds -= old.seconds; _windowBytes -= old.bytes;
                }
            }
            if (!reachable) { State = StartupNetState.Lost; return; }
            if (receivedBytes > 0)
            {
                _lastByteTime = _clock; Attempt = 0; _retryClock = 0;
                State = pingMs >= _settings.weakPingMs || BytesPerSecond < _settings.weakBytesPerSecond
                    ? StartupNetState.Weak : StartupNetState.Good;
                return;
            }
            if (State == StartupNetState.Lost || State == StartupNetState.ServerTimeout) return;
            if (requestAgeSeconds > _settings.requestTimeoutSeconds) { State = StartupNetState.ServerTimeout; return; }
            if (_clock - _lastByteTime < _settings.stalledSeconds) return;
            var wasWaiting = State == StartupNetState.Stalled || State == StartupNetState.Retrying;
            State = State == StartupNetState.Retrying ? StartupNetState.Retrying : StartupNetState.Stalled;
            _retryClock += wasWaiting ? dt : Math.Min(dt, Math.Max(0, _clock - _lastByteTime - _settings.stalledSeconds));
            var delays = _settings.retrySeconds ?? Array.Empty<float>();
            if (Attempt >= delays.Length) { State = StartupNetState.Lost; return; }
            if (_retryClock >= delays[Attempt])
            {
                _retryClock = 0; Attempt++; RetryDue = true;
            }
        }

        public void Retry()
        {
            State = StartupNetState.Retrying; Attempt = 0; _retryClock = 0;
            _lastByteTime = _clock; _samples.Clear(); _windowBytes = 0; _sampleSeconds = 0;
        }
    }

    /// <summary>Owns preparation/cache lifetime. Retry must preserve completed work. No invented network endpoints.</summary>
    public interface IStartupPreparation : IDisposable
    {
        StartupProgress Current { get; }
        void Tick(float deltaTime);
        void Retry();
    }

    public interface IAccountActions
    {
        void RequestVerification(string address, Action<string> done);
        void ResetPassword(string address, Action<string> done);
        void Link(string accountId, AccountKind provider, string address, string password, string code, Action<SignInResult> done);
        void RegisterVerified(string address, string password, string code, Action<SignInResult> done);
    }

    public sealed class OfflineAccountActions : IAccountActions
    {
        public void RequestVerification(string address, Action<string> done) => done?.Invoke(OfflineAccountService.NotAvailableMessage);
        public void ResetPassword(string address, Action<string> done) => done?.Invoke(OfflineAccountService.NotAvailableMessage);
        public void Link(string accountId, AccountKind provider, string address, string password, string code, Action<SignInResult> done) =>
            done?.Invoke(SignInResult.Fail(OfflineAccountService.NotAvailableMessage));
        public void RegisterVerified(string address, string password, string code, Action<SignInResult> done) =>
            done?.Invoke(SignInResult.Fail(OfflineAccountService.NotAvailableMessage));
    }
}
