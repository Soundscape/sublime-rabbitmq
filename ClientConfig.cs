using System;

namespace Sublime.RabbitMQ
{
	public class ClientConfig
	{
		#region Properties

		public string HostName { get; set; }
        public string Exchange { get; set; }
        public bool DeclareExchange { get; set; }
        public string QueueName { get; set; }

		#endregion
	}
}

