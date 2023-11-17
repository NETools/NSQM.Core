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
    public class NSQMProducer
	{
		private HttpClient _httpClient;
		private NSQMBasicWebSocket _nsqmSocket;
		private string _host;

		public Guid UserId { get; private set; }
		
		public event Action<ReceivedTask, AcceptConnection>? TaskFinished;

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
			_nsqmSocket.MessageReceived += MessageReceived;

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
					TaskFinished?.Invoke(receivedTask, new AcceptConnection(_nsqmSocket, nsqmTaskMessage));
					break;
			}
		}

		public async Task<ApiResponseL3<Channel>?> OpenChannel(string channelName)
		{
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

			var message = await _httpClient.PostAsync(
				$"http://{_host}/CreateChannel/{channelName}", null);

			var response = await message.Content.ReadAsStringAsync();
			var instance = JsonSerializer.Deserialize<ApiResponseL3<Channel>>(response);
			return instance;
		}

		public async Task Close()
		{
			await _nsqmSocket.Close();
		}

		public async Task<ApiResponseL3<object>?> Subscribe(Channel? channel)
		{
			if(channel == null)
			{
				return ApiResponseL3<object>.Failed("User could not be added because channel was null", ApiResponseL1.ChannelNotFound);

			}
			var subscribeMessage = NSQMSubscribeMessage.Build(UserId, channel.ChannelId, UserType.Producer, Encoding.UTF8);
			return await _nsqmSocket.SendAndReceive<object>(subscribeMessage, CancellationToken.None);
		}

		public async Task<ApiResponseL3<User>?> PublishTask(Channel channel, string taskName, byte[] content)
		{
			_httpClient.DefaultRequestHeaders.Accept.Clear();
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

			var taskData = new TaskData()
			{
				PhaseId = Guid.NewGuid(),
				ChannelId = channel.ChannelId,
				SenderType = UserType.Producer,
				Status = Data.Extensions.TaskStatus.TaskStarted,
				AddresseeType = UserType.Consumer,
				Content = content,
				TaskId = Guid.NewGuid(),
				FromId = UserId,
				TaskName = taskName,
				ToId = Guid.Empty
			};

			var message = await _httpClient.PostAsync(
				$"http://{_host}/CreateTask/",
				new StringContent(JsonSerializer.Serialize(taskData), Encoding.UTF8, "application/json"));

			var response = await message.Content.ReadAsStringAsync();
			var instance = JsonSerializer.Deserialize<ApiResponseL3<User>>(response);
			return instance;
		}


	

		
	}
}
