using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using Namiono_Provider.IO;

namespace Namiono_Provider.Members.Sockets
{
	public class _socket<TY, REQ, RES, REX> : Member<string>
	{
		private TY socket;
		private IPEndPoint _endpoint;
		private readonly SocketState _state;
		private AuthenticationSchemes authtype;

		public delegate void RequestEventHandler(_socket<TY, REQ, RES, REX> sender, RequestEventArgs<REQ> e);
		public delegate void ResponseEventHandler(_socket<TY, REQ, RES, REX> sender, ResponseEventArgs<RES> e);
		public delegate void ErrorEventHandler(_socket<TY, REQ, RES, REX> sender, ErrorEventArgs<REX> e);

		public sealed class RequestEventArgs<O> : ProviderEventArgs<O>, IDisposable
		{
			public RequestEventArgs(Guid id, O result, string path) : base(id, result, path) { }

			public RequestEventArgs(Guid id, O result, Dictionary<string, string> postdata, string path)
				: base(id, result, path) => Parameters = postdata;

			/// <summary>
			/// Contains the POST and GET data which was send by the client.
			/// </summary>
			public Dictionary<string, string> Parameters { get; }
		}

		public class ResponseEventArgs<O> : ProviderEventArgs<O>
		{
			public ResponseEventArgs(Guid id, O result) : base(id, result, string.Empty) { }
		}

		public class ErrorEventArgs<O> : ProviderEventArgs<O>
		{
			public ErrorEventArgs(Guid id, O result) : base(id, result, string.Empty) { }
		}

		/// <summary>
		/// Will be raised when a requests from the client was received.
		/// Context and Parameters can be grabbed from here. 
		/// </summary>
		public event RequestEventHandler Request;

		/// <summary>
		/// Is raised when the response was sent to the client.
		/// The amount of bytes can be grabbed from here.
		/// </summary>
		public event ResponseEventHandler Response;

		/// <summary>
		/// Is raised on exceptions.
		/// </summary>
		public event ErrorEventHandler Error;

		public _socket(Guid id) : base(id, string.Empty)
		{
			Broadcast = false;
			Multicast = false;

			if (typeof(TY) == typeof(HttpListener))
			{
				socket = (TY)Convert.ChangeType(Activator.CreateInstance(typeof(TY)), typeof(TY));
			}
		}

