using NSQM.Core.Producer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSQM.Core.Model
{
	public class MessageResult
	{
		public ReceivedMessage Result { get; internal set; }
		public AcceptConnection Connection { get; internal set; }

		internal MessageResult(ReceivedMessage result, AcceptConnection connection)
		{
			Result = result;
			Connection = connection;
		}
	}
}
