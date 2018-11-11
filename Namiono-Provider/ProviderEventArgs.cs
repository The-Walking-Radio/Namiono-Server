using System;

namespace Namiono_Provider
{
	public abstract class ProviderEventArgs<O> : IDisposable
	{
		internal ProviderEventArgs(Guid id, O result, string path)
		{
			Result = result;
			Id = id;
			Path = path;
		}

		/// <summary>
		/// Contains the object (HTTPContext, etc)  
		/// </summary>
		public O Result { get; private set; }

		/// <summary>
		/// Returns the current Ident of the listener.
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// Returns the local Path of the URL.
		/// </summary>
		public string Path { get; private set; }

		public void Dispose() { }
	}
}
