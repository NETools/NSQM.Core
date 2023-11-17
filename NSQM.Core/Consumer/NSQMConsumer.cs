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
		private HttpClient _httpClient;
		private NSQMBasicWebSocket _nsqmSocket;
		private string _host;
		
		public Guid UserId { get; private set; }

		public event Action<ReceivedTask, ResultConnection>? TaskReceived;
		public NSQMConsumer(string host, Guid id)
		{
			UserId = id;

			_host = host;
			_httpClient = new HttpClient();
		}

		public async Task Connect(CancellationToken cancellationToken)
		{
			var webSocket = new ClientWebSocket();
			await webSocket.ConnectAsync(new Uri($"ws://{_host}/"), cancellationToken);

			_nsqmSocket = new NSQMBasicWebSocket(webSocket);
			_nsqmSocket.MessageReceived += MessageReceived; ;

			Task.Run(async () => await _nsqmSocket.Start());
		}

		private void MessageReceived(NSQMessage message)
		{
			switch (message.Type)
			{
				case MessageType.Task:
					var nsqmTaskMessage = message.StructBuffer.ToStruct<NSQMTaskMessage>(Encoding.UTF8);
					var receivedTask = new ReceivedTask()
					{
						Content = nsqmTaskMessage.Content,
						FromId = nsqmTaskMessage.FromId,
						Name = nsqmTaskMessage.TaskName,
						TaskId = nsqmTaskMessage.TaskId
					};
					TaskReceived?.Invoke(receivedTask, new ResultConnection(_nsqmSocket, nsqmTaskMessage, UserId));
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
