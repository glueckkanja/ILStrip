using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GK
{
    public class ILMerge : BuildTaskBase
    {
        [Required]
        public string[] InputAssemblies { get; set; }
        public string OutputFileName { get; set; }

        public string[] SearchDirectories { get; set; }
        public bool Log { get; set; }
        public bool Internalize { get; set; }
        public string[] InternalizeExclude { get; set; }
        public string InternalizeExcludeFile { get; set; }
        public string TargetPlatform { get; set; }
        public string TargetPlatformDir { get; set; }
        public bool WildCards { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(OutputFileName))
                OutputFileName = InputAssemblies[0];

            if (!Directory.Exists(Path.GetDirectoryName(OutputFileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFileName));

            var cleanupExcludeFile = false;
            if (InternalizeExclude != null && InternalizeExclude.Length > 0)
            {
                InternalizeExcludeFile = Path.GetTempFileName();
                File.WriteAllLines(InternalizeExcludeFile, InternalizeExclude);
                cleanupExcludeFile = true;
            }

            var ilmSearchPath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, "..", ".."));
            var ilMergePath = Directory.GetFiles(ilmSearchPath, "ilmerge.exe", SearchOption.AllDirectories).Max(x => x);

            var args = new StringBuilder();
            
            if (SearchDirectories != null && SearchDirectories.Length > 0)
                args.Append(string.Join("", SearchDirectories.Select(x => string.Format(@" /lib:""{0}""", x))));
            
            if (Log)
                args.Append(" /log");
            
            if (!string.IsNullOrWhiteSpace(InternalizeExcludeFile))
                args.AppendFormat(@" /internalize:""{0}""", InternalizeExcludeFile);
            else if (Internalize)
                args.Append(" /internalize");

            if (!string.IsNullOrWhiteSpace(TargetPlatform))
                args.AppendFormat(@" /targetplatform:{0}", TargetPlatform);
            if (!string.IsNullOrWhiteSpace(TargetPlatformDir))
                args.AppendFormat(@",""{0}""", TargetPlatformDir);

            if (WildCards)
                args.Append(" /wildcards");

            args.AppendFormat(@" /out:""{0}""", OutputFileName);

            args.Append(string.Join("", InputAssemblies.Select(x => @" """ + x + @"""")));

            var ilMergeProc = new ProcessHostRedirect() { FileName = ilMergePath, Arguments = args.ToString().Trim() };
            ilMergeProc.CommandLinePrefix = "";
            ilMergeProc.OutputDataHandler = (dataType, line) => { LogLine(line); };
            ilMergeProc.Execute();

            if (cleanupExcludeFile)
                File.Delete(InternalizeExcludeFile);

            return true;
        }
    }
}
