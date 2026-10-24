using System;
using System.Collections.Generic;

namespace Arena3Q.Core
{
    public sealed class SignInResult
    {
        public bool Success { get; set; }
        public string AccountId { get; set; } = "";
        public AccountKind Kind { get; set; }
        /// <summary>Username, e-mail or "Khách", shown on the "tap to enter" screen.</summary>
        public string Label { get; set; } = "";
        /// <summary>Why it failed, for the player.</summary>
        public string Message { get; set; } = "";

        public static SignInResult Fail(string message) => new SignInResult { Message = message };
    }

    /// <summary>
    /// Sign-in and registration. Calls finish through the callback so a real implementation can talk to a server
    /// without changing the screens.
    /// </summary>
    public interface IAccountService
    {
        bool Supports(AccountKind kind);
        void SignIn(AccountKind kind, string username, string password, Action<SignInResult> done);
        void Register(string username, string password, Action<SignInResult> done);
    }

    /// <summary>
    /// Prototype accounts: "Chơi ngay" (guest bound to the device) works; Google, Apple and game accounts need the
    /// backend and answer that they are not available yet (spec §11 ⏳).
    /// </summary>
    public sealed class OfflineAccountService : IAccountService
    {
        public const string NotAvailableMessage = "Chưa hỗ trợ ở bản thử nghiệm (cần máy chủ). Hãy dùng \"Chơi ngay\".";
        public const string GuestLabel = "Khách";

        public bool Supports(AccountKind kind) => kind == AccountKind.Guest;

        public void SignIn(AccountKind kind, string username, string password, Action<SignInResult> done)
        {
            if (kind != AccountKind.Guest)
            {
                done?.Invoke(SignInResult.Fail(NotAvailableMessage));
                return;
            }
            done?.Invoke(new SignInResult
            {
                Success = true,
                AccountId = "guest-" + Guid.NewGuid().ToString("N"),
                Kind = AccountKind.Guest,
                Label = GuestLabel
            });
        }

        public void Register(string username, string password, Action<SignInResult> done) =>
            done?.Invoke(SignInResult.Fail(NotAvailableMessage));
    }

    public enum ServerStatus
    {
        New,
        Busy,
        Maintenance,
        Good,
        Full
    }

    public sealed class ServerInfo
    {
        public ServerInfo(string id, string name, ServerStatus status, bool recommended, bool isNew = false, bool isHot = false)
        {
            Id = id;
            Name = name;
            Status = status;
            Recommended = recommended;
            IsNew = isNew || status == ServerStatus.New;
            IsHot = isHot;
        }

        public string Id { get; }
        public string Name { get; }
        public ServerStatus Status { get; }
        public bool Recommended { get; }
        public bool IsNew { get; }
        public bool IsHot { get; }
        public bool CanEnter => Status != ServerStatus.Maintenance && Status != ServerStatus.Full;
    }

    /// <summary>The server list. Prototype: from an asset; later: fetched from the backend.</summary>
    public interface IServerDirectory
    {
        void Fetch(Action<IReadOnlyList<ServerInfo>> done);
    }

    /// <summary>A fixed server list (prototype and tests).</summary>
    public sealed class StaticServerDirectory : IServerDirectory
    {
        private readonly IReadOnlyList<ServerInfo> _servers;

        public StaticServerDirectory(IReadOnlyList<ServerInfo> servers)
        {
            _servers = servers ?? Array.Empty<ServerInfo>();
        }

        public void Fetch(Action<IReadOnlyList<ServerInfo>> done) => done?.Invoke(_servers);

        /// <summary>The server a new player lands on: the recommended one that can be entered, else the first open one.</summary>
        public static ServerInfo PickDefault(IReadOnlyList<ServerInfo> servers, string lastServerId)
        {
            if (servers == null)
                return null;
            ServerInfo recommended = null, open = null;
            foreach (var server in servers)
            {
                if (server == null || !server.CanEnter)
                    continue;
                if (server.Id == lastServerId)
                    return server;
                if (server.Recommended && recommended == null)
                    recommended = server;
                if (open == null)
                    open = server;
            }
            return recommended ?? open;
        }
    }
}
