using NSQM.Core.Networking;
using NSQM.Data.Extensions;
using NSQM.Data.Messages;
using NSQM.Data.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSQM.Core.Producer
{
	public sealed class AcceptConnection
	{
		private NSQMBasicWebSocket _nsqmWebSocket;
		private NSQMTaskMessage _taskMessage;

		internal AcceptConnection(NSQMBasicWebSocket webSocket, NSQMTaskMessage taskMessage)
		{
			_nsqmWebSocket = webSocket;
			_taskMessage = taskMessage;
		}

		public async Task<ApiResponseL3<object>?> Accept(AckType ackType)
		{
			var ackMessage = NSQMAck.Build(_taskMessage.ToId, _taskMessage.ToId, _taskMessage.FromId, _taskMessage.TaskId, _taskMessage.ChannelId, ackType, UserType.Consumer);
			return await _nsqmWebSocket.SendAndReceive<object>(ackMessage, CancellationToken.None);
		}
	}
}