		public _socket(Guid id, IPEndPoint endpoint, AuthenticationSchemes authentication,
			bool broadcast = false, bool multicast = false) : base(id, string.Empty)
		{
			_endpoint = endpoint;
			
			if (typeof(TY) == typeof(Socket))
			{
				Broadcast = broadcast;
				Multicast = multicast;

				socket = (TY)Convert.ChangeType(Activator.CreateInstance(typeof(TY),
					endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp), typeof(TY));

				_state = new SocketState { Socket = socket };
				Provider<TY>.Invoke(socket, "SetSocketOption", false, SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
			}

			if (typeof(TY) == typeof(HttpListener))
			{
				socket = (TY)Convert.ChangeType(Activator.CreateInstance(typeof(TY)), typeof(TY));
				authtype = authentication;
			}
		}

		/// <summary>
		/// Specifies whether the socket can be used for broadcast.
		/// </summary>
		public bool Broadcast { get; }

		/// <summary>
		/// Specifies whether the socket can be used for multicast.
		/// </summary>
		public bool Multicast { get; }

		/// <summary>
		/// Set or get credentials for authentication.
		/// </summary>
		public string Credentials { get; set; }

		/// <summary>
		/// Causes the socket to leave the current joined multicast group.
		/// </summary>
		/// <returns>true on success or false on error.</returns>
		public bool LeaveGroup()
		{
			if (!Multicast)
				return false;

			switch (_endpoint.AddressFamily)
			{
				case AddressFamily.InterNetwork:
					Provider<TY>.Invoke(socket, "SetSocketOption", false, SocketOptionLevel.IP, SocketOptionName.DropMembership,
						new MulticastOption(_endpoint.Address, IPAddress.Any));
					break;
				case AddressFamily.InterNetworkV6:
					Provider<TY>.Invoke(socket, "SetSocketOption", false, SocketOptionLevel.IPv6, SocketOptionName.DropMembership,
						new IPv6MulticastOption(_endpoint.Address, _endpoint.Address.ScopeId));
					break;
			}

			return true;
		}

		/// <summary>
		/// Sets and binds the socket. 
		/// </summary>
		public override void Init()
		{
			if (typeof(TY) == typeof(Socket))
			{
				if (Multicast)
				{
					Provider<TY>.Invoke(socket, "SetSocketOption", false, SocketOptionLevel.IP,
						SocketOptionName.MulticastTimeToLive, byte.MaxValue);

					Provider<TY>.Invoke(socket, "SetSocketOption", false, SocketOptionLevel.IPv6,
						SocketOptionName.MulticastTimeToLive, byte.MaxValue);

					Provider<TY>.Invoke(socket, "SetSocketOption", false, (_endpoint.AddressFamily == AddressFamily.InterNetwork) ?
						SocketOptionLevel.IP : SocketOptionLevel.IPv6, SocketOptionName.AddMembership,
							new MulticastOption(_endpoint.Address, _endpoint.AddressFamily
								== AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any));
				}
				else
					Provider<TY>.Invoke(socket, "Bind", false, _endpoint);
			}

			if (typeof(TY) == typeof(HttpListener))
			{
				var x = (HttpListener)Convert.ChangeType(socket, typeof(HttpListener));
				x.Prefixes.Add(string.Format("http://*:{0}/", _endpoint.Port));
				x.IgnoreWriteExceptions = true;
				if (authtype != AuthenticationSchemes.None)
				{
					x.AuthenticationSchemes = authtype;
					x.UnsafeConnectionNtlmAuthentication = true;
				}
			}
		}

		public override void Start()
		{
			if (typeof(TY) == typeof(Socket))
			{
				Provider<TY>.Invoke(socket, "BeginReceiveFrom", false, _state.Buffer,
					0, _state.Buffer.Length, SocketFlags.None, _endpoint,
						(AsyncCallback)Receive, _state);
			}

			if (typeof(TY) == typeof(HttpListener))
			{
				Provider<TY>.Invoke(socket, "Start", false);
				var x = (HttpListener)Convert.ChangeType(socket, typeof(HttpListener));
				x.BeginGetContext(Receive, socket);
			}
		}

		void Receive(IAsyncResult ar)
		{
			if (typeof(TY) == typeof(HttpListener))
			{
				var context = ((HttpListener)ar.AsyncState).EndGetContext(ar);
				var postdata = GetPostData(context.Request.HttpMethod, context);
				var path = GetContentType(context.Request.Url.LocalPath, ref context);

				using (var evtargs = new RequestEventArgs<REQ>(Guid.Parse(Id), (REQ)Convert.ChangeType(context, typeof(REQ)), postdata, path))
					Request?.Invoke(this, evtargs);

				var x = (HttpListener)Convert.ChangeType(socket, typeof(HttpListener));
				x.BeginGetContext(Receive, socket);
			}
		}

		/// <summary>
		/// Closes the current socket. Note: Multicast groups will also be leaved. 
		/// </summary>
		public override void Close()
			=> Provider<TY>.Invoke(socket, "Close", false);

		/// <summary>
		/// Info: Make internal an call to Close()!
		/// </summary>
		public override void Dispose()
			=> Provider<TY>.Invoke(socket, "Dispose", false);

		/// <summary>
		/// Sends data to the passed target (client).
		/// </summary>
		/// <param name="data"></param>
		/// <param name="client"></param>
		public void Send(byte[] data, IPEndPoint client)
		{
			var bytesSend = (RES)Convert.ChangeType(null, typeof(RES));

			if (typeof(TY) == typeof(Socket))
				bytesSend = (RES)Convert.ChangeType(Provider<TY>.Invoke(socket, "SendTo", false, data,
					0, data.Length, SocketFlags.None, Multicast ? _endpoint : client), typeof(RES));

			using (var evArgs = new ResponseEventArgs<RES>(Guid.Parse(Id), bytesSend))
				Response?.Invoke(this, evArgs);
		}

		/// <summary>
		/// Send a string as response back to the client.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="context"></param>
		public void Send(string data, HttpListenerContext context)
			=> Send(Encoding.UTF8.GetBytes(data), context);

		/// <summary>
		/// Send the Response (images, files, etc.) as bytes format back to the client.
		/// </summary>
		/// <param name="data">Data as byte array</param>
		/// <param name="context">The current context</param>
		public void Send(byte[] data, HttpListenerContext context)
		{
			context.Response.Headers.Remove("Server");
			context.Response.Headers["Server"] = "Namiono-Server";

			using (var strm = new BufferedStream(context.Response.OutputStream))
			{
				try
				{
					context.Response.ContentLength64 = data.Length;
					strm.Write(data, 0, data.Length);

					strm.Close();
				}
				catch (Exception ex)
				{
					using (var evtargs = new ErrorEventArgs<REX>(Guid.Parse(Id), (REX)Convert.ChangeType(ex, typeof(REX))))
						Error?.Invoke(this, evtargs);
				}
			}

			using (var evtargs = new ResponseEventArgs<RES>(Guid.Parse(Id), (RES)Convert.ChangeType(data.Length, typeof(RES))))
				Response?.Invoke(this, evtargs);
		}

		internal class SocketState
		{
			public byte[] Buffer = new byte[16384];
			public TY Socket;
		}

		/// <summary>
		/// Extracts the additional received POST and GET data from the client, like forms, images, etc. 
		/// </summary>
		/// <param name="method">HTTP Method (POST or GET)</param>
		/// <param name="context">The current context</param>
		/// <returns />
		private static Dictionary<string, string> GetPostData(string method, HttpListenerContext context)
		{
			var formdata = new Dictionary<string, string>();
			var path = context.Request.Url.AbsoluteUri;

			foreach (var header in new [] { "Extradata", "Provider", "Target", "Action", "Request-Type" })
			{
				if (context.Request.Headers[header] != null)
					formdata.Add(header, context.Request.Headers[header]);
			}

			switch (method)
			{
				case "POST":
					var encoding = context.Request.ContentEncoding;
					var ctype = context.Request.ContentType;
					var line = string.Empty;

					using (var reader = new StreamReader(new BufferedStream(context.Request.InputStream), encoding, true))
						line = reader.ReadToEnd();

					if (string.IsNullOrEmpty(line))
						return formdata;

					if (!string.IsNullOrEmpty(ctype) && ctype.Split(';')
							[0] != "application/x-www-form-urlencoded")
					{
						var boundary = ctype.Split('=')[1];

						if (string.IsNullOrEmpty(line))
							return null;

						var start = line.IndexOf(boundary, StringComparison.Ordinal) + boundary.Length + 2;
						line = line.Substring(start, line.LastIndexOf(boundary,
							StringComparison.Ordinal) + boundary.Length - start);
						var formparts = new List<string>();

						while (line.Contains(boundary))
						{
							if (line.StartsWith("Content-Disposition:"))
							{
								var tag = "Content-Disposition: form-data;";
								start = line.IndexOf(tag, StringComparison.Ordinal) + tag.Length;

								var end = line.IndexOf(boundary, StringComparison.Ordinal);
								formparts.Add(line.Substring(start, end - start).TrimStart());
								line = line.Remove(0, end);
							}

							if (!line.StartsWith(boundary))
								continue;

							var boundaryLength = boundary.Length + 2;

							if (line.Length <= boundaryLength)
								break;

							line = line.Remove(0, boundaryLength);
						}

						foreach (var item in formparts)
							if (item.Contains("filename=\""))
							{
								var posttag = item.Substring(0, item.IndexOf(";", StringComparison.Ordinal));
								if (string.IsNullOrEmpty(posttag))
									continue;

								var data = item;
								data = data.Remove(0, data.IndexOf("filename=\"",
									StringComparison.Ordinal) + "filename=\"".Length);

								var filename = data.Substring(0, data.IndexOf("\"", StringComparison.Ordinal));

								if (string.IsNullOrEmpty(filename))
									continue;

								if (filename.Contains("\\") || filename.Contains("/"))
								{
									var parts = filename.Split(filename.Contains("\\") ? '\\' : '/');
									filename = parts[parts.Length - 1];
								}

								data = data.Remove(0, data.IndexOf("Content-Type: ", StringComparison.Ordinal));
								data = data.Remove(0, data.IndexOf("\r\n", StringComparison.Ordinal) + 2);

								var filedata = context.Request.ContentEncoding.GetBytes
									(data.Substring(2, data.IndexOf("\r\n--",
										StringComparison.Ordinal)));

								if (filedata.Length == 0)
									continue;

								var uploadpath = Filesystem.Combine(path, filename);
								using (var fs = new Filesystem(uploadpath))
								{
									fs.Write(uploadpath, filedata);
									formdata.Add(posttag, uploadpath);
								}
							}
							else
							{
								var x = item.Replace("\r\n--", string.Empty).Replace("name=\"", string.Empty)
									.Replace("\"", string.Empty).Replace("\r\n\r\n", "|").Split('|');

								x[0] = x[0].Replace(" file", string.Empty);

								if (!formdata.ContainsKey(x[0]))
									formdata.Add(x[0], x[1]);
							}

						formparts.Clear();
					}
					else
					{
						foreach (var t in line.Split('&'))
						{
							if (!t.Contains("="))
								continue;

							var p = t.Split('=');
							if (formdata.ContainsKey(p[0]))
								continue;

							if (string.IsNullOrEmpty(p[1]))
								continue;

							formdata.Add(p[0], HttpUtility.UrlDecode(p[1]));
						}
					}
					break;
				case "GET":
					if (path.Contains("?") && path.Contains("="))
					{
						var getParams = HttpUtility.UrlDecode(path).Split('?')[1].Split('&');
						foreach (var t in getParams)
						{
							if (!t.Contains("=") || string.IsNullOrEmpty(t))
								continue;

							var p = t.Split('=');
							if (formdata.ContainsKey(p[0]))
								continue;

							if (string.IsNullOrEmpty(p[1]))
								continue;

							formdata.Add(p[0], p[1]);
						}
					}

					break;
			}

			return formdata;
		}

		string GetContentType(string path, ref HttpListenerContext context)
		{
			if (path.EndsWith(".css"))
				context.Response.ContentType = "text/css";

			if (path.EndsWith(".js"))
				context.Response.ContentType = "text/javascript";

			if (path.EndsWith(".htm") || path.EndsWith(".html"))
				context.Response.ContentType = "text/html";

			if (path.EndsWith(".png"))
				context.Response.ContentType = "image/png";

			if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
				context.Response.ContentType = "image/jpg";

			if (path.EndsWith(".gif"))
				context.Response.ContentType = "image/gif";

			if (path.EndsWith(".appcache"))
				context.Response.ContentType = "text/cache-manifest";

			if (path.EndsWith(".woff2"))
				context.Response.ContentType = "application/font-woff2";

			if (path.EndsWith(".ico"))
				context.Response.ContentType = "image/x-icon";

			return path.ToLowerInvariant();
		}
	}
}
