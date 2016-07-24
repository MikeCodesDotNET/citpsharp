using System;

namespace Imp.CitpSharp.DummyVisualizer
{
    class Program
    {
        static void Main(string[] args)
        {
            var device = new DummyVisualizerDevice();

            var service = new CitpVisualizerService(new CitpConsoleLogger(CitpLoggerLevel.Debug), device, true);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();

            service.Dispose();
        }
    }
}
