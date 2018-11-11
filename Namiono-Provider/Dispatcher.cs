using Namiono_Provider.IO;
using Namiono_Provider.Members;
using Namiono_Provider.Members.Abstract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Namiono_Provider
{
	public static class Dispatcher
	{
		public static Guid GetUserID(HttpListenerContext context)
		{
			var userid = Guid.Empty;

			if (context.Request.Cookies.Count != 0)
				if (context.Request.Cookies["UserID"] != null)
					userid = Guid.Parse(context.Request.Cookies["UserID"].Value);

			return userid;
		}

		private static string DeserializeContent<P>(string provider, string guid,
			string dict, HttpListenerContext context, string extradata)
		{


			var userid = GetUserID(context);
			var output = string.Empty;
			var collection = JsonConvert.DeserializeObject<List<P>>(dict);

			switch (provider)
			{
				case "navigation":
					
					output = "<ul>";

					foreach (var item in collection)
					{
						var url = Provider<P>.GetPropertyValue<string, P>(item, "Output");
						var icon = Provider<P>.GetPropertyValue<string, P>(item, "Image");
						var name = Provider<P>.GetPropertyValue<string, P>(item, "Name");
						var target = Provider<P>.GetPropertyValue<string, P>(item, "Target");
						output += string.Format("<li> <img src=\"/images/{1}\" width=\"16px\" />" +
							"<a href=\"{0}\">{2}</a></li>", url, icon, name);
					}

					output += "</ul>";

					var box = new ContentBox
					{
						Title = "Navigation",
						Content = output
					};

					return box.Output;

				case "news":
					output = "";
					foreach (var item in collection)
					{
						var url = Provider<P>.GetPropertyValue<string, P>(item, "Output");
						var icon = Provider<P>.GetPropertyValue<string, P>(item, "Image");
						var name = Provider<P>.GetPropertyValue<string, P>(item, "Name");
						var date = Provider<P>.GetPropertyValue<int, P>(item, "Created");

						var newsbox = new ContentBox
						{
							Title = name,
							Content = url,
							Date = new DateTime(date, DateTimeKind.Unspecified).ToShortDateString()
						};

						output += newsbox.Output;
					}

					return output;
				case "user":
					if (userid != Guid.Empty)
					{
						output = "";
						foreach (var item in collection)
						{
							var url = Provider<P>.GetPropertyValue<string, P>(item, "Output");
							var icon = Provider<P>.GetPropertyValue<string, P>(item, "Image");
							var name = Provider<P>.GetPropertyValue<string, P>(item, "Name");

							var newsbox = new ContentBox
							{
								Title = name,
								Content = url
							};

							output += newsbox.Output;
						}

						return output;
					}
					else
					{
						var userbox = new ContentBox("Anmeldung");
						userbox.Content = ReadTemplate(userid, "user-login", context, extradata, null);
						return userbox.Output;
					}
					
				default:
					output = "";

					foreach (var item in collection)
					{
						output = Provider<P>.GetPropertyValue<string, P>(item, "Output")
						.Replace("[#IMAGE#]", Provider<P>.GetPropertyValue<string, P>(item, "Image"));
					}

					break;
			}


















			return output;
		}

		/// <summary>
		/// Read a Template from the backend server or from a local file.
		/// </summary>
		/// <param name="fs">the Filesystem instance.</param>
		/// <param name="name">The Name of the Template</param>
		/// <param name="cookies">Cookies which needs to be passed to the Backend.</param>
		/// <returns>The parsed Template with Content.</returns>
		public static string ReadTemplate(Guid userid, string name, HttpListenerContext context, string extradata,
			Dictionary<string, string> parameters)
		{
			if (name.Contains("~"))
			{
				name = name.Replace("##UID##", userid.ToString());

				var parts = name.Split('~');
				var provider = parts[0];
				var id = parts[1];

				// In Version 3 des Dispatcher kann das Template auch Optionen beinhalten xD
				if (parts[0].Contains("(") && parts[0].Contains(")"))
				{
					provider = parts[0].Substring(0, parts[0].IndexOf("("));
					var options = ExtractString(parts[0], "(", ")").Split(';');

					foreach (var item in options)
					{
						var option = item.Split(':');
						if (option[0] == "Load")
						{
							if (!parameters.ContainsKey("Request-Type"))
								parameters.Add("Request-Type", option[1]);
						}

						if (option[0] == "Target")
						{
							if (!parameters.ContainsKey("Target"))
								parameters.Add("Target", option[1]);
							else
								parameters["Target"] = option[1];
						}
					}
				}

				var response = MakeBackendRequest(userid, provider, id, extradata, context, parameters);
				return response;
			}
			else
			{
				using (var _fs = new Filesystem(Filesystem.Combine(Environment.CurrentDirectory, "http_root")))
				{
					var tpl_path = string.Format("templates/{0}.tpl", name.Replace("-", "_"));
					if (!_fs.Exists(tpl_path))
						return string.Format("<p class=\"exclaim\">Template \"{0}\" not found!</p>", name);

					var output = _fs.Read(tpl_path).Result;
					return Encoding.UTF8.GetString(output, 0, output.Length);
				}
			}
		}

		public static string MakeBackendRequest(Guid userid, string provider, string id,
			string extradata, HttpListenerContext context, Dictionary<string, string> parameters)
		{
			var resp = string.Empty;
			var cookies = context.Request.Cookies;

			var _url = string.Format("http://localhost:91/?Provider={0}&Id={1}", provider, id);

			using (var wc = new HTTPClient(_url, cookies))
			{
				wc.Headers.Add("Extradata", extradata);
				wc.HTTPClientResponse += (sender, e) =>
				{
					switch (provider)
					{
						case "content":
							resp = DeserializeContent<Content>(provider, id, e.Data, context, extradata);
							break;
						case "navigation":
							resp = DeserializeContent<Navigation>(provider, id, e.Data, context, extradata);
							break;
						case "news":
							resp = DeserializeContent<News>(provider, id, e.Data, context, extradata);
							break;
						case "user":
							resp = DeserializeContent<User>(provider, id, e.Data, context, extradata);
							break;
					}
				};

				wc.HTTPClientError += (sender, e) =>
				{
					resp = string.Format("<p class=\"exclaim\">Cant load Template \"{0}\"!" +
						"<br/>(Error: {1})</p>", provider, e.Description);
				};

				wc.GetResponse();
			}

			while (resp.Contains("[[") && resp.Contains("]]"))
			{
				var tplname = GetTPLName(resp);
				resp = resp.Replace(string.Format("[[{0}]]", tplname),
					ReadTemplate(userid, tplname, context, extradata, parameters));
			}

			return resp;
		}

		static string ExtractString(this string input, string startDel = "[[", string endDel = "]]")
		{
			var start = input.IndexOf(startDel) + startDel.Length;

			return input.Substring(start, input.IndexOf(endDel) - start);
		}

		/// <summary>
		/// Returns the (extracted) name of the Template 
		/// </summary>
		/// <param name="input">The input string</param>
		/// <param name="startDel">The start Delimeter</param>
		/// <param name="endDel">The end Delimeter</param>
		/// <returns>The Name of the Template</returns>
		public static string GetTPLName(string input, string startDel = "[[", string endDel = "]]")
			=> input.ExtractString(startDel, endDel);
	}
}
