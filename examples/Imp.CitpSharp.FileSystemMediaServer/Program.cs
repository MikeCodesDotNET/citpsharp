using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Imp.CitpSharp.FileSystemMediaServer
{
	public class Program
	{
		static Guid stringToGuid(string value)
		{
			var md5Hasher = MD5.Create();
			byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(value));
			return new Guid(data);
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("--------------------------------------------------------------------------");
			Console.WriteLine("      CitpSharp - Copyright 2018 David Butler / The Impersonal Stereo     ");
			Console.WriteLine("--------------------------------------------------------------------------");
			Console.WriteLine("                        Filesystem Media Server App                       ");
			Console.WriteLine("--------------------------------------------------------------------------");
			Console.WriteLine();


			string libraryRootPath = Directory.GetCurrentDirectory();
			IPAddress localIp = null;

			if (args.Length > 0)
				libraryRootPath = args[0];

			if (args.Length > 1)
			{
				if (!IPAddress.TryParse(args[1], out localIp))
				{
					Console.WriteLine("FAILED TO PARSE LOCAL IP ADDRESS");

					Console.WriteLine();
					Console.WriteLine("Press any key to exit...");
					Console.ReadKey();
				}
			}

			try
			{
				libraryRootPath = Path.GetFullPath(libraryRootPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine("INVALID LIBRARY PATH EXCEPTION");
				Console.WriteLine(ex);

				Console.WriteLine();
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();

				Environment.Exit(1);
			}
			

			var device = new FileSystemMediaServerDevice(stringToGuid(Environment.MachineName), Environment.MachineName, 
				"Online", "Filesystem Media Server", 
				1, 0, 0,
				libraryRootPath);

	        try
	        {
				var service = new CitpMediaServerService(new CitpDebugLogger(CitpLoggerLevel.Debug, true, true), device, 
					CitpServiceFlags.UseLegacyMulticastIp, preferredTcpListenPort: 56676, localIp: localIp);

				if (localIp == null)
					Console.WriteLine("Server started on any network adapter");
				else
					Console.WriteLine($"Server started on any local IP {localIp}");

				Console.WriteLine("Press any key to stop...");
				Console.WriteLine();
				Console.ReadKey();

				Console.WriteLine("Server stopping...");
				Console.WriteLine();

				service.Dispose();

				Console.WriteLine("Server stopped. Press any key to exit...");
				Console.ReadKey();

				Environment.Exit(0);
			}
			catch (Exception ex)
	        {
				Console.WriteLine();
				Console.WriteLine("UNHANDLED EXCEPTION");
				Console.WriteLine(ex);

				Console.WriteLine();
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();

				Environment.Exit(2);
			}
		}
	}
}
