using System.Collections.Concurrent;

namespace BabyStepsMultiplayerClient.Networking
{
    internal class PlayerPacketQueue<TKey, TPacket>
    {
        private ConcurrentDictionary<TKey, ConcurrentQueue<TPacket>> _queue = new();

        internal void Clear()
        {
            if (_queue.Count <= 0)
                return;
            foreach (var key in _queue.Keys.ToArray())
                Clear(key);
            _queue.Clear();
        }

        internal void Clear(TKey key)
        {
            if (!_queue.TryRemove(key, out var packetQueue))
                return;
            if ((packetQueue == null)
                || (packetQueue.Count > 0))
                return;
            packetQueue.Clear();
        }

        internal void Enqueue(TKey key, TPacket packet)
        {
            if (!_queue.TryGetValue(key, out var packetQueue)
                || (packetQueue == null))
                packetQueue = _queue[key] = new();
            packetQueue.Enqueue(packet);
        }

        internal void Process(Action<TPacket> callback)
        {
            if (_queue.Count <= 0)
                return;
            foreach (var pair in _queue.ToArray())
            {
                Process(pair.Value, callback);
                _queue.Remove(pair.Key, out _);
            }
        }

        internal void Process(TKey key, Action<TPacket> callback)
        {
            if (_queue.Count <= 0)
                return;
            if (!_queue.TryRemove(key, out var packetQueue))
                return;
            Process(packetQueue, callback);
        }

        private void Process(ConcurrentQueue<TPacket> packetQueue, Action<TPacket> callback)
        {
            if (packetQueue == null)
                return;
            while (packetQueue.TryDequeue(out TPacket packet))
                try
                {
                    if (callback != null)
                        callback.Invoke(packet);
                }
                catch (Exception ex)
                {
                    Core.logger.Error(ex.ToString());
                }
        }
    }
}
