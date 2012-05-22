using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SDB.DataServices.Tcp
{
    class TcpMessageQueue
    {
        private readonly LinkedList<TcpMessage> _messages;
        private readonly ManualResetEvent _eventListener;

        public TcpMessageQueue()
        {
            _messages = new LinkedList<TcpMessage>();
            _eventListener = new ManualResetEvent(false);
        }

        public void Add(TcpMessage message)
        {
            if (message == null)
                return;

            lock (_messages)
            {
                _messages.AddLast(message);
            }
            _eventListener.Set();
        }

        public IEnumerable<TcpMessage> WaitAndPopAll()
        {
            TcpMessage[] result;
            do
            {
                _eventListener.WaitOne();
                lock (_messages)
                {
                    result = _messages.ToArray();
                    _messages.Clear();
                    _eventListener.Reset();
                }
            } while (result.Length <= 0);
            return result;
        }
    }
}
