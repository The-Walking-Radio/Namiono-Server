using System;
using Namiono_Backend;

namespace Namiono_Wrapper
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var s = string.Empty;
			using (var backend = new Backend<Guid>(args))
			{
				while (s != "!exit")
					s = Console.ReadLine();

				backend.Close();
			}
		}
	}
}
