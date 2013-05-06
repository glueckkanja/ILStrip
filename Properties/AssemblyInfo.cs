using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ILStrip")]
[assembly: AssemblyDescription(@"A simple tool to strip unused types/classes from a .Net assembly. 
Useful when ilmerge is used with large libraries - use the /internalize switch with ilmerge and 
afterwards have ilstrip remove types/classes that are not referenced by publicly accessible code.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Glück & Kanja Consulting AG")]
[assembly: AssemblyProduct("ILStrip")]
[assembly: AssemblyCopyright("Copyright © 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("38a3ac55-057c-4bd5-8291-d4c50763fb02")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.0")]
