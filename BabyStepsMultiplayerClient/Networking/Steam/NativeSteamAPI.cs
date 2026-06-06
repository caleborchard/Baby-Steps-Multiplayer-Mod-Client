using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BabyStepsMultiplayerClient.Networking.Steam
{
    /// <summary>
    /// Thin P/Invoke wrapper around steam_api64.dll's flat C interface.
    /// The DLL is already loaded by the game at startup so no extra loading is needed.
    ///
    /// All interface pointers are lazily cached on first use.
    /// Not thread-safe — call only from the main/poll thread.
    /// </summary>
    internal static unsafe class NativeSteamAPI
    {
        private const string Dll = "steam_api64";

        // ── Interface accessor P/Invoke ──────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SteamAPI_SteamNetworking_v006();
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SteamAPI_SteamMatchmaking_v009();
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SteamAPI_SteamUtils_v010();
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SteamAPI_SteamUser_v021();
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SteamAPI_SteamFriends_v017();

        // ── Direct callback dispatch ─────────────────────────────────────────────

        [DllImport(Dll, EntryPoint = "SteamAPI_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RunCallbacks();

        // ── ISteamApps ────────────────────────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SteamAPI_SteamApps_v008();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern int SteamAPI_ISteamApps_GetLaunchCommandLine(IntPtr self, [Out] byte[] pchCommandLine, int cubCommandLine);

        // ── ISteamNetworking ─────────────────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamNetworking_SendP2PPacket(
            IntPtr self, ulong steamIDRemote, [In] byte[] pubData, uint cubData, int eP2PSendType, int nChannel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamNetworking_IsP2PPacketAvailable(
            IntPtr self, out uint pcubMsgSize, int nChannel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamNetworking_ReadP2PPacket(
            IntPtr self, [Out] byte[] pubDest, uint cubDest, out uint pcubMsgSize, out ulong psteamIDRemote, int nChannel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamNetworking_AcceptP2PSessionWithUser(
            IntPtr self, ulong steamIDRemote);

        // ── ISteamMatchmaking ────────────────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamMatchmaking_CreateLobby(
            IntPtr self, int eLobbyType, int cMaxMembers);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamMatchmaking_SetLobbyData(
            IntPtr self, ulong steamIDLobby,
            [MarshalAs(UnmanagedType.LPStr)] string pchKey,
            [MarshalAs(UnmanagedType.LPStr)] string pchValue);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr SteamAPI_ISteamMatchmaking_GetLobbyData(
            IntPtr self, ulong steamIDLobby,
            [MarshalAs(UnmanagedType.LPStr)] string pchKey);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamMatchmaking_LeaveLobby(
            IntPtr self, ulong steamIDLobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamMatchmaking_GetLobbyOwner(
            IntPtr self, ulong steamIDLobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern int SteamAPI_ISteamMatchmaking_GetNumLobbyMembers(
            IntPtr self, ulong steamIDLobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern void SteamAPI_ISteamMatchmaking_AddRequestLobbyListStringFilter(
            IntPtr self,
            [MarshalAs(UnmanagedType.LPStr)] string pchKeyToMatch,
            [MarshalAs(UnmanagedType.LPStr)] string pchValueToMatch,
            int eComparisonType);  // k_ELobbyComparisonEqual = 0

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern void SteamAPI_ISteamMatchmaking_AddRequestLobbyListDistanceFilter(
            IntPtr self, int eLobbyDistanceFilter);  // k_ELobbyDistanceFilterWorldwide = 3

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamMatchmaking_SetLobbyJoinable(
            IntPtr self, ulong steamIDLobby, bool bLobbyJoinable);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamMatchmaking_RequestLobbyList(IntPtr self);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamMatchmaking_GetLobbyByIndex(IntPtr self, int iLobby);

        // ── ISteamUtils ──────────────────────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamUtils_IsAPICallCompleted(
            IntPtr self, ulong hSteamAPICall, out bool pbFailed);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamUtils_GetAPICallResult(
            IntPtr self, ulong hSteamAPICall, IntPtr pCallback, int cubCallback,
            int iCallbackExpected, out bool pbFailed);

        // ── ISteamUser ───────────────────────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamUser_GetSteamID(IntPtr self);

        // ── ISteamFriends ────────────────────────────────────────────────────────

        private const int k_EFriendFlagImmediate = 0x04; // "regular" friends

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern int SteamAPI_ISteamFriends_GetFriendCount(IntPtr self, int iFriendFlags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamFriends_GetFriendByIndex(IntPtr self, int iFriend, int iFriendFlags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr SteamAPI_ISteamFriends_GetPersonaName(IntPtr self);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr SteamAPI_ISteamFriends_GetFriendPersonaName(IntPtr self, ulong steamIDFriend);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamFriends_InviteUserToGame(
            IntPtr self, ulong steamIDFriend, [MarshalAs(UnmanagedType.LPStr)] string pchConnectString);

        // ── ISteamMatchmaking (invite) ────────────────────────────────────────────

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern bool SteamAPI_ISteamMatchmaking_InviteUserToLobby(
            IntPtr self, ulong steamIDLobby, ulong steamIDInvitee);

        // ── Lazy interface pointer caches ────────────────────────────────────────

        private static IntPtr _apps    = IntPtr.Zero;
        private static IntPtr _net     = IntPtr.Zero;
        private static IntPtr _mm      = IntPtr.Zero;
        private static IntPtr _utils   = IntPtr.Zero;
        private static IntPtr _user    = IntPtr.Zero;
        private static IntPtr _friends = IntPtr.Zero;

        private static IntPtr Apps    => _apps    != IntPtr.Zero ? _apps    : (_apps    = SteamAPI_SteamApps_v008());
        private static IntPtr Net     => _net     != IntPtr.Zero ? _net     : (_net     = SteamAPI_SteamNetworking_v006());
        private static IntPtr MM      => _mm      != IntPtr.Zero ? _mm      : (_mm      = SteamAPI_SteamMatchmaking_v009());
        private static IntPtr Utils   => _utils   != IntPtr.Zero ? _utils   : (_utils   = SteamAPI_SteamUtils_v010());
        private static IntPtr User    => _user    != IntPtr.Zero ? _user    : (_user    = SteamAPI_SteamUser_v021());
        private static IntPtr Friends => _friends != IntPtr.Zero ? _friends : (_friends = SteamAPI_SteamFriends_v017());

        // ── Callback IDs ─────────────────────────────────────────────────────────
        // From Steamworks SDK: k_iSteamMatchmakingCallbacks = 500
        public const int k_LobbyCreated   = 513;  // 500 + 13
        public const int k_LobbyMatchList = 510;  // 500 + 10

        // ── P2P ──────────────────────────────────────────────────────────────────

        public static bool SendP2PPacket(ulong steamId, byte[] data, int sendType, int channel)
            => SteamAPI_ISteamNetworking_SendP2PPacket(Net, steamId, data, (uint)data.Length, sendType, channel);

        public static bool IsP2PPacketAvailable(out uint size, int channel)
            => SteamAPI_ISteamNetworking_IsP2PPacketAvailable(Net, out size, channel);

        public static bool ReadP2PPacket(byte[] buf, out uint bytesRead, out ulong from, int channel)
            => SteamAPI_ISteamNetworking_ReadP2PPacket(Net, buf, (uint)buf.Length, out bytesRead, out from, channel);

        public static void AcceptP2PSession(ulong steamId)
            => SteamAPI_ISteamNetworking_AcceptP2PSessionWithUser(Net, steamId);

        // ── Matchmaking ──────────────────────────────────────────────────────────

        public static ulong CreateLobby(int lobbyType = 2 /*Public*/, int maxMembers = 16)
            => SteamAPI_ISteamMatchmaking_CreateLobby(MM, lobbyType, maxMembers);

        public static bool SetLobbyData(ulong lobbyId, string key, string value)
            => SteamAPI_ISteamMatchmaking_SetLobbyData(MM, lobbyId, key, value);

        public static string GetLobbyData(ulong lobbyId, string key)
        {
            var ptr = SteamAPI_ISteamMatchmaking_GetLobbyData(MM, lobbyId, key);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? "" : "";
        }

        public static void LeaveLobby(ulong lobbyId)
            => SteamAPI_ISteamMatchmaking_LeaveLobby(MM, lobbyId);

        public static ulong GetLobbyOwner(ulong lobbyId)
            => SteamAPI_ISteamMatchmaking_GetLobbyOwner(MM, lobbyId);

        public static int GetLobbyMemberCount(ulong lobbyId)
            => SteamAPI_ISteamMatchmaking_GetNumLobbyMembers(MM, lobbyId);

        public static void AddLobbyFilter(string key, string value)
            => SteamAPI_ISteamMatchmaking_AddRequestLobbyListStringFilter(MM, key, value, 0);

        /// <summary>Must be called before RequestLobbyList to include all regions.</summary>
        public static void SetLobbyDistanceWorldwide()
            => SteamAPI_ISteamMatchmaking_AddRequestLobbyListDistanceFilter(MM, 3 /* k_ELobbyDistanceFilterWorldwide */);

        public static bool SetLobbyJoinable(ulong lobbyId, bool joinable)
            => SteamAPI_ISteamMatchmaking_SetLobbyJoinable(MM, lobbyId, joinable);

        public static ulong RequestLobbyList()
            => SteamAPI_ISteamMatchmaking_RequestLobbyList(MM);

        public static ulong GetLobbyByIndex(int index)
            => SteamAPI_ISteamMatchmaking_GetLobbyByIndex(MM, index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex(IntPtr self, ulong steamIDLobby, int iMember);

        public static ulong GetLobbyMemberByIndex(ulong lobbyId, int index)
            => SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex(MM, lobbyId, index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        static extern ulong SteamAPI_ISteamMatchmaking_JoinLobby(IntPtr self, ulong steamIDLobby);

        /// <summary>
        /// Ask Steam to join this lobby. Returns a SteamAPICall_t handle.
        /// Joining adds this player to the lobby member list so the host can
        /// AcceptP2PSessionWithUser for them and Steam delivers their packets.
        /// </summary>
        public static ulong JoinLobby(ulong lobbyId)
            => SteamAPI_ISteamMatchmaking_JoinLobby(MM, lobbyId);

        // ── API call result polling ───────────────────────────────────────────────
        // The game's own Steam update loop calls SteamAPI_RunCallbacks() each frame,
        // so results become available without us needing to run callbacks ourselves.

        public static bool IsAPICallCompleted(ulong handle, out bool failed)
            => SteamAPI_ISteamUtils_IsAPICallCompleted(Utils, handle, out failed);

        /// <summary>
        /// Reads the result of a completed API call into an unmanaged struct.
        /// Returns false if the call failed or the result isn't ready.
        /// </summary>
        public static bool GetAPICallResult<T>(ulong handle, int callbackId, out T result)
            where T : unmanaged
        {
            result = default;
            fixed (T* ptr = &result)
            {
                bool failed;
                return SteamAPI_ISteamUtils_GetAPICallResult(
                    Utils, handle, (IntPtr)ptr, sizeof(T), callbackId, out failed) && !failed;
            }
        }

        // ── Local user ───────────────────────────────────────────────────────────

        public static ulong GetLocalSteamId()
            => SteamAPI_ISteamUser_GetSteamID(User);

        /// <summary>Returns the local user's Steam persona name (display name).</summary>
        public static string GetPersonaName()
        {
            var ptr = SteamAPI_ISteamFriends_GetPersonaName(Friends);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? "" : "";
        }

        /// <summary>
        /// Sends a Steam game invite to the specified friend.
        /// connectString is passed back to the game when they accept.
        /// </summary>
        public static bool InviteUserToGame(ulong steamId, string connectString = "")
            => SteamAPI_ISteamFriends_InviteUserToGame(Friends, steamId, connectString);

        /// <summary>
        /// Send a Steam lobby invite to a friend. This triggers GameLobbyJoinRequested_t
        /// on the friend's side when accepted — more reliable than InviteUserToGame because
        /// the callback fires both in-game and on cold launch via RunCallbacks.
        /// </summary>
        public static bool InviteUserToLobby(ulong lobbyId, ulong friendSteamId)
            => SteamAPI_ISteamMatchmaking_InviteUserToLobby(MM, lobbyId, friendSteamId);

        /// <summary>
        /// Returns the command line string that was used to launch the game via a Steam
        /// game invite.  Contains the connect string passed to InviteUserToGame.
        /// Empty when the game was not launched from an invite.
        /// </summary>
        public static string GetLaunchCommandLine()
        {
            var buf = new byte[512];
            SteamAPI_ISteamApps_GetLaunchCommandLine(Apps, buf, buf.Length);
            return System.Text.Encoding.UTF8.GetString(buf).TrimEnd('\0');
        }

        // ── GameLobbyJoinRequested_t callback (ID 333) ───────────────────────────
        // Fires when the user accepts a lobby invite (InviteUserToLobby) either in-game
        // or on cold launch.  Struct: CSteamID m_steamIDLobby (8) + CSteamID m_steamIDFriend (8) = 16 bytes.

        private const int k_GameLobbyJoinRequested = 333;

        private static NativeRunDelegate    _lobbyJoinRun;
        private static NativeRunExDelegate  _lobbyJoinRunEx;
        private static NativeGetSizeDelegate _lobbyJoinGetSize;
        private static IntPtr _lobbyJoinVtable      = IntPtr.Zero;
        private static IntPtr _lobbyJoinCallbackObj = IntPtr.Zero;

        public static void RegisterLobbyJoinRequestedCallback(Action<ulong> onLobbyId)
        {
            if (_lobbyJoinCallbackObj != IntPtr.Zero) return;

            _lobbyJoinRun = (thisPtr, pvParam) =>
            {
                if (pvParam == IntPtr.Zero) return;
                ulong lobbyId = (ulong)Marshal.ReadInt64(pvParam); // m_steamIDLobby at offset 0
                if (lobbyId != 0) onLobbyId(lobbyId);
            };
            _lobbyJoinRunEx   = (thisPtr, pvParam, bFail, hCall) => { };
            _lobbyJoinGetSize = (thisPtr) => 16;

            _lobbyJoinVtable = Marshal.AllocHGlobal(3 * 8);
            Marshal.WriteIntPtr(_lobbyJoinVtable, 0 * 8, Marshal.GetFunctionPointerForDelegate(_lobbyJoinRun));
            Marshal.WriteIntPtr(_lobbyJoinVtable, 1 * 8, Marshal.GetFunctionPointerForDelegate(_lobbyJoinRunEx));
            Marshal.WriteIntPtr(_lobbyJoinVtable, 2 * 8, Marshal.GetFunctionPointerForDelegate(_lobbyJoinGetSize));

            _lobbyJoinCallbackObj = Marshal.AllocHGlobal(16);
            for (int i = 0; i < 16; i++) Marshal.WriteByte(_lobbyJoinCallbackObj, i, 0);
            Marshal.WriteIntPtr(_lobbyJoinCallbackObj, 0, _lobbyJoinVtable);
            Marshal.WriteInt32(_lobbyJoinCallbackObj, 12, k_GameLobbyJoinRequested);

            SteamAPI_RegisterCallback(_lobbyJoinCallbackObj, k_GameLobbyJoinRequested);
        }

        // ── GameRichPresenceJoinRequested_t callback ──────────────────────────────
        // Fires when the local user accepts a game invite while the game is running.
        // Struct: CSteamID m_steamIDFriend (8 bytes) + char m_rgchConnect[256] = 264 bytes
        // k_iCallback = 337 (k_iSteamFriendsCallbacks 300 + 37)

        private const int k_GameRichPresenceJoinRequested = 337;

        private static NativeRunDelegate    _joinRun;
        private static NativeRunExDelegate  _joinRunEx;
        private static NativeGetSizeDelegate _joinGetSize;
        private static IntPtr _joinVtable      = IntPtr.Zero;
        private static IntPtr _joinCallbackObj = IntPtr.Zero;

        public static void RegisterJoinRequestedCallback(Action<string> onConnect)
        {
            if (_joinCallbackObj != IntPtr.Zero) return;

            _joinRun = (thisPtr, pvParam) =>
            {
                if (pvParam == IntPtr.Zero) return;
                // m_steamIDFriend at offset 0 (8 bytes), m_rgchConnect at offset 8 (256 bytes)
                string connectStr = Marshal.PtrToStringAnsi(
                    new IntPtr(pvParam.ToInt64() + 8)) ?? "";
                if (!string.IsNullOrWhiteSpace(connectStr))
                    onConnect(connectStr);
            };
            _joinRunEx   = (thisPtr, pvParam, bFail, hCall) => { };
            _joinGetSize = (thisPtr) => 264;

            _joinVtable = Marshal.AllocHGlobal(3 * 8);
            Marshal.WriteIntPtr(_joinVtable, 0 * 8, Marshal.GetFunctionPointerForDelegate(_joinRun));
            Marshal.WriteIntPtr(_joinVtable, 1 * 8, Marshal.GetFunctionPointerForDelegate(_joinRunEx));
            Marshal.WriteIntPtr(_joinVtable, 2 * 8, Marshal.GetFunctionPointerForDelegate(_joinGetSize));

            _joinCallbackObj = Marshal.AllocHGlobal(16);
            for (int i = 0; i < 16; i++) Marshal.WriteByte(_joinCallbackObj, i, 0);
            Marshal.WriteIntPtr(_joinCallbackObj, 0, _joinVtable);
            Marshal.WriteInt32(_joinCallbackObj, 12, k_GameRichPresenceJoinRequested);

            SteamAPI_RegisterCallback(_joinCallbackObj, k_GameRichPresenceJoinRequested);
        }

        /// <summary>Returns a Steam friend's persona name, or empty string if unknown.</summary>
        public static string GetFriendPersonaName(ulong steamId)
        {
            var ptr = SteamAPI_ISteamFriends_GetFriendPersonaName(Friends, steamId);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? "" : "";
        }

        /// <summary>
        /// Pre-accept P2P sessions from all Steam friends so they can connect without
        /// waiting for the P2PSessionRequest_t callback. Per Steam docs:
        /// "You can call AcceptP2PSessionWithUser before the P2PSessionRequest_t callback
        /// is received. If this happens, packets can be exchanged without a callback."
        /// Returns the number of friends accepted.
        /// </summary>
        public static int GetFriendCount()
            => SteamAPI_ISteamFriends_GetFriendCount(Friends, k_EFriendFlagImmediate);

        public static ulong GetFriendByIndex(int index)
            => SteamAPI_ISteamFriends_GetFriendByIndex(Friends, index, k_EFriendFlagImmediate);

        public static int PreAcceptFriends()
        {
            int count = SteamAPI_ISteamFriends_GetFriendCount(Friends, k_EFriendFlagImmediate);
            if (count <= 0) return 0;
            for (int i = 0; i < count; i++)
            {
                ulong fid = SteamAPI_ISteamFriends_GetFriendByIndex(Friends, i, k_EFriendFlagImmediate);
                if (fid != 0) AcceptP2PSession(fid);
            }
            return count;
        }

        // ── Native callback registration ─────────────────────────────────────────
        //
        // Registers P2PSessionRequest_t (callback ID 1202) directly with Steam's
        // callback system, bypassing Facepunch.Steamworks.  The game's own
        // SteamAPI_RunCallbacks() (called via Il2Cpp every frame) delivers it.
        //
        // CCallbackBase layout (x64 Windows MSVC):
        //   offset  0 : vtable*          (8 bytes)
        //   offset  8 : m_nCallbackFlags (1 byte)
        //   offset  9 : padding          (3 bytes)
        //   offset 12 : m_iCallback      (4 bytes)
        //   total = 16 bytes
        //
        // P2PSessionRequest_t = { CSteamID m_steamIDRemote } = 8 bytes.

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamAPI_RegisterCallback(IntPtr pCallback, int nCallbackExpected);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SteamAPI_UnregisterCallback(IntPtr pCallback);

        private const int k_P2PSessionRequest = 1202; // k_iSteamNetworkingCallbacks(1200) + 2

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeRunDelegate(IntPtr thisPtr, IntPtr pvParam);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeRunExDelegate(IntPtr thisPtr, IntPtr pvParam,
            [MarshalAs(UnmanagedType.I1)] bool bFail, ulong hCall);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int NativeGetSizeDelegate(IntPtr thisPtr);

        // Static fields prevent GC from collecting the delegates (which would
        // invalidate the native function pointers in the vtable).
        private static NativeRunDelegate _p2pRun;
        private static NativeRunExDelegate _p2pRunEx;
        private static NativeGetSizeDelegate _p2pGetSize;
        private static IntPtr _p2pVtable = IntPtr.Zero;
        private static IntPtr _p2pCallbackObj = IntPtr.Zero;

        /// <summary>
        /// Register a native P2PSessionRequest_t callback with Steam's callback system.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public static void RegisterP2PSessionRequestCallback(Action<ulong> onRequest)
        {
            if (_p2pCallbackObj != IntPtr.Zero) return; // already registered

            _p2pRun = (thisPtr, pvParam) =>
            {
                if (pvParam != IntPtr.Zero)
                    onRequest((ulong)Marshal.ReadInt64(pvParam));
            };
            _p2pRunEx   = (thisPtr, pvParam, bFail, hCall) => { };
            _p2pGetSize = (thisPtr) => 8; // sizeof(P2PSessionRequest_t) == sizeof(CSteamID)

            // Build vtable: [Run | RunEx | GetCallbackSizeBytes]
            _p2pVtable = Marshal.AllocHGlobal(3 * 8);
            Marshal.WriteIntPtr(_p2pVtable, 0 * 8, Marshal.GetFunctionPointerForDelegate(_p2pRun));
            Marshal.WriteIntPtr(_p2pVtable, 1 * 8, Marshal.GetFunctionPointerForDelegate(_p2pRunEx));
            Marshal.WriteIntPtr(_p2pVtable, 2 * 8, Marshal.GetFunctionPointerForDelegate(_p2pGetSize));

            // Build CCallbackBase
            _p2pCallbackObj = Marshal.AllocHGlobal(16);
            for (int i = 0; i < 16; i++) Marshal.WriteByte(_p2pCallbackObj, i, 0);
            Marshal.WriteIntPtr(_p2pCallbackObj, 0,  _p2pVtable);          // vtable*
            Marshal.WriteInt32(_p2pCallbackObj, 12, k_P2PSessionRequest);   // m_iCallback

            SteamAPI_RegisterCallback(_p2pCallbackObj, k_P2PSessionRequest);
        }

        /// <summary>Unregister the native P2PSessionRequest_t callback and free its memory.</summary>
        public static void UnregisterP2PSessionRequestCallback()
        {
            if (_p2pCallbackObj == IntPtr.Zero) return;
            SteamAPI_UnregisterCallback(_p2pCallbackObj);
            Marshal.FreeHGlobal(_p2pCallbackObj);
            Marshal.FreeHGlobal(_p2pVtable);
            _p2pCallbackObj = IntPtr.Zero;
            _p2pVtable      = IntPtr.Zero;
        }

        // ── Callback result structs ───────────────────────────────────────────────

        // The Steamworks SDK uses #pragma pack(push, 8) on Windows, so:
        //   EResult  (int,    4 bytes) at offset 0
        //   padding              4 bytes  at offset 4  (align ulong to 8)
        //   CSteamID (ulong,  8 bytes) at offset 8
        //   Total = 16 bytes
        // Using Pack=1 (12 bytes) reads the lobby ID from the wrong offset and yields a
        // garbage CSteamID that Steam refuses to accept for SetLobbyData etc.
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct LobbyCreated_t
        {
            [FieldOffset(0)] public int   m_eResult;         // EResult — 1 = OK
            [FieldOffset(8)] public ulong m_ulSteamIDLobby;  // CSteamID, correctly 8-byte aligned
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LobbyMatchList_t
        {
            public uint m_nLobbiesMatching;
        }
    }
}
