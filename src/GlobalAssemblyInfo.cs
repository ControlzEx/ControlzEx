using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("https://github.com/ControlzEx/ControlzEx")]
[assembly: AssemblyProduct("ControlzEx")]
[assembly: AssemblyCopyright("Copyright © 2015 - 2017 Jan Karger, Bastian Schmidt, James Willock")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Only increase AssemblyVersion for major releases. 
// Otherwise we get issues with nuget version ranges for dependent projects.
// Especially dependent projects which use strong names get problems with changing version numbers.
[assembly: AssemblyVersion("4.0.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]
[assembly: AssemblyInformationalVersion("SRC")]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]

[assembly: NeutralResourcesLanguage("en-US")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]