using System;

namespace Namiono_Provider
{
	public class HTTPResponseEventArgs : EventArgs, IDisposable
	{
		public HTTPResponseEventArgs(string data)
			=> Data = data;

		public string Data { get; set; }

		public void Dispose()
			=> Data = string.Empty;
	}
}