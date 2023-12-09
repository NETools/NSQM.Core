using NSQM.Data.Extensions;
using NSQM.Data.Messages;
using NSQM.Data.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NSQM.Core.Networking
{
    internal class NSQMBasicWebSocket : NSQMWebSocket
    {
        public event Action<NSQMessage>? ProcessMessage;
        public NSQMBasicWebSocket(WebSocket webSocket) : base(webSocket) { }

        protected override Task HandleMessage(NSQMessage message)
        {
            ProcessMessage?.Invoke(message);
            return Task.CompletedTask;
        }
    }
}
