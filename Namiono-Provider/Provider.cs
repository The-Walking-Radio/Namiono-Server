using Namiono_Provider.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace Namiono_Provider
{
	public class Provider<T>
	{
		/// <summary>
		/// Calls a bunch of <c>Methods</c> with the given names. each return value is one item.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="methodsToCall">String array with names of (a) Method(s) to Call (Ex: "Dispose")</param>
		/// <param name="args">List of (required) arguments for the method</param>
		public static object[] Invoke<P>(P obj, string[] methodsToCall, bool isSealed, params object[] args)
		{
			var retval = new object[methodsToCall.Length];

			for (var i = 0; i < methodsToCall.Length; i++)
				retval.SetValue(Invoke(obj, methodsToCall[i], isSealed, args), i);

			return retval;
		}

		public static string ComputeHash(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			var result = (byte[])null;
			using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
				result = md5.ComputeHash(Encoding.ASCII.GetBytes(text));

			return BitConverter.ToString(result).Replace("-", string.Empty).ToLower();
		}

		public static string GetName()
		{
			var n = typeof(T).ToString().Split('.');
			return n[n.Length - 1];
		}

		/// <summary>
		/// Calls a <c>Method</c> with the given name.
		/// </summary>
		/// <param name="obj">Location which contains the method.</param>
		/// <param name="methodName">Name of the method</param>
		/// <param name="args">Argument which needs to passed to the method-</param>
		/// <returns>The return value of the called Method</returns>
		public static object Invoke<I>(I obj, string methodName, bool issealed, params object[] args)
		{
			var result = (object)null;

			if (string.IsNullOrEmpty(methodName))
				return result;

			var parameters = new Type[args != null ? args.Length : 0];
			for (var i = 0; i < parameters.Length; i++)
				parameters.SetValue(args[i].GetType(), i);
			var instance = typeof(I);
			var m = !issealed ? instance.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.ExactBinding,
				null, CallingConventions.Any, parameters, null) : instance.GetMethod(methodName);

			if (m == null)
				throw new Exception(string.Format("Method '{0}' not found!", methodName));

			if (m.ReturnType != typeof(void))
				result = m.Invoke(obj, parameters.Length == 0 ? null : args);
			else
				m.Invoke(obj, parameters.Length == 0 ? null : parameters);

			return result;
		}

		/// <summary>
		/// Returns the elapsed seconds since the date 01.01.1970
		/// </summary>
		/// <returns></returns>
		public static double GetUnixTime()
			=> Math.Round(DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds, 0);

		/// <summary>
		/// Determines whether a member has events with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool HasEvent(string name)
			=> typeof(T).GetEvent(name, BindingFlags.Public) != null;

		/// <summary>
		/// Determines whether a member instance has a <c>Method</c> with the specified name.
		/// </summary>
		/// <returns><c>true</c> if a member instance has a <c>Method</c> with the specified name; otherwise, <c>false</c>.</returns>
		/// <param name="name">Name.</param>
		public static bool HasMethod(string name)
			=> typeof(T).GetMethod(name) != null;

		public static void Handle_Login_Request(Guid userid, ref HttpListenerContext context)
		{
			var cookie = new Cookie("UserID", userid.ToString());
			cookie.Expires = DateTime.Now.AddHours(2);
			cookie.Path = "/";

			context.Response.AppendCookie(cookie);
		}

		/// <summary>
		/// Determines whether a member instance has a <c>Property</c> with the specified name.
		/// </summary>
		/// <returns><c>true</c> if this instance has property the specified obj <c>name</c>; otherwise, <c>false</c>.</returns>
		/// <param name="name">Name.</param>
		public static bool HasProperty(string name)
			=> typeof(T).GetProperty(name) != null;

		/// <summary>
		/// Determines whether a member instance has a field with the specified name.
		/// </summary>
		/// <returns><c>true</c> when a member has a field the specified obj name; otherwise, <c>false</c>.</returns>
		/// <param name="name">Name.</param>
		public static bool HasField(string name)
			=> typeof(T).GetField(name) != null;

		/// <summary>
		/// Returns the Value from a property with the given Name.
		/// </summary>
		/// <typeparam name="TE">Member which contains the property.</typeparam>
		/// <param name="name">Name of the property</param>
		/// <returns>the Value of the property (as object)</returns>
		public static TO GetPropertyValue<TO, TI>(TI obj, string name)
			=> (TO)Convert.ChangeType(obj.GetType()
				.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
					.GetValue(obj), typeof(TO));

		/// <summary>
		/// Set a Value on the Property
		/// </summary>
		/// <param name="obj">The Object which contains the Property</param>
		/// <param name="name">The Name of the Property</param>
		/// <param name="value">The Value of the PRoperty</param>
		public static void SetPropertyValue<TY, VT>(TY obj, string name, VT value)
		{
			var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

			if (p.SetMethod != null)
				p.SetValue(obj, value);
			else
			{
				if (p.PropertyType.GetMethod("Add") != null)
					Provider<TY>.Invoke(p.PropertyType, new[] { "Add" }, true, value);
				else
					new InvalidOperationException(string.Format(
						"Unable to add Value '{0}' to Property '{1}'", value, name));
			}
		}
	}

	public class Provider<TI, T> : IDisposable
	{
		SQLDatabase<int> db;

		public Provider()
		{
			var n = typeof(T).ToString().Split('.');
			var dbname = n[n.Length - 1];
			var date_now = Provider<T>.GetUnixTime();
			var system_Guid = Guid.NewGuid();

			using (db = new SQLDatabase<int>(dbname))
			{
				var sql = string.Format(@"CREATE TABLE IF NOT EXISTS `{0}` (
						`_id`		INTEGER PRIMARY KEY AUTOINCREMENT,
						`id`		TEXT NOT NULL,
						`name`		TEXT NOT NULL,
						`content`	TEXT,
						`level`		INTEGER DEFAULT 1,
						`active`	INTEGER DEFAULT 0,
						`target`	TEXT,
						`locked`	INTEGER DEFAULT 1,
						`updated`	TEXT DEFAULT '{1}',
						'image'		TEXT DEFAULT 'big_circle.png',
						`created`	TEXT DEFAULT '{1}',
						`createdby`	TEXT DEFAULT '{2}',
						`online`	INTEGER DEFAULT 0,
						`hostname`	TEXT DEFAULT '127.0.0.1',
						`password`	TEXT DEFAULT '{3}',
						`moderator`	INTEGER DEFAULT 0,
						`service`	INTEGER DEFAULT 0
					);", dbname, date_now, system_Guid, Provider<T>.ComputeHash("-"));

				if (db.SQLInsert(sql))
				{
					if (dbname == "User")
					{
						if (db.Count(dbname, "id") == 0)
						{
							var sql_newGast = string.Format("INSERT INTO '{0}' (id, name, target, content, level, active)" +
							"VALUES ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}');",
							dbname, Guid.Empty, "Gast", "user", "-", "0", "1");
							db.SQLInsert(sql_newGast);

							var sql_newAdmin = string.Format("INSERT INTO '{0}' (id, name, target, content, level, active, locked, password)" +
							"VALUES ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}');",
							dbname, Guid.NewGuid(), "Admin", "user", "-", "255", "1", "1", Provider<T>.ComputeHash("admin123456"));
							db.SQLInsert(sql_newAdmin);

							var sql_newService = string.Format("INSERT INTO '{0}' (id, name, target, content, level, active, locked, password, service)" +
							"VALUES ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}');",
							dbname, system_Guid, "System", "user", "-", "255", "0", "0", Provider<T>.ComputeHash("432hj34gjh34g3"), "1");
							db.SQLInsert(sql_newService);
						}

					}

					if (dbname == "Navigation")
					{
						if (db.Count(dbname, "id") == 0)
						{
							var sql_startpage = string.Format("INSERT INTO '{0}' (id, name, content, level, active)" +
							"VALUES ('{1}', '{2}', '{3}', '{4}', '{5}');", dbname, Guid.Empty.ToString(), "Startseite", "/", "0", "1");
							db.SQLInsert(sql_startpage);
						}
					}

					if (dbname == "Content")
					{
						if (db.Count(dbname, "id") == 0)
						{
							var sql_header = string.Format("INSERT INTO '{0}' (id, name, level, active, locked, target)" +
							"VALUES ('E56191C5-99E7-4106-A5DC-E6F75FF8583D', '{1}', '{2}', '{3}', '{4}', '{5}');", dbname, "header", "0", "1", "1", "header");
							db.SQLInsert(sql_header);

							var sql_navigation = string.Format("INSERT INTO '{0}' (id, name, content, level, active, locked, target)" +
							"VALUES ('CD9A92CC-2255-46F2-AFBC-11060A9C697B', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", dbname, "navigation",
								"[[navigation~navigation~ffffffff-ffff-ffff-ffff-ffffffffffff]]", "0", "1", "1", "navigation");
							db.SQLInsert(sql_navigation);

							var sql_content = string.Format("INSERT INTO '{0}' (id, name, content, level, active, locked, target)" +
							"VALUES ('845D8E2A-3014-44AD-AB5B-26BA48EE60A0', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", dbname, "content",
								"[[news~news~ffffffff-ffff-ffff-ffff-ffffffffffff]]", "0", "1", "1", "content");
							db.SQLInsert(sql_content);

							var sql_aside = string.Format("INSERT INTO '{0}' (id, name, content, level, active, locked, target)" +
							"VALUES ('B92DFA02-2AB3-493A-9C60-A852B43E7F0E', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", dbname, "aside",
								"[[user~user~##UID##]]", "0", "1", "1", "aside");
							db.SQLInsert(sql_aside);

							var footer_text = "<p class=\"copyright\">Powered by [#APPNAME#] &copy; [#YEAR#] [#SITE_TITLE#]</p>" +
							"<p class=\"copyright\">This product includes GeoLite2 data created by<a href=\"http://www.maxmind.com\">MaxMind</a></p>" +
							"[#DEBUG_WARN#]";

							var sql_footer = string.Format("INSERT INTO '{0}' (id, name, content, level, active, locked, target)" +
							"VALUES ('594F714E-9E78-4CA5-BD38-06FBD79D5F0F', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", dbname, "footer",
								footer_text, "0", "1", "1", "footer");
							db.SQLInsert(sql_footer);

							var page_text = @"<header>[[content~header~E56191C5-99E7-4106-A5DC-E6F75FF8583D]]</header>" +
							"<nav id=\"navigation\" >[[content~navigation~CD9A92CC-2255-46F2-AFBC-11060A9C697B]]</nav>" +
							"<main id=\"content\">[[content~content~845D8E2A-3014-44AD-AB5B-26BA48EE60A0]]</main>" +
							"<aside>[[content~aside~B92DFA02-2AB3-493A-9C60-A852B43E7F0E]]</aside>" +
							"<footer>[[content~footer~594F714E-9E78-4CA5-BD38-06FBD79D5F0F]]</footer>";

							var sql_page = string.Format("INSERT INTO '{0}' (id, name, content, level, active, locked, target)" +
								"VALUES ('E7BA695E-E362-447E-A708-B6CA0AB86F3D', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", dbname, "page",
									page_text, "0", "1", "1", "page");
							db.SQLInsert(sql_page);
						}
					}

					if (dbname == "News")
					{
						if (db.Count(dbname, "id") == 0)
						{
							var news_text = @"Die Datenbank wurde erfolgreich am 01.11.18 um 21:17 Uhr auf Version 17 aktualisiert.";

							var sql_news = string.Format("INSERT INTO '{0}' (id, name, content, level, active, locked, target)" +
								"VALUES ('40E9B2B0-2003-4275-B377-DC8150AF4DC9', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", dbname, "[#APPNAME#] Installation",
									news_text, "0", "1", "1", "content");
							db.SQLInsert(sql_news);
						}
					}
				}

				var sql_query = db.SQLQuery(string.Format("SELECT * FROM `{0}`", dbname));
				var count = sql_query.Count;
				for (var i = 0; i < count; i++)
				{
					var mb = (T)Activator.CreateInstance(typeof(T), Guid.Parse(sql_query[i]["id"]), sql_query[i]["name"]);

					Provider<T>.SetPropertyValue(mb, "Output", sql_query[i]["content"]);
					Provider<T>.SetPropertyValue(mb, "Name", sql_query[i]["name"]);
					Provider<T>.SetPropertyValue(mb, "Admin", sql_query[i]["admin"]);
					Provider<T>.SetPropertyValue(mb, "Level", sql_query[i]["level"]);
					Provider<T>.SetPropertyValue(mb, "Image", sql_query[i]["image"]);
					Provider<T>.SetPropertyValue(mb, "Created", int.Parse(sql_query[i]["created"]));
					Provider<T>.SetPropertyValue(mb, "Updated", int.Parse(sql_query[i]["updated"]));

					Provider<T>.SetPropertyValue(mb, "Online", bool.Parse(sql_query[i]["online"] == "1" ? "True" : "False"));
					Provider<T>.SetPropertyValue(mb, "Password", sql_query[i]["password"]);
					Provider<T>.SetPropertyValue(mb, "Target", sql_query[i]["target"]);
					Provider<T>.SetPropertyValue(mb, "Locked", bool.Parse(sql_query[i]["locked"] == "1"
						? "True" : "False"));
					Provider<T>.SetPropertyValue(mb, "Active", bool.Parse(sql_query[i]["active"] == "1"
						? "True" : "False"));
					Provider<T>.SetPropertyValue(mb, "Service", bool.Parse(sql_query[i]["service"] == "1"
						? "True" : "False"));

					Members.Add(Guid.Parse(sql_query[i]["id"]), mb);
				}
			}
		}

		public Dictionary<Guid, T> Members { get; }
			= new Dictionary<Guid, T>();

		/// <summary>
		/// Adds initialize and starts members.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="key">Ident (Key)</param>
		/// <param name="member">Member</param>
		/// <param name="methods">Calls <c>Method(s)</c> in the passed order, after
		/// the provider has added the <c>Member</c>. (Ex: "Init", "Start")</param>
		/// <param name="args"></param>
		/// <example>Add(cref="key", cref="member", cref="Method")</example>
		public void Add<TO>(TO subscriber, Guid key,
			T member, string[] methods = null, params object[] args)
		{
			foreach (var evt in new[] { "Request", "Response", "Error", "Exception" })
				SubscribeEvent(subscriber, member, $"Handle_{evt}", evt);

			if (!Exist(key))
				Members.Add(key, member);

			var _methods = new[] { "Init", "Setup", "Start" };

			if (methods != null)
				for (var i = 0; i < methods.Length; i++)
					_methods.SetValue(methods, i);

			Provider<T>.Invoke(Members[key], _methods, false, args);
		}

		/// <summary>
		/// Remove the specified member.
		/// </summary>
		/// <param name="key">Name.</param>
		public bool Remove(Guid key)
			=> Members.Remove(key);

		/// <summary>
		/// Clears the member in this instance.
		/// </summary>
		public void Clear()
			=> Members.Clear();

		/// <summary>
		/// Checks if the specified key exists.
		/// </summary>
		/// <param name="key">Name</param>
		public bool Exist(Guid key)
			=> Members.ContainsKey(key);

		public void Close()
		{
			foreach (var m in Members.Values.Where(m => m != null))
				Provider<T>.Invoke(m, "Close", false);

			db.Close();
		}

		public void Dispose()
		{
			foreach (var m in Members.Values.Where(m => m != null))
				Provider<T>.Invoke(m, "Dispose", false);

			db.Dispose();
		}

		/// <summary>
		/// Fordert alle Objekte in der Auflistung an.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public List<T> RequestAll(string target = "")
		{
			var results = Members.Values.ToList();

			if (string.IsNullOrEmpty(target))
				results = results.Where(v => Provider<T>.GetPropertyValue<string, T>(v, "Target") == target)
					.ToList();

			return results;
		}
		/// <summary>
		/// Fordert ein bestimmtes Objekt an
		/// </summary>
		/// <param name="key"></param>
		/// <param name="target"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public T Request(Guid key)
		{
			var result = (T)Convert.ChangeType(null, typeof(T));
			if (Members.ContainsKey(key))
				result = Members[key];

			return result;
		}

		public T Login(string args)
		{
			var result = (T)Convert.ChangeType(Members[Guid.Empty], typeof(T));
			foreach (var member in Members.Values)
			{
				if ((bool)Provider<T>.Invoke(member, "Login", true, args))
				{
					result = member;
					break;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns the count of members 
		/// </summary>
		public int Count
			=> Members.Count;

		public void SubscribeEvent<TS, TM>(TS subscriber,
			TM member, string funcName, string eventName)
		{
			var method = subscriber.GetType().GetMethod(funcName,
				BindingFlags.Public | BindingFlags.Instance);

			var eventInfo = member.GetType().GetEvent(eventName);
			if (method == null || eventInfo == null)
				return;

			eventInfo.AddEventHandler(member, Delegate
				.CreateDelegate(eventInfo.EventHandlerType, subscriber, method));
		}
	}
}
