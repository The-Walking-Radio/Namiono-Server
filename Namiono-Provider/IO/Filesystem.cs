using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Namiono_Provider.IO
{
	public class Filesystem : IDisposable
	{
		private readonly string _rootDir;
		private readonly int _fscache;

		public Filesystem(string path, int cache = 4096)
		{
			_rootDir = Path.Combine(Directory.GetCurrentDirectory(), path);
			Directory.CreateDirectory(_rootDir);

			_fscache = cache;
		}

		private static string ReplaceSlashes(string path, string curSlash = "\\", string newSlash = "/")
			=> path.Replace(curSlash, newSlash).Trim();

		/// <summary>
		/// Excute a process
		/// </summary>
		/// <param name="path">Absolute path to the executeable</param>
		/// <param name="arg">Commandline arguments</param>
		/// <returns></returns>
		public static int Execute(string path, string arg)
		{
			var result = 1;
			using (var prc = new Process())
			{
				prc.StartInfo.Arguments = arg;
				prc.StartInfo.FileName = path;

				prc.Start();
				prc.WaitForExit();

				result = prc.ExitCode;
			}

			return result;
		}

		/// <summary>
		/// Deletes afile, this method check also the existance of the file.
		/// </summary>
		/// <param name="path"></param>
		public void Delete(string path)
		{
			if (Exists(path))
				File.Delete(path);
		}

		public static string ResolvePath(string _rootdir, string path, bool strip = true)
		{
			var p = path.Trim();

			if (p.StartsWith("/") && p.Length > 3 && strip)
				p = p.Remove(0, 1);

			p = ReplaceSlashes(Combine(_rootdir, p).Trim());

			return p;
		}

		/// <summary>
		/// Determain wether a File exists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool Exists(string path)
		{
			var x = ResolvePath(_rootDir, path);
			return File.Exists(x);
		}

		/// <summary>
		/// Combines 2 or more parts of paths segments.
		/// </summary>
		/// <param name="p1">First part</param>
		/// <param name="paths">Append these segments of paths to p1</param>
		/// <returns></returns>
		public static string Combine(string p1, params string[] paths)
		{
			var path = p1;

			foreach (var t in paths)
				path = ReplaceSlashes(Path.Combine(path, t)).Trim();

			return path;
		}

		/// <summary>
		/// Reads a binary file as byte array
		/// </summary>
		/// <param name="path">absolute path to the file.</param>
		/// <param name="offset">start position</param>
		/// <param name="count">How much bytes to read.</param>
		/// <returns>The content of the file or requested parts of it.</returns>
		public async Task<byte[]> Read(string path,
			int offset = 0, int count = 0)
		{
			byte[] data;
			ulong bytesRead;

			var p = ResolvePath(_rootDir, path);
			using (var fs = new BufferedStream(new FileStream(p,
				FileMode.Open, FileAccess.Read, FileShare.Read, _fscache,
					FileOptions.RandomAccess), _fscache))
			{
				data = new byte[(count == 0 || fs.Length < count) ? fs.Length : count];

				do
				{
					bytesRead = (ulong)await fs.ReadAsync(data, offset, data.Length);
				} while (bytesRead > ulong.MinValue);
			}

			return data;
		}

		/// <summary>
		/// Writes an array of bytes into a file. 
		/// </summary>
		/// <param name="path">Absolute path to the file</param>
		/// <param name="data">Data which should be written into a file.</param>
		/// <param name="offset">Position where the data should be written in the file.</param>
		/// <param name="count">The amount of bytes which should be written to the file.</param>
		/// <returns>True on success or false when errors occured.</returns>
		public void Write(string path, byte[] data, int offset = 0, int count = 0)
		{
			using (var fs = new BufferedStream(new FileStream(ResolvePath(_rootDir, path),
				FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write), _fscache))
				fs.Write(data, offset, count == 0 ? data.Length : count);
		}

		/// <summary>
		/// Writes an string into a file. The string gets internally converted into an array of bytes.
		/// </summary>
		/// <param name="path">Absolute path to the file</param>
		/// <param name="data">Data which should be written into a file.</param>
		/// <param name="offset">Position where the data should be written in the file.</param>
		/// <param name="count">The amount of bytes which should be written to the file.</param>
		public async void Write(string path,
			string data, Encoding encoding, int offset = 0, int count = 0)
		{
			var tmp = encoding.GetBytes(data);

			await Task.Run(() => Write(path, tmp, offset, count == 0
					? tmp.Length : count));
		}

		public void Dispose() { }

		/// <summary>
		/// The current directory of this instance.
		/// </summary>
		public string Root => _rootDir;
	}
}
