using System.Reflection;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("The Impersonal Stereo")]
[assembly: AssemblyProduct("Imp.CitpSharp")]

[assembly: ComVisible(false)]
[assembly: Guid("259e73d2-19d6-4f06-95e9-90bfc98e5ddd")]

[assembly: AssemblyMetadata("ImplicitNullability.AppliesTo", "InputParameters, RefParameters, OutParametersAndResult")]