using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Snare
{
    using Bolt;

    public class Session : Dispatch
    {
        public delegate void Reading(int peer, byte type, ref Reader reader);

        private readonly Dictionary<int, Socket> m_peers = new Dictionary<int, Socket>();

        public Session(EndPoint listen, int max) : base()
        {
            m_socket.Bind(listen);
            m_socket.Listen(max);
        }

        public Session(AddressFamily family, EndPoint listen, int max) : base(family)
        {
            m_socket.Bind(listen);
            m_socket.Listen(max);
        }

        public bool Kickout(int peer)
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

        public bool Send(int peer, byte type, Write callback)
        {
            if (m_peers.TryGetValue(peer, out Socket socket))
            {
                m_writer.Reset();
                m_writer.Write(type);
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

        public void Update(int timeout, Action<int> joined, Reading received)
        {
            while (m_socket.Poll(timeout, SelectMode.SelectRead))
            {
                Socket socket = m_socket.Accept();
                int id = socket.RemoteEndPoint.Serialize().GetHashCode();
                m_peers.Add(id, socket);
                joined?.Invoke(id);
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
                            received?.Invoke(peer.Key, reader.Read(), ref reader);
                        }, null);
                }
            }
        }
    }
}
