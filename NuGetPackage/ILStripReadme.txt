**********************************************************************
Project Web Site: https://github.com/glueckkanja/ILStrip
**********************************************************************
Licensed under the GPL: http://www.gnu.org/licenses/gpl.html
**********************************************************************


*** Add the following XML Elements to the *.csproj file and customize them:
  <Target Name="AfterBuild" Condition=" '$(Configuration)' == 'Release' ">
    <PropertyGroup>
      <TargetPathMerged>$(TargetDir)Merged\$(TargetFileName)</TargetPathMerged>
    </PropertyGroup>
    <ILMerge SearchDirectories="$(TargetDir)"
             InputAssemblies="$(TargetFileName);Library1.dll;Library2.dll"
             OutputFileName="$(TargetPathMerged)"
             TargetPlatform="v2" />
    <ILStrip InputFileName="$(TargetPathMerged)" />
  </Target>


*** ILMerge
Tries to find the latest ILMerge.exe binary (by path with usually includes the version) in the NuGet packages
folder and executes it with parameters.
The ILMerge NuGet package must be added manually to allow usage without it. Calling ILMerge.exe is used to avoid
hard dependencies and to facilitate showing ILMerge log output.

Parameters (elements in arrays are separated by semicolons ";"):
string[] InputAssemblies (required)
string OutputFileName (first input assembly will be overridden if omitted)
string[] SearchDirectories (/lib)
bool Log (will be sent to VS Build Output window)
bool Internalize
string[] InternalizeExclude (writes strings to temp file and passes file to ILMerge.exe, implies "Internalize")
string InternalizeExcludeFile (implies "Internalize")
string TargetPlatform
string TargetPlatformDir
bool WildCards


*** ILStrip
A simple tool to strip unused types/classes/resources from a .Net assembly. Useful when ILMerge is used with
large libraries - use the internalize function with ILMerge and afterwards have ILStrip remove types/classes
that are not referenced by publicly accessible code.

Parameters (multiple elements are separated by semicolons ";"):
string InputFileName (required)
string OutputFileName (input will be overridden if omitted)
string KeepTypes (Regex(s) with type names to keep even if not directly used)
string KeepResources (Regex(s) with resource names to keep - all others will be removed)
string RemoveResources (Regex(s) with resource names to remove)
string RenameResources (Regex(s) with resource names to rename, e.g. "AWSSDK.Amazon.S3.Model.*" will be renamed to "MyTool.Amazon.S3.Model.*")
int Verbose (0, 1 or 2)
