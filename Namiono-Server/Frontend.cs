using Namiono_Provider;
using Namiono_Provider.IO;
using Namiono_Provider.Members.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Namiono_Frontend
{
	public class Frontend : IDisposable
	{
		public static Provider<Guid, _socket<HttpListener, HttpListenerContext, long, Exception>> HttpSockets
			= new Provider<Guid, _socket<HttpListener, HttpListenerContext, long, Exception>>();

		public Frontend(string[] args)
		{
			var httpFEId = Guid.NewGuid();
			HttpSockets.Add(this, httpFEId, new _socket<HttpListener,
				HttpListenerContext, long, Exception>(httpFEId, new IPEndPoint(IPAddress.Any, 90), AuthenticationSchemes.None));
		}

		string GenerateSite(HttpListenerContext context, Dictionary<string, string> parameters)
		{
			var userid = Dispatcher.GetUserID(context);

			var extbytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(parameters));
			var extdata = Convert.ToBase64String(extbytes);
			var response = string.Empty;

			response = Dispatcher.ReadTemplate(userid, "site-content", context, extdata, parameters);

			while (response.Contains("[[") && response.Contains("]]"))
			{
				var tplname = Dispatcher.GetTPLName(response);
				response = response.Replace(string.Format("[[{0}]]", tplname),
					Dispatcher.ReadTemplate(userid, tplname, context, extdata, parameters));
			}

#if DEBUG
			response = response.Replace("[#DEBUG_WARN#]", "<p class=\"developer\">Entwickler-Version!" +
				" Software befindet sich (noch) in Entwicklung.</p>");
#else
			response = response.Replace("[#DEBUG_WARN#]", string.Empty);
#endif
			response = response.Replace("[#SITE_TITLE#]", "The Radio Fire and Ice");
			response = response.Replace("[#APPNAME#]", "Namiono");
			response = response.Replace("[#YEAR#]", string.Format("{0}", DateTime.Now.Year));
			response = response.Replace("[#MONTH#]", string.Format("{0}", DateTime.Now.Month));
			response = response.Replace("[#SITE_STYLE#]", "default");

			return response;
		}

		public void Handle_Request(object sender, _socket<HttpListener,
			HttpListenerContext, long, Exception>.RequestEventArgs<HttpListenerContext> e)
		{
			e.Result.Response.StatusCode = 200;
			e.Result.Response.StatusDescription = "OK";
			e.Result.Response.KeepAlive = e.Result.Request.KeepAlive;

			if (e.Path.EndsWith("/") || e.Path == "/" || e.Path.EndsWith(".html") || e.Path.EndsWith(".htm"))
			{
				var response = GenerateSite(e.Result, e.Parameters);

				e.Result.Response.ContentType = "text/html";
				HttpSockets.Request(e.Id).Send(response, e.Result);
			}
			else
			{
				var bytes = new byte[0];
				try
				{
					using (var fs = new Filesystem("http_root/"))
						bytes = fs.Read(e.Path).Result;
				}
				catch (Exception)
				{
					e.Result.Response.StatusCode = 404;
					e.Result.Response.StatusDescription = "Not Found!";
				}

				HttpSockets.Request(e.Id).Send(bytes, e.Result);
			}
		}

		public void Dispose()
			=> HttpSockets.Dispose();

		public void Close()
			=> HttpSockets.Close();
	}
}
