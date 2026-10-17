using System;
using System.IO;
using Arena3Q.Core;
using Arena3Q.Data;
using UnityEngine;

namespace Arena3Q.Frontend
{
    /// <summary>
    /// The services behind the menu screens. Prototype defaults: a JSON profile on the device, offline accounts
    /// ("Chơi ngay" only) and the server list asset. Swap them here for the real backend; tests swap in memory ones.
    /// </summary>
    public static class FrontendServiceLocator
    {
        private static IPlayerProfileStore _profiles;
        private static IAccountService _accounts;
        private static IServerDirectory _servers;

        public static IPlayerProfileStore Profiles => _profiles ?? (_profiles = new JsonFileProfileStore(JsonFileProfileStore.DefaultPath));
        public static IAccountService Accounts => _accounts ?? (_accounts = new OfflineAccountService());

        public static IServerDirectory ServersFor(FrontendConfig config) =>
            _servers ?? new StaticServerDirectory(config != null && config.ServerList != null
                ? config.ServerList.CreateServers()
                : Array.Empty<ServerInfo>());

        /// <summary>Replaces some services (null = keep the current one).</summary>
        public static void Override(IPlayerProfileStore profiles = null, IAccountService accounts = null,
            IServerDirectory servers = null)
        {
            if (profiles != null) _profiles = profiles;
            if (accounts != null) _accounts = accounts;
            if (servers != null) _servers = servers;
        }

        /// <summary>Back to the defaults.</summary>
        public static void Reset()
        {
            _profiles = null;
            _accounts = null;
            _servers = null;
        }
    }

    /// <summary>The profile as a JSON file in the app's private storage (prototype; the backend replaces it later).</summary>
    public sealed class JsonFileProfileStore : IPlayerProfileStore
    {
        private readonly string _path;

        public JsonFileProfileStore(string path)
        {
            _path = path;
        }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "profile.json");

        public PlayerProfile Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(_path));
                    if (profile != null)
                        return profile;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Arena3Q] Could not read the profile ({exception.Message}); starting a new one.");
            }
            return new PlayerProfile();
        }

        public void Save(PlayerProfile profile)
        {
            // Write next to the file and swap, so a crash never leaves half a profile.
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(profile));
            if (File.Exists(_path))
                File.Delete(_path);
            File.Move(temp, _path);
        }

        public void Delete()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}
