using System;
using System.Net;
using System.Net.Sockets;

namespace Snare
{
    using Bolt;

    public class Peer : Dispatch
    {
        public Peer(EndPoint session) : base()
        {
            m_socket.BeginConnect(session,
                (IAsyncResult ar) =>
                {
                    m_socket.EndConnect(ar);
                }, null);
        }

        public Peer(AddressFamily family, EndPoint session) : base(family)
        {
            m_socket.BeginConnect(session,
                (IAsyncResult ar) =>
                {
                    m_socket.EndConnect(ar);
                }, null);
        }

        public void Leave()
        {
            m_socket.BeginDisconnect(false,
                (IAsyncResult ar) =>
                {
                    m_socket.EndConnect(ar);
                }, null);
        }

        public void Send(Write callback)
        {
            m_writer.Reset();
            callback?.Invoke(ref m_writer);
            m_socket.BeginSend(m_writer.ToArray(), 0, m_writer.Current, SocketFlags.None,
                (IAsyncResult ar) =>
                {
                    m_socket.EndSend(ar);
                }, null);
        }

        public void Update(int timeout, Read received)
        {
            while (m_socket.Poll(timeout, SelectMode.SelectRead))
            {
                m_socket.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None,
                    (IAsyncResult ar) =>
                    {
                        int size = m_socket.EndReceive(ar);
                        Reader reader = new Reader(new Segment(m_buffer, 0, size));
                        received?.Invoke(ref reader);
                    }, null);
            }
        }
    }
}