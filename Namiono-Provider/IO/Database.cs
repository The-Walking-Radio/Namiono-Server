using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SQLite;

namespace Namiono_Provider.IO
{
	public class SQLDatabase<T> : IDisposable
	{
		SQLiteConnection sqlConn;

		public SQLDatabase(string database)
		{
			var dataBase = Filesystem.Combine(Environment.CurrentDirectory, string.Concat(database, ".db"));
			sqlConn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dataBase));

			Open();
		}

		async void Open() => await sqlConn.OpenAsync();

		public int Count(string table, string condition, T value)
		{
			var x = 0;

			using (var cmd = new SQLiteCommand(
				string.Format("SELECT Count({0}) FROM {1} WHERE {0}=\"{2}\"",
					condition, table, string.Format("{0}", value)), sqlConn))
			{
				cmd.CommandType = CommandType.Text;
				x = Convert.ToInt32(cmd.ExecuteScalarAsync().Result);
			}

			return x;
		}

		public int Count(string table, string condition)
		{
			var x = 0;

			using (var cmd = new SQLiteCommand(
				string.Format("SELECT Count({0}) FROM {1}", condition, table), sqlConn))
			{
				cmd.CommandType = CommandType.Text;
				x = Convert.ToInt32(cmd.ExecuteScalarAsync().Result);
			}

			return x;
		}

		public Dictionary<T, NameValueCollection> SQLQuery(string sql)
		{
			var x = new Dictionary<T, NameValueCollection>();
			using (var cmd = new SQLiteCommand(sql, sqlConn))
			{
				var y = false;
				nonqry(ref sql, out y);

				var reader = cmd.ExecuteReader();
				var i = 0;
				while (reader.Read())
					if (!x.ContainsKey((T)Convert.ChangeType(i, typeof(T))))
					{
						x.Add((T)Convert.ChangeType(i, typeof(T)), reader.GetValues());
						i++;
					}

				reader.Close();
			}

			return x;
		}

		public bool SQLInsert(string sql)
		{
			var result = false;
			nonqry(ref sql, out result);

			return result;
		}

		public string SQLSingleQuery(string sql, string key)
		{
			var result = string.Empty;
			using (var cmd = new SQLiteCommand(sql, sqlConn))
			{
				var x = false;
				nonqry(ref sql, out x);

				var reader = cmd.ExecuteReaderAsync().Result;
				while (reader.Read())
					result = string.Format("{0}", reader[key]);

				reader.Close();
			}

			return result;
		}

		void nonqry(ref string sql, out bool result)
		{
			using (var cmd = new SQLiteCommand(sql, sqlConn))
			{
				cmd.CommandType = CommandType.Text;
				result = cmd.ExecuteNonQuery() != 0;
			}
		}

		public void Close()
			=> sqlConn.Close();

		public void HeartBeat()
			=> sqlConn.ReleaseMemory();

		public void Dispose()
		{
			Close();
			sqlConn.Dispose();
		}
	}
}
