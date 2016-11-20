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
[assembly: AssemblyCopyright("Copyright © 2015 Jan Karger, Bastian Schmidt")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("2.1.4.0")]
[assembly: AssemblyFileVersion("2.1.4.0")]
[assembly: AssemblyInformationalVersion("SRC")]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

[assembly: NeutralResourcesLanguage("en-US")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]