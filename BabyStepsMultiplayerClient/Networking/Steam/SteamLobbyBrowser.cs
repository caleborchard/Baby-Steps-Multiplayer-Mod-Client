using BabyStepsNetworking.ServerBrowser;
using System.Collections.Generic;

namespace BabyStepsMultiplayerClient.Networking.Steam
{
    public sealed class SteamLobbyBrowser : SteamLobbyBrowserSource
    {
        public override event Action<IReadOnlyList<ServerInfo>>? ServersFound;

        private ulong _pendingCall = 0; 

        public override void Refresh()
        {
            NativeSteamAPI.AddLobbyFilter("bbs_game", "1");
            // Without worldwide distance filter Steam only returns lobbies in the same region.
            NativeSteamAPI.SetLobbyDistanceWorldwide();
            _pendingCall = NativeSteamAPI.RequestLobbyList();
            Core.logger.Msg($"[Steam Browser] RequestLobbyList fired, handle={_pendingCall}");
        }

        public void Poll()
        {
            if (_pendingCall == 0) return;
            if (!NativeSteamAPI.IsAPICallCompleted(_pendingCall, out bool failed)) return;

            ulong handle = _pendingCall;
            _pendingCall = 0;

            Core.logger.Msg($"[Steam Browser] LobbyList call completed. handle={handle} failed={failed}");

            if (failed)
            {
                Core.logger.Msg($"[Steam Browser] Call failed.");
                ServersFound?.Invoke(System.Array.Empty<ServerInfo>());
                return;
            }

            if (!NativeSteamAPI.GetAPICallResult<NativeSteamAPI.LobbyMatchList_t>(
                    handle, NativeSteamAPI.k_LobbyMatchList, out var list))
            {
                Core.logger.Msg($"[Steam Browser] GetAPICallResult returned false.");
                ServersFound?.Invoke(System.Array.Empty<ServerInfo>());
                return;
            }

            Core.logger.Msg($"[Steam Browser] m_nLobbiesMatching={list.m_nLobbiesMatching}");

            var results = new List<ServerInfo>((int)list.m_nLobbiesMatching);
            for (int i = 0; i < (int)list.m_nLobbiesMatching; i++)
            {
                ulong lobbyId = NativeSteamAPI.GetLobbyByIndex(i);
                Core.logger.Msg($"[Steam Browser] lobby[{i}] id={lobbyId}");
                if (lobbyId == 0) continue;

                string bbs     = NativeSteamAPI.GetLobbyData(lobbyId, "bbs_game");
                string name    = NativeSteamAPI.GetLobbyData(lobbyId, "bbs_name");
                string hostId  = NativeSteamAPI.GetLobbyData(lobbyId, "bbs_host");
                bool   locked  = NativeSteamAPI.GetLobbyData(lobbyId, "bbs_locked") == "1";
                Core.logger.Msg($"[Steam Browser]   bbs_game='{bbs}' bbs_name='{name}' bbs_host='{hostId}' locked={locked}");
                if (string.IsNullOrEmpty(name)) name = $"Game {lobbyId}";

                if (string.IsNullOrEmpty(hostId) || hostId == "0")
                {
                    ulong ownerId = NativeSteamAPI.GetLobbyOwner(lobbyId);
                    hostId = ownerId.ToString();
                    Core.logger.Msg($"[Steam Browser]   bbs_host missing, using GetLobbyOwner: {hostId}");
                }

                if (!ulong.TryParse(hostId, out ulong hostSteamId) || hostSteamId == 0)
                {
                    Core.logger.Warning($"[Steam Browser]   Could not resolve host SteamID for lobby {lobbyId}, skipping");
                    continue;
                }

                int.TryParse(NativeSteamAPI.GetLobbyData(lobbyId, "bbs_players"), out int players);

                results.Add(new ServerInfo
                {
                    Name                = (locked ? "🔒 " : "") + name,
                    Address             = hostSteamId.ToString(),
                    SessionId           = lobbyId.ToString(),
                    PlayerCount         = players,
                    MaxPlayers          = 16,
                    Type                = ServerType.SteamP2P,
                    IsPasswordProtected = locked,
                });
            }

            ServersFound?.Invoke(results);
        }
    }
}
