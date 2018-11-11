using Namiono_Provider.IO;
using Namiono_Provider.Members.Abstract;
using System;
using System.Collections.Generic;

namespace Namiono_Provider.Members
{
	public abstract class Member<T>
	{
		internal Member(Guid id, string name)
		{
			Id = id.ToString();
			Name = name;
		}

		/// <summary>
		/// The name of the member.
		/// </summary>
		public virtual string Name { get; set; }

		public virtual string Password { get; set; }

		public virtual string Provider { get; set; } = Provider<T>.GetName();

		public virtual string Id { get; set; }

		public virtual string Image { get; set; }

		public virtual bool Locked { get; set; }

		public virtual bool Service { get; set; }

		public virtual string Level { get; set; }

		public virtual bool Active { get; set; }

		public virtual string Output { get; set; }

		public virtual string Admin { get; set; }

		public virtual int Created { get; set; }

		public virtual int Updated { get; set; }

		public virtual string Target { get; set; }

		public virtual bool Online { get; set; }

		public virtual string Design { get; set; }

		public virtual void Init() { }

		public virtual void Setup() { }

		public virtual bool Login(string name, string pass)
		{
			if (Locked || Service)
				return false;

			var n = name.ToLowerInvariant();
			var p = pass.ToLowerInvariant();

			var result = false;

			if (n == Name.ToLowerInvariant())
				if (Provider<T>.ComputeHash(pass) == Password)
				{
					Online = true;
					Update(Guid.Parse(Id), "online", "1");
					result = true;
				}

			return result;
		}

		public virtual bool Logout(Guid id)
		{
			Online = false;
			return Update(id, "online", "0");
		}

		public virtual void Download() { }

		public virtual void Start() { }

		public virtual void Stop() { }

		public virtual void Heartbeat() { }

		public virtual void Close() { }

		public virtual bool Update(Guid id, string option, string value)
		{
			var dbname = Provider<T>.GetName();
			var result = false;

			using (var db = new SQLDatabase<int>(dbname))
			{
				if (db.SQLInsert(string.Format("UPDATE '{0}' SET {1}='{2}' WHERE " +
					"id='{3}'", dbname, option, value, id)))
				{
					result = db.SQLInsert(string.Format("UPDATE '{0}' SET updated='{1}' WHERE " +
						"id='{2}'", dbname, Provider<T>.GetUnixTime(), id));
				}
			}

			return result;
		}

		public virtual void Remove(Guid id)
		{
			var dbname = Provider<T>.GetName();

			using (var db = new SQLDatabase<int>(dbname))
				db.SQLInsert(string.Format("DELETE FROM `{0}` WHERE id=`{1}` LIMIT 1", dbname, id));
		}

		/// <summary>
		/// Adds items to the Members submember list. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The Name of the Item</param>
		/// <param name="target">The Target of the Item.</param>
		/// <param name="content">The Content of the Item.</param>
		/// <param name="level">The access Level to access this Item.</param>
		/// <param name="active">The state of the Item.</param>
		/// <param name="locked">The locked state of the Item.</param>
		public virtual void Add(string name, string target, string content, int level, bool active, bool locked)
			=> Add(Guid.NewGuid(), name, target, content, level, active, locked);

		public virtual void Add(Guid id, string name, string target, string content, int level, bool active, bool locked)
		{
			var dbname = Provider<T>.GetName();
			var result = false;
			var sql = string.Format("INSERT INTO '{0}' (id, name, target, content, level, active, locked)" +
					"VALUES ('{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}');",
						dbname, id, name, target, content, level, active, locked);

			using (var db = new SQLDatabase<int>(dbname))
				result = db.SQLInsert(sql);

			if (!result)
				throw new Exception(string.Format("Add: SQL: {0}", sql));
		}

		public virtual int Count()
		{
			var dbname = Provider<T>.GetName();
			var count = 0;

			using (var db = new SQLDatabase<int>(dbname))
				count = db.Count(dbname, "id");

			return count;
		}

		public virtual void Dispose() { }
	}
}
