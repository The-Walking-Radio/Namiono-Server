using System;

namespace Namiono_Backend
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var s = string.Empty;
			using (var backend = new Backend(args))
			{
				while (s != "!exit")
					s = Console.ReadLine();

				backend.Close();
			}
		}
	}
}
