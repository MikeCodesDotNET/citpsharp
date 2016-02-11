using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
[assembly: AssemblyTitle("Imp.CitpSharp")]
[assembly:
	AssemblyDescription(
		"C# PCL implementation of CITP. The protocol allows transfer of status information, image thumbnails and streaming video frames between media servers, lighting desks and visualization software."
		)]

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else

[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("The Impersonal Stereo")]
[assembly: AssemblyProduct("Imp.CitpSharp")]
[assembly: AssemblyCopyright("Copyright © David Butler / The Impersonal Stereo 2016")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: InternalsVisibleTo("Imp.CitpSharp.Tests")]