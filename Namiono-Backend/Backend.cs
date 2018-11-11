using Namiono_Provider;
using Namiono_Provider.Members;
using Namiono_Provider.Members.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Namiono_Backend
{
	public class Backend : IDisposable
	{
		public static Provider<Guid, _socket<HttpListener, HttpListenerContext, long, Exception>> HttpSockets
			= new Provider<Guid, _socket<HttpListener, HttpListenerContext, long, Exception>>();

		public static Provider<Guid, User> Users = new Provider<Guid, User>();
		public static Provider<Guid, Content> Content = new Provider<Guid, Content>();
		public static Provider<Guid, News> News = new Provider<Guid, News>();
		public static Provider<Guid, Navigation> Navigation = new Provider<Guid, Navigation>();

		private bool Handle_Auth_Request(HttpListenerContext context)
		{
			if (context.User == null)
				return false;

			return context.User.Identity.IsAuthenticated;
		}

		public void Handle_Request(object sender, _socket<HttpListener, HttpListenerContext, long, Exception>
				.RequestEventArgs<HttpListenerContext> e)
		{
			var response = string.Empty;
			var context = e.Result;

			// Protect the backend with an username and password. 
			if (!Handle_Auth_Request(context))
			{
				context.Response.ContentType = "text/html";
				context.Response.StatusCode = 401;
				context.Response.StatusDescription = "Not Authenticated!";
				HttpSockets.Request(e.Id).Send(response, context);
			}
			else
			{
				context.Response.ContentType = "application/json";
				context.Response.StatusCode = 200;
				context.Response.StatusDescription = "OK";

				var resp = "";
				if (e.Parameters.ContainsKey("Provider"))
				{
					var providers = e.Parameters["Provider"].Split(';');

					foreach (var provider in providers)
					{
						switch (provider)
						{
							case "content":
								resp = Handle_Provider_Request(Content, "Request", e.Parameters);
								break;
							case "user":
								resp = Handle_Provider_Request(Users, "Request", e.Parameters);
								break;
							case "navigation":
								resp = Handle_Provider_Request(Navigation, "Request", e.Parameters);
								break;
							case "news":
								resp = Handle_Provider_Request(News, "Request", e.Parameters);
								break;
							default:
								break;
						}
					}
				}

				response = resp;
				HttpSockets.Request(e.Id).Send(response, context);
			}
		}

		public string Handle_Provider_Request<TP>(TP provider, string action, Dictionary<string, string> parameters)
		{
			var id = string.Empty;
			var target = string.Empty;

			if (parameters.ContainsKey("Id"))
				id = parameters["Id"];

			if (parameters.ContainsKey("Provider"))
				target = parameters["Provider"];


			if (parameters.ContainsKey("Target"))
				target = parameters["Target"];

			if (id == "ffffffff-ffff-ffff-ffff-ffffffffffff")
				return JsonConvert.SerializeObject(Provider<TP>.Invoke(provider, "RequestAll", true, target));
			else
			{
				var x = new List<object>
					{
						Provider<TP>.Invoke(provider, "Request", true, Guid.Parse(id))
					};

				return JsonConvert.SerializeObject(x);
			}
		}

		public static string FirstCharToUpper(string str)
			=> str[0].ToString().ToUpper() + str.Substring(1).ToLower();

		public void Handle_Response
			(object sender, _socket<HttpListener, HttpListenerContext, long, Exception>.ResponseEventArgs<long> e)
				=> Console.WriteLine("Sent {0} bytes in response for {1}...", e.Result, e.Path);

		public void Handle_Error
			(object sender, _socket<HttpListener, HttpListenerContext, long, Exception>.ErrorEventArgs<Exception> e)
				=> Console.WriteLine("Error: {0}", e.Result);

		public Backend(string[] args)
		{
			var credentials = Convert.ToBase64String(Encoding.ASCII
				.GetBytes("Namiono-Client:6452312135464321"));

			var httpBEId = Guid.NewGuid();
			HttpSockets.Add(this, httpBEId, new _socket<HttpListener, HttpListenerContext,
				long, Exception>(httpBEId, new IPEndPoint(IPAddress.Any, 91), AuthenticationSchemes.Basic));

			HttpSockets.Members[httpBEId].Credentials = credentials;
		}

		public void Close()
		{
			HttpSockets.Close();

			Users.Close();
			News.Close();
			Content.Close();
			Navigation.Close();
		}

		public void Dispose()
		{
			HttpSockets.Dispose();
			Users.Dispose();
			News.Dispose();
			Content.Dispose();
			Navigation.Dispose();
		}
	}
}
