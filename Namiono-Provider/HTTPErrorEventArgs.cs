using System;

namespace Namiono_Provider
{
	public class HTTPErrorEventArgs : EventArgs
	{
		public string Description { get; set; }
		public int StatusCode { get; set; }

		public HTTPErrorEventArgs(int statuscode, string message)
		{
			StatusCode = statuscode;
			Description = message;
		}
	}
}
