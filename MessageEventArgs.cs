using System;

namespace Sublime.RabbitMQ
{
	public class MessageEventArgs : EventArgs
	{
		#region Properties

		public string Route { get; private set; }
		public Object Message { get; private set; }

		#endregion

		#region Constructors

		public MessageEventArgs (string route, object message)
		{
			this.Route = route;
			this.Message = message;
		}

		#endregion
	}
}

