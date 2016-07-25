using System;

namespace Imp.CitpSharp.DummyVisualizer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var device = new DummyVisualizerDevice(Guid.NewGuid(), Environment.MachineName, "Online");

            var service = new CitpVisualizerService(new CitpConsoleLogger(CitpLoggerLevel.Debug), device, true);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();

            service.Dispose();
        }
    }
}
