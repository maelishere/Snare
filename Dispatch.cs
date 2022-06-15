using System.Net.Sockets;

namespace Snare
{
    using Bolt;

    public class Dispatch
    {
        protected readonly Socket m_socket;
        protected readonly byte[] m_buffer = new byte[1200];

        protected Writer m_writer = new Writer();

        internal Dispatch(Family family)
        {
            switch (family)
            {
                case Family.Dual:
                    m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    break;

                case Family.IPV6:
                    m_socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    break;

                case Family.IPV4:
                default:
                    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    break;
            }
        }

        public void Close()
        {
            m_socket.Close();
        }
    }
}
