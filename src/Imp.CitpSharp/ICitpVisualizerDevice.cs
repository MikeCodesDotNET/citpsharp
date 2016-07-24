using JetBrains.Annotations;

namespace Imp.CitpSharp
{
    /// <summary>
    ///     Interface allowing <see cref="CitpVisualizerService" /> access to properties of a visualizer and ability to satisfy
    ///     streaming requests.
    /// </summary>
    [PublicAPI]
    public interface ICitpVisualizerDevice : ICitpServerDevice { }
}