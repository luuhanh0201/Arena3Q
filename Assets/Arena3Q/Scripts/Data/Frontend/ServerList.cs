using System;
using System.Collections.Generic;
using Arena3Q.Core;
using UnityEngine;

namespace Arena3Q.Data
{
    /// <summary>The prototype's server list (later fetched from the backend).</summary>
    [CreateAssetMenu(menuName = "Arena3Q/Frontend/Server List", fileName = "ServerList")]
    public sealed class ServerList : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string serverId = "s1";
            public string displayName = "S1";
            public ServerStatus status = ServerStatus.New;
            public bool recommended;
            public bool isNew, isHot;
        }

        [SerializeField] private Entry[] servers = Array.Empty<Entry>();

        public IReadOnlyList<ServerInfo> CreateServers()
        {
            var list = new List<ServerInfo>();
            foreach (var entry in servers)
            {
                if (entry != null)
                    list.Add(new ServerInfo(entry.serverId, entry.displayName, entry.status, entry.recommended, entry.isNew, entry.isHot));
            }
            return list;
        }

        public void Validate(List<string> errors)
        {
            var owner = $"Server list '{name}'";
            var ids = new HashSet<string>();
            var open = false;
            foreach (var entry in servers)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.serverId))
                {
                    errors.Add($"{owner}: a server has no id.");
                    continue;
                }
                if (!ids.Add(entry.serverId))
                    errors.Add($"{owner}: '{entry.serverId}' is listed twice.");
                open |= entry.status != ServerStatus.Maintenance && entry.status != ServerStatus.Full;
            }
            if (!open)
                errors.Add($"{owner}: no server is open.");
        }
    }
}
