using System;
using System.IO;
using System.Net;

namespace Namiono_Provider
{
	public partial class HTTPClient : IDisposable
	{
		HttpWebRequest wc;
		public delegate void HTTPErrorEventHandler(HTTPClient sender, HTTPErrorEventArgs e);
		public event HTTPErrorEventHandler HTTPClientError;

		public delegate void HTTPResponseEventHandler(HTTPClient sender, HTTPResponseEventArgs e);
		public event HTTPResponseEventHandler HTTPClientResponse;

		public HTTPClient(string url, CookieCollection cookies,
			string ua = "", string accept = "application/json",
				string method = "GET")
		{
			Method = method;
			UserAgent = !string.IsNullOrEmpty(ua) ? ua : "Namiono-Client";
			Url = new Uri(url);
			KeepAlive = false;
			TimeOut = 90000;
			ContentType = string.Empty;
			StatusCode = HttpStatusCode.NotFound;
			Accept = accept;
			CredCache = new CredentialCache
			{
				{
					Url,
					"Basic",
					new NetworkCredential(UserAgent, "6452312135464321")
				}
			};

			wc = WebRequest.CreateHttp(Url);
			wc.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
		}

		public WebHeaderCollection Headers
			=> wc.Headers;

		public void GetResponse()
		{
			try
			{
				wc.UserAgent = UserAgent;
				wc.Method = Method;
				wc.KeepAlive = KeepAlive;
				wc.Timeout = TimeOut;
				wc.Accept = Accept;
				wc.Credentials = CredCache;

				using (var response = (HttpWebResponse)wc.GetResponse())
				{
					ContentLength = response.ContentLength;
					ContentType = response.ContentType.Split(';')[0];
					StatusCode = response.StatusCode;

					using (var strm = new BufferedStream(response.GetResponseStream()))
					{
						using (var str = new StreamReader(strm, true))
						{
							using (var evArgs = new HTTPResponseEventArgs(str.ReadToEnd()))
								HTTPClientResponse?.Invoke(this, evArgs);

							str.Close();
						}

						strm.Close();
					}

					response.Close();
				}
			}
			catch (WebException ex)
			{
				HTTPClientError?.Invoke(this, new HTTPErrorEventArgs((int)ex.Status, ex.Message));
			}
		}

		public void Dispose() { }

		public long ContentLength { get; private set; }

		public bool KeepAlive { set; get; }

		public string ContentType { set; get; }

		public HttpStatusCode StatusCode { get; private set; }

		public string Method { get; }

		public string UserAgent { get; set; }

		public string Accept { get; set; }

		public int TimeOut { get; set; }

		public Uri Url { get; set; }

		public CredentialCache CredCache { get; set; }
	}
}
