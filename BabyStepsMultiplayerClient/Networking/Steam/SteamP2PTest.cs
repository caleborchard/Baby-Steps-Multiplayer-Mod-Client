using System.Linq;

namespace BabyStepsMultiplayerClient.Networking.Steam
{
    /// <summary>
    /// Minimal raw Steam P2P smoke test.  Bypasses all networking infrastructure
    /// (NetworkHost, transports, opcodes, etc.) to verify basic packet delivery
    /// between two instances.
    ///
    /// Usage:
    ///   Host:   click "P2P Test: Listen"  — begins scanning all channels each frame
    ///   Client: click "P2P Test: Ping"    — fires a 1-byte packet on every channel
    ///
    /// Host will log the first channel on which a packet arrives, confirming that
    /// Steam P2P is usable and identifying the right channel number.
    /// </summary>
    public static class SteamP2PTest
    {
        private static bool _listening = false;

        // Channels to try.  We scan 0-10 (game defaults), our current mod channels
        // (5 and 113), and a few others to catch any working value.
        private static readonly int[] ScanChannels =
            Enumerable.Range(0, 11)     // 0-10
            .Append(50)
            .Append(100)
            .Append(113)
            .Append(200)
            .Append(255)
            .ToArray();

        public static bool IsListening => _listening;

        public static void StartListening()
        {
            _listening = true;
            Core.logger.Msg("[P2PTest] Listening mode ON — scanning channels: " +
                string.Join(", ", ScanChannels));
        }

        public static void StopListening()
        {
            _listening = false;
            Core.logger.Msg("[P2PTest] Listening mode OFF");
        }

        /// <summary>
        /// Send a 1-byte test packet to targetId on every scanned channel.
        /// </summary>
        public static void SendPing(ulong targetId)
        {
            if (targetId == 0)
            {
                Core.logger.Warning("[P2PTest] No target SteamID set");
                return;
            }
            Core.logger.Msg($"[P2PTest] Sending ping to SteamID={targetId} on {ScanChannels.Length} channels");
            foreach (int ch in ScanChannels)
            {
                // payload: [0xBB][channel_byte] so host can confirm which channel arrived
                bool ok = NativeSteamAPI.SendP2PPacket(
                    targetId,
                    new byte[] { 0xBB, (byte)(ch & 0xFF) },
                    0 /* unreliable */,
                    ch);
                if (!ok)
                    Core.logger.Msg($"[P2PTest]   channel {ch}: SendP2PPacket returned false");
            }
            Core.logger.Msg("[P2PTest] All pings sent");
        }

        /// <summary>Call every frame from Core.OnUpdate when listening.</summary>
        public static void Poll()
        {
            if (!_listening) return;

            foreach (int ch in ScanChannels)
            {
                if (!NativeSteamAPI.IsP2PPacketAvailable(out uint size, ch)) continue;

                var buf = new byte[size];
                if (!NativeSteamAPI.ReadP2PPacket(buf, out _, out ulong from, ch)) continue;

                string hex = string.Join(" ", buf.Select(b => $"{b:X2}"));
                Core.logger.Msg(
                    $"[P2PTest] *** PACKET RECEIVED *** channel={ch} from={from} " +
                    $"len={size} bytes=[{hex}]");

                // Accept the session so further packets can flow
                NativeSteamAPI.AcceptP2PSession(from);
            }
        }
    }
}
