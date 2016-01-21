//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.


using System.Reflection;
using System.Resources;

[assembly: AssemblyTitle("CitpSharp")]
[assembly: AssemblyDescription("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else

[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("The Impersonal Stereo")]
[assembly: AssemblyProduct("CitpSharp")]
[assembly: AssemblyCopyright("Copyright © David Butler / The Impersonal Stereo 2016")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]