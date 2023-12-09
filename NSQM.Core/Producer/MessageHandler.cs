using NSQM.Core.Model;
using NSQM.Data.Model.Persistence;
using NSQM.Data.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSQM.Core.Producer
{
	public sealed class MessageHandler
	{
		private SemaphoreSlim _resultSemaphore = new SemaphoreSlim(0, 1);
		private bool _resultLoaded;
		private MessageResult _result;
		private Action<MessageResult> _callBack;

		public ApiResponseL3<User>? Response { get; internal set; }
		
		internal MessageHandler() { }

		public async Task<MessageResult> GetResult(CancellationToken cancellationToken)
		{
			if (!_resultLoaded)
			{
				await _resultSemaphore.WaitAsync(cancellationToken);
				_resultSemaphore.Dispose();
			}
			_resultLoaded = true;
			return _result;
		}

		public MessageHandler ConfigureCallback(Action<MessageResult> callback)
		{
			_callBack = callback;
			return this;
		}

		internal void Acivate(ReceivedMessage message, AcceptConnection connection)
		{
			_result = new MessageResult(message, connection);
			_callBack?.Invoke(_result);
			_resultSemaphore.Release();
		}
	}
}
