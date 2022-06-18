using System;
using System.Net;
using System.Net.Sockets;

namespace Snare
{
    using Bolt;

    public class Member : Dispatch
    {
        public int Local { get; }
        public int Remote { get; }

        public Member(Family family, EndPoint session, Action<int, int> connected) : base(family)
        {
            m_socket.BeginConnect(session,
                (IAsyncResult ar) =>
                {
                    m_socket.EndConnect(ar);

                    // make sure we can use socket.connected
                    Send(
                        (ref Writer writer) =>
                        {
                            writer.Write(new byte[32]);
                        });
                }, null);

            if (m_socket.Connected)
            {
                Local = m_socket.LocalEndPoint.Serialize().GetHashCode();
                Remote = m_socket.RemoteEndPoint.Serialize().GetHashCode();
                connected?.Invoke(Local, Remote);
            }
        }

        public void Disconnect(Action left = null)
        {
            m_socket.BeginDisconnect(false,
                (IAsyncResult ar) =>
                {
                    m_socket.EndDisconnect(ar);
                    left?.Invoke();
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

        public bool Update(int timeout, Read received)
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
            return m_socket.Connected;
        }
    }
}