using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Snare
{
    using Bolt;

    public class Session : Dispatch
    {
        public delegate void ReadFrom(int peer, ref Reader reader);

        private readonly Queue<int> m_outgoing = new Queue<int>();
        private readonly Dictionary<int, Socket> m_peers = new Dictionary<int, Socket>();

        public Session(Family family, EndPoint listen, int max) : base(family)
        {
            m_socket.Bind(listen);
            m_socket.Listen(max);
        }

        public bool Disconnect(int peer)
        {
            if (m_peers.TryGetValue(peer, out Socket socket))
            {
                socket.BeginDisconnect(false,
                    (IAsyncResult ar) =>
                    {
                        socket.EndSend(ar);
                        m_peers.Remove(peer);
                    }, null);
                return true;
            }
            return false;
        }

        public bool Send(int peer, Write callback)
        {
            if (m_peers.TryGetValue(peer, out Socket socket))
            {
                m_writer.Reset();
                callback?.Invoke(ref m_writer);
                socket.BeginSend(m_writer.ToArray(), 0, m_writer.Current, SocketFlags.None,
                    (IAsyncResult ar) =>
                    {
                        socket.EndSend(ar);
                    }, null);
                return true;
            }
            return false;
        }

        public void Update(int timeout, Action<int> connected, ReadFrom received, Action<int> disconnected)
        {
            while (m_socket.Poll(timeout, SelectMode.SelectRead))
            {
                Socket socket = m_socket.Accept();
                int id = socket.RemoteEndPoint.Serialize().GetHashCode();
                m_peers.Add(id, socket);

                // make sure we can use socket.connected
                Send(id,
                    (ref Writer writer) =>
                    {
                        writer.Write(new byte[32]);
                    });
                connected?.Invoke(id);
            }

            foreach (var peer in m_peers)
            {
                while (peer.Value.Poll(timeout, SelectMode.SelectRead))
                {
                    peer.Value.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None,
                        (IAsyncResult ar) =>
                        {
                            int size = peer.Value.EndReceive(ar);
                            Reader reader = new Reader(new Segment(m_buffer, 0, size));
                            received?.Invoke(peer.Key, ref reader);
                        }, null);

                    if (!peer.Value.Connected)
                    {
                        disconnected?.Invoke(peer.Key);
                        m_outgoing.Enqueue(peer.Key);
                    }
                }
            } 
            
            while (m_outgoing.Count > 0)
            {
                m_peers.Remove(m_outgoing.Dequeue());
            }
        }
    }
}