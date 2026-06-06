using BabyStepsMultiplayerClient.Components;
using BabyStepsMultiplayerClient.Networking;
using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.UI;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using System.Collections;
using System.Reflection;
using UnityEngine;

[assembly: MelonInfo(typeof(BabyStepsMultiplayerClient.Core),
    "BabyStepsMultiplayerClient",
    "1.3.1",
    "Caleb Orchard",
    "https://github.com/caleborchard/Baby-Steps-Multiplayer-Mod-Client")]
[assembly: MelonGame("DefaultCompany", "BabySteps")]

namespace BabyStepsMultiplayerClient
{
    public class Core : MelonMod
    {
        public const string SERVER_VERSION = "108";
        public static string CLIENT_VERSION;

        public const string cloneText = "(Clone)";

        public static MelonLogger.Instance logger;

        public static UIManager uiManager;
        public static NetworkManager networkManager;
        public static InGameHost inGameHost;

        // Fired on the main thread whenever the connection goes up or down
        public static Action OnConnectionStateChanged;

        // SteamID64 to auto-connect to from a game invite; 0 = none pending.
        private static ulong  _pendingInviteConnect = 0;
        // Invite key read from lobby metadata lets invited players bypass a password.
        private static string _pendingInviteKey = string.Empty;

        private static float  _continueBtnFirstSeenAt = -1f;
        private static float  _lastContinueClickAt = -10f;

        private static ulong _pendingJoinLobbyCall = 0;
        private static ulong _pendingJoinLobbyId   = 0;

