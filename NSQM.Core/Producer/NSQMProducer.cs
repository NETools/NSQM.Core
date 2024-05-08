using NSQM.Core.Model;
using NSQM.Core.Networking;
using NSQM.Data.Extensions;
using NSQM.Data.Messages;
using NSQM.Data.Model;
using NSQM.Data.Model.Persistence;
using NSQM.Data.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NSQM.Core.Producer
{
    public class NSQMProducer : IDisposable
	{
		private HttpClient _httpClient;
		private NSQMBasicWebSocket _nsqmSocket;
		private string _host;
		private Dictionary<Guid, MessageHandler> _publishedMessages = new Dictionary<Guid, MessageHandler>();

		public Guid UserId { get; private set; }

		public event Action<ReceivedMessage, AcceptConnection>? Mailbox;

		public NSQMProducer(string host, Guid id)
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
			_nsqmSocket.ProcessMessage += MessageReceived;

			Task.Run(async () => await _nsqmSocket.Start());
		}

		private void MessageReceived(NSQMessage message)
		{
			switch (message.Type)
			{
				case MessageType.Task:
					var nsqmTaskMessage = message.StructBuffer.ToStruct<NSQMTaskMessage>(Encoding.UTF8);
					var receivedTask = new ReceivedMessage()
					{
						Content = nsqmTaskMessage.Content,
						FromId = nsqmTaskMessage.FromId,
						Name = nsqmTaskMessage.TaskName,
						TaskId = nsqmTaskMessage.TaskId
					};

					var connection = new AcceptConnection(_nsqmSocket, nsqmTaskMessage);

					if (_publishedMessages.ContainsKey(receivedTask.TaskId))
					{
						_publishedMessages[receivedTask.TaskId].Activate(receivedTask, connection);
						_publishedMessages.Remove(receivedTask.TaskId);
					}
					else
					{
						Mailbox?.Invoke(receivedTask, connection);
					}
					break;
			}
		}

		public async Task Close()
		{
			await _nsqmSocket.Close();
		}

		public async Task<ApiResponseL3<User>?> Subscribe(string channelId)
		{
			var subscribeMessage = NSQMSubscribeMessage.Build(UserId, channelId, UserType.Producer, Encoding.UTF8);
			return await _nsqmSocket.SendAndReceive<User>(subscribeMessage, CancellationToken.None);
		}

		public async Task<MessageHandler> PublishMessage(string channelId, string taskName, byte[] content, Guid? receiverId = null)
		{
			if (receiverId == null)
				receiverId = Guid.Empty;

			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

			var taskData = new TaskData()
			{
				PhaseId = Guid.NewGuid(),
				ChannelId = channelId,
				SenderType = UserType.Producer,
				Status = Data.Extensions.TaskStatus.TaskStarted,
				AddresseeType = UserType.Consumer,
				Content = content,
				TaskId = Guid.NewGuid(),
				FromId = UserId,
				TaskName = taskName,
				ToId = receiverId.Value
			};

			var messageHandler = new MessageHandler();
			_publishedMessages.Add(taskData.TaskId, messageHandler);

			var message = await _httpClient.PostAsync(
				$"http://{_host}/CreateTask/",
				new StringContent(JsonSerializer.Serialize(taskData), Encoding.UTF8, "application/json"));

			var response = await message.Content.ReadAsStringAsync();
			var parsedResponse = JsonSerializer.Deserialize<ApiResponseL3<User>>(response);

			messageHandler.Response = parsedResponse;
	
			return messageHandler;
		}

		public async Task StreamMessage(string channelId, string taskName, byte[] content, Guid? receiverId = null)
		{
			if (receiverId == null)
				receiverId = Guid.Empty;

			var streamMessage = NSQMTaskMessage.Build(UserId, UserId, receiverId.Value, taskName, Guid.NewGuid(), channelId, Data.Extensions.TaskStatus.TaskStarted, content, UserType.Consumer, UserType.Producer, Encoding.UTF8, true);
			await _nsqmSocket.Send(streamMessage);
		}

		public void Dispose()
		{
			_httpClient.Dispose();
			_nsqmSocket.Dispose();
		}
	}
}
