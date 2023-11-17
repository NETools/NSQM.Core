using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSQM.Core.Model
{
	public class ReceivedTask
	{
		public Guid FromId { get; set; }
		public Guid TaskId { get; set; }
		public string? Name { get; set; }
		public byte[] Content { get; set; }
	}
}