        public override void OnLateInitializeMelon()
        {
            CLIENT_VERSION = Info.Version;

            logger = LoggerInstance;

            try
            {
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                HarmonyInstance.PatchAll(typeof(BabyStepsMenuLib.MenuInjectionLibrary).Assembly);
            }
            catch (Exception ex)
            {
                logger.Error($"Harmony patching failed: {ex}");
            }

            ManagedEnumerator.Register();

            // Initialize our own Facepunch instance before subscribing to its events.
            try
            {
                Steamworks.SteamClient.Init(0, asyncCallbacks: false);
                logger.Msg("[Steam] Facepunch initialized for invite callbacks");
            }
            catch (Exception ex) when (
                ex.Message.Contains("already initialized") ||
                ex.Message.Contains("SteamAPI_SteamInput") ||
                ex.Message.Contains("entry point") ||
                ex.Message.Contains("SteamInput"))
            {
                logger.Msg($"[Steam] Facepunch init note: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.Warning($"[Steam] Facepunch init failed: {ex.Message}");
            }

            uiManager = new();
            networkManager = new();

            MultiplayerMenu.Initialize();

            Networking.Steam.NativeSteamAPI.RegisterLobbyJoinRequestedCallback(lobbyId =>
            {
                logger.Msg($"[Steam] Lobby invite accepted — lobbyId={lobbyId}, calling JoinLobby to fetch host ID…");
                if (_pendingJoinLobbyCall != 0)
                    logger.Warning($"[Steam] Overwriting a pending JoinLobby call ({_pendingJoinLobbyCall}) for lobby {_pendingJoinLobbyId}");
                _pendingJoinLobbyCall = Networking.Steam.NativeSteamAPI.JoinLobby(lobbyId);
                _pendingJoinLobbyId   = lobbyId;
                logger.Msg($"[Steam] JoinLobby handle={_pendingJoinLobbyCall} (will poll for result in OnUpdate)");
            });

            Networking.Steam.NativeSteamAPI.RegisterJoinRequestedCallback(connectStr =>
            {
                logger.Msg($"[Steam] GameRichPresenceJoinRequested fired — connectStr='{connectStr}'");
                if (ulong.TryParse(connectStr.Trim(), out ulong hostId) && hostId != 0)
                {
                    logger.Msg($"[Steam] Game invite accepted — queuing connect to {hostId}");
                    _pendingInviteConnect = hostId;
                }
                else
                    logger.Warning($"[Steam] GameRichPresenceJoinRequested: could not parse '{connectStr}' as SteamID64");
            });

            try
            {
                Steamworks.SteamFriends.OnGameLobbyJoinRequested += (lobby, friend) =>
                {
                    ulong lid = lobby.Id;
                    logger.Msg($"[Steam] Facepunch GameLobbyJoinRequested lid={lid} friend={friend.Value}");
                    if (_pendingJoinLobbyCall != 0)
                        logger.Warning($"[Steam] Overwriting pending JoinLobby {_pendingJoinLobbyCall}");
                    _pendingJoinLobbyCall = Networking.Steam.NativeSteamAPI.JoinLobby(lid);
                    _pendingJoinLobbyId   = lid;
                    logger.Msg($"[Steam] JoinLobby handle={_pendingJoinLobbyCall}");
                };
                logger.Msg("[Steam] Facepunch OnGameLobbyJoinRequested hooked");
            }
            catch (Exception ex) { logger.Msg($"[Steam] Facepunch GameLobbyJoinRequested hook: {ex.Message}"); }

            try
            {
                // friend is Steamworks.Friend (has .Id.Value, not .Value directly)
                Steamworks.SteamFriends.OnGameRichPresenceJoinRequested += (friend, connectStr) =>
                {
                    logger.Msg($"[Steam] Facepunch GameRichPresenceJoinRequested connectStr='{connectStr}' friend={friend.Id.Value}");
                    if (ulong.TryParse(connectStr.Trim(), out ulong hostId) && hostId != 0)
                        _pendingInviteConnect = hostId;
                    else
                        logger.Warning($"[Steam] Facepunch GameRichPresenceJoinRequested: could not parse '{connectStr}'");
                };
                logger.Msg("[Steam] Facepunch OnGameRichPresenceJoinRequested hooked");
            }
            catch (Exception ex) { logger.Msg($"[Steam] Facepunch GameRichPresenceJoinRequested hook: {ex.Message}"); }

            // Check if we were launched from a game invite (cold start path).
            try
            {
                string launchCmd = Networking.Steam.NativeSteamAPI.GetLaunchCommandLine().Trim();
                if (!string.IsNullOrEmpty(launchCmd) &&
                    ulong.TryParse(launchCmd, out ulong coldHostId) && coldHostId != 0)
                {
                    logger.Msg($"[Steam] Launched from game invite, will connect to {coldHostId}");
                    _pendingInviteConnect = coldHostId;
                }
            }
            catch { /* GetLaunchCommandLine unavailable in this build */ }

            logger.Msg("Initialized!");

            VersionCheck.CheckForUpdate();
        }

        public override void OnGUI()
        {
            if (uiManager == null)
                return;

            uiManager.Draw();
        }

        public override void OnUpdate()
        {
            if (uiManager == null || networkManager == null)
                return;

            uiManager.Update();
            networkManager.Update();
            inGameHost?.Tick(Time.deltaTime * 1000f);
            MultiplayerMenu.Update();

            if (LocalPlayer.Instance != null)
                LocalPlayer.Instance.Update();

            // Drive Steam callbacks every frame so registered handlers fire when invites are accepted.
            // Only do this when NOT hosting
            if (!(inGameHost?.IsRunning ?? false))
            {
                try { Networking.Steam.NativeSteamAPI.RunCallbacks(); } catch { }
                try { Steamworks.SteamClient.RunCallbacks(); } catch { }
            }

            if (_pendingJoinLobbyCall != 0)
            {
                bool callDone;
                bool joinFailed;
                try { callDone = Networking.Steam.NativeSteamAPI.IsAPICallCompleted(_pendingJoinLobbyCall, out joinFailed); }
                catch (Exception ex) { logger.Warning($"[Steam] IsAPICallCompleted threw: {ex.Message}"); callDone = false; joinFailed = false; }

                if (callDone)
                {
                    ulong call = _pendingJoinLobbyCall;
                    ulong lid  = _pendingJoinLobbyId;
                    _pendingJoinLobbyCall = 0;
                    _pendingJoinLobbyId   = 0;

                    logger.Msg($"[Steam] JoinLobby call={call} lobby={lid} completed, failed={joinFailed}");

                    if (!joinFailed)
                    {
                        string bbs_host   = Networking.Steam.NativeSteamAPI.GetLobbyData(lid, "bbs_host");
                        string inviteKey  = Networking.Steam.NativeSteamAPI.GetLobbyData(lid, "bbs_invite_key");
                        ulong  owner      = Networking.Steam.NativeSteamAPI.GetLobbyOwner(lid);
                        logger.Msg($"[Steam] Invite lobby {lid}: bbs_host='{bbs_host}' GetLobbyOwner={owner} hasInviteKey={!string.IsNullOrEmpty(inviteKey)}");

                        ulong hostId = 0;
                        if (!string.IsNullOrEmpty(bbs_host) && bbs_host != "0")
                            ulong.TryParse(bbs_host, out hostId);
                        if (hostId == 0)
                            hostId = owner;

                        if (hostId != 0)
                        {
                            logger.Msg($"[Steam] Resolved invite host SteamID: {hostId}, queuing connect…");
                            _pendingInviteConnect = hostId;
                            _pendingInviteKey = inviteKey; // may be empty for old hosts without the key
                        }
                        else
                            logger.Warning($"[Steam] Could not resolve host SteamID from invite lobby {lid} — bbs_host='{bbs_host}' owner={owner}");
                    }
                    else
                        logger.Warning($"[Steam] JoinLobby failed for lobby {lid}");

                    // Leave the lobby after reading the data — we only joined to resolve the host ID.
                    try { Networking.Steam.NativeSteamAPI.LeaveLobby(lid); } catch { }
                }
            }

            // Auto-connect from a Steam game invite.
            if (_pendingInviteConnect != 0 && !networkManager.IsConnected)
            {
                if (!HasLoadedGame())
                {
                    var continueBtn = Menu.me?.ContinueGameButton;
                    if (continueBtn != null)
                    {
                        float now = Time.realtimeSinceStartup;
                        if (_continueBtnFirstSeenAt < 0)
                        {
                            _continueBtnFirstSeenAt = now;
                            logger.Msg("[Invite] ContinueGameButton found — waiting for menu to settle before clicking");
                        }
                        // Wait 3s after the button first appears (lets the menu intro animation then retry every 5s in case a click was silently dropped.
                        float sinceAppear = now - _continueBtnFirstSeenAt;
                        float sinceClick  = now - _lastContinueClickAt;
                        if (sinceAppear >= 3f && sinceClick >= 5f)
                        {
                            _lastContinueClickAt = now;
                            logger.Msg($"[Invite] Clicking ContinueGameButton (interactable={continueBtn.interactable}, sinceAppear={sinceAppear:F1}s, sinceClick={sinceClick:F1}s)");
                            continueBtn.onClick.Invoke();
                        }
                    }
                    else
                    {
                        // Menu not loaded yet
                        _continueBtnFirstSeenAt = -1f;
                        float now = Time.realtimeSinceStartup;
                        if (now - _lastContinueClickAt >= 5f)
                        {
                            _lastContinueClickAt = now;
                            logger.Msg($"[Invite] Waiting for main menu (Menu.me={(Menu.me != null ? "exists" : "null")}, HasLoadedGame={HasLoadedGame()})");
                        }
                    }
                }
                else
                {
                    // Game is loaded so connect now.
                    _continueBtnFirstSeenAt = -1f;
                    _lastContinueClickAt    = -10f;
                    ulong  hostId    = _pendingInviteConnect;
                    string inviteKey = _pendingInviteKey;
                    _pendingInviteConnect = 0;
                    _pendingInviteKey     = string.Empty;
                    logger.Msg($"[Steam] Auto-connecting to invite host {hostId} (inviteKey={!string.IsNullOrEmpty(inviteKey)})");
                    // Pass the invite key as the "password"; the server accepts it as an alternate
                    // auth token so invited players don't need to know the session password.
                    // Empty inviteKey falls back to the default no-password key ("cuzzillobochfoddy").
                    networkManager.ConnectSteam(hostId.ToString(), inviteKey);
                }
            }
            else if (_pendingInviteConnect == 0)
            {
                _continueBtnFirstSeenAt = -1f;
                _lastContinueClickAt    = -10f;
            }

            Networking.Steam.SteamP2PTest.Poll();

            if (MelonDebug.IsEnabled())
            {
                if (Input.GetKeyDown(KeyCode.F3))
                    networkManager.Connect(ModSettings.connection.Address.Value,
                        ModSettings.connection.Port.Value,
                        ModSettings.connection.Password.Value);

                if (Input.GetKeyDown(KeyCode.F4))
                    networkManager.Disconnect();
            }
        }

        public override void OnLateUpdate()
        {
            if (networkManager == null)
                return;

            if (LocalPlayer.Instance != null)
                LocalPlayer.Instance.LateUpdate();

            networkManager.LateUpdate();
        }

        public override void OnApplicationQuit()
        {
            // Stop hosting first. InGameHost.Stop() sends the 0xFE graceful disconnect packet to all connected Steam peers before the process exits.
            if (inGameHost?.IsRunning == true)
                inGameHost.Stop();

            networkManager?.Disconnect();
        }

        public static bool HasLoadedGame()
        {
            if (Menu.me == null)
                return false;
            return Menu.me.gameInProgress;
        }

        public static void DebugMsg(string msg)
        {
            //if (!MelonDebug.IsEnabled())
            //  return;
            logger.Msg(msg);
        }

        public static bool RegisterComponent<T>(params Type[] interfaces)
            where T : class
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<T>(new()
                {
                    LogSuccess = true,
                    Interfaces = interfaces
                });
            }
            catch (Exception e)
            {
                logger.Error($"Exception while attempting to Register {typeof(T).Name}: {e}");
                return false;
            }
            return true;
        }
    }
}
