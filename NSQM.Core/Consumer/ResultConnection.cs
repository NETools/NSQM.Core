using NSQM.Core.Networking;
using NSQM.Data.Extensions;
using NSQM.Data.Messages;
using NSQM.Data.Model.Persistence;
using NSQM.Data.Model.Response;
using System.Text;
using TaskStatus = NSQM.Data.Extensions.TaskStatus;

namespace NSQM.Core.Consumer
{
    public sealed class ResultConnection
    {
        private NSQMBasicWebSocket _nsqmWebSocket;
        private NSQMTaskMessage _taskMessage;
        private Guid _userId;

        internal ResultConnection(NSQMBasicWebSocket webSocket, NSQMTaskMessage taskMessage, Guid userId)
        {
            _nsqmWebSocket = webSocket;
            _taskMessage = taskMessage;
            _userId = userId;
        }

        public async Task<ApiResponseL3<TaskData>> Done(byte[] resultData, TaskStatus taskStatus)
        {
            var taskMessage = NSQMTaskMessage.Build(_userId, _userId, _taskMessage.FromId, _taskMessage.TaskName, _taskMessage.TaskId, _taskMessage.ChannelId, taskStatus, resultData, UserType.Producer, UserType.Consumer, Encoding.UTF8);

            return await _nsqmWebSocket.SendAndReceive<TaskData>(taskMessage, CancellationToken.None);
        }
    }
}
