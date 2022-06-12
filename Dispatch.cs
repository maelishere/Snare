using System.Net.Sockets;

namespace Snare
{
    using Bolt;

    public class Dispatch
    {
        protected readonly Socket m_socket;
        protected readonly byte[] m_buffer = new byte[1200];

        protected Writer m_writer = new Writer();

        internal Dispatch()
        {
            m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        internal Dispatch(AddressFamily family)
        {
            m_socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Close()
        {
            m_socket.Close();
        }
    }
}
