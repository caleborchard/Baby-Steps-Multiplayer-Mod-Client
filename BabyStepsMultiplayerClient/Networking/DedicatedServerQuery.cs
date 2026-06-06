using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BabyStepsMultiplayerClient.Networking
{
    /// <summary>
    /// Queries a dedicated server's raw UDP status listener for its current status.
    /// The status listener runs on <c>gamePort + 1</c> (e.g., 7778 for a game on 7777).
    ///
    /// Protocol (plain UDP, no LiteNetLib framing):
    ///   Client → Server: 4 bytes { 'B','B','S','Q' }
    ///   Server → Client: 7 bytes { 'B','B','S','R', playerCount, maxPlayers, flags }
    ///                    flags bit-0 = password protected
    /// </summary>
    public static class DedicatedServerQuery
    {
        private static readonly byte[] QueryBytes    = { (byte)'B', (byte)'B', (byte)'S', (byte)'Q' };
        private static readonly byte[] ResponseMagic = { (byte)'B', (byte)'B', (byte)'S', (byte)'R' };

        /// <summary>
        /// Queries the status listener on <paramref name="statusPort"/> (= game port + 1).
        /// Returns (PlayerCount, MaxPlayers, IsLocked) or null on timeout / no response.
        /// </summary>
        public static Task<(int PlayerCount, int MaxPlayers, bool IsLocked)?> QueryAsync(
            string host, int statusPort, int timeoutMs = 1000)
            => Task.Run(() => QueryBlocking(host, statusPort, timeoutMs));

        private static (int PlayerCount, int MaxPlayers, bool IsLocked)? QueryBlocking(
            string host, int statusPort, int timeoutMs)
        {
            try
            {
                using var udp = new UdpClient();
                udp.Client.ReceiveTimeout = timeoutMs;
                udp.Send(QueryBytes, QueryBytes.Length, host, statusPort);
                var ep = new IPEndPoint(IPAddress.Any, 0);
                return ParseResponse(udp.Receive(ref ep));
            }
            catch { return null; }
        }

        private static (int, int, bool)? ParseResponse(byte[] data)
        {
            if (data == null || data.Length < 7)                   return null;
            if (data[0] != ResponseMagic[0] || data[1] != ResponseMagic[1] ||
                data[2] != ResponseMagic[2] || data[3] != ResponseMagic[3]) return null;
            return (data[4], data[5], (data[6] & 1) != 0);
        }
    }
}
