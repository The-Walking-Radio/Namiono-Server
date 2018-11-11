﻿using System;

namespace Namiono_Frontend
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var s = string.Empty;
			using (var frontend = new Frontend(args))
			{
				while (s != "!exit")
					s = Console.ReadLine();

				frontend.Close();
			}
		}
	}
}
