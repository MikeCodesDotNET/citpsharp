using System;

namespace Imp.CitpSharp.DummyVisualizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
			Console.WriteLine("--------------------------------------------------------------------------");
			Console.WriteLine("      CitpSharp - Copyright 2016 David Butler / The Impersonal Stereo     ");
			Console.WriteLine("--------------------------------------------------------------------------");
			Console.WriteLine("                        Dummy Visualizer Test App                         ");
			Console.WriteLine("--------------------------------------------------------------------------");
			Console.WriteLine();

			var device = new DummyVisualizerDevice(Guid.NewGuid(), Environment.MachineName, "Online");

	        try
	        {
				var service = new CitpVisualizerService(new CitpDebugLogger(CitpLoggerLevel.Debug, true, true), device,
					CitpServiceFlags.UseLegacyMulticastIp);

				Console.WriteLine("Server started on any network adapter");

				Console.WriteLine("Press any key to stop...");
				Console.WriteLine();
				Console.ReadKey();

				Console.WriteLine("Server stopping...");
				Console.WriteLine();

				service.Dispose();

				Console.WriteLine("Server stopped. Press any key to exit...");
				Console.ReadKey();
			}
			catch (Exception ex)
	        {
				Console.WriteLine();
				Console.WriteLine("UNHANDLED EXCEPTION");
				Console.WriteLine(ex);

				Console.WriteLine();
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
			}
        }
    }
}
