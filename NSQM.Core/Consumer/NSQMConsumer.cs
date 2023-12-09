using NSQM.Core.Model;
using NSQM.Core.Networking;
using NSQM.Core.Producer;
using NSQM.Data.Extensions;
using NSQM.Data.Messages;
using NSQM.Data.Model.Persistence;
using NSQM.Data.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace NSQM.Core.Consumer
{
	public class NSQMConsumer
	{
		private NSQMBasicWebSocket _nsqmSocket;
		private string _host;
		
		public Guid UserId { get; private set; }

		public event Action<ReceivedMessage, ResultConnection>? MessageReceived;
		public event Action<ReceivedMessage>? MessageStreamReceived;

		public NSQMConsumer(string host, Guid id)
		{
			UserId = id;

			_host = host;
		}

		public async Task Connect(CancellationToken cancellationToken)
		{
			var webSocket = new ClientWebSocket();
			await webSocket.ConnectAsync(new Uri($"ws://{_host}/"), cancellationToken);

			_nsqmSocket = new NSQMBasicWebSocket(webSocket);
			_nsqmSocket.ProcessMessage += ProcessMessage; ;

			Task.Run(async () => await _nsqmSocket.Start());
		}

		private void ProcessMessage(NSQMessage message)
		{
			switch (message.Type)
			{
				case MessageType.Task:
				case MessageType.TaskStream:
					var nsqmTaskMessage = message.StructBuffer.ToStruct<NSQMTaskMessage>(Encoding.UTF8);
					var receivedTask = new ReceivedMessage()
					{
						Content = nsqmTaskMessage.Content,
						FromId = nsqmTaskMessage.FromId,
						Name = nsqmTaskMessage.TaskName,
						TaskId = nsqmTaskMessage.TaskId
					};
					if (message.Type == MessageType.Task)
						MessageReceived?.Invoke(receivedTask, new ResultConnection(_nsqmSocket, nsqmTaskMessage, UserId));
					else if (message.Type == MessageType.TaskStream)
						MessageStreamReceived?.Invoke(receivedTask);
					break;
			}
		}

		public async Task Close()
		{
			await _nsqmSocket.Close();
		}

		public async Task<ApiResponseL3<User>?> Subscribe(string channelId)
		{
			var subscribeMessage = NSQMSubscribeMessage.Build(UserId, channelId, UserType.Consumer, Encoding.UTF8);
			return await _nsqmSocket.SendAndReceive<User>(subscribeMessage, CancellationToken.None);
		}
	}
}
