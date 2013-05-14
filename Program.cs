using System;
using System.IO;
using System.Text.RegularExpressions;

namespace GK
{
    class Program
    {
        static int Main(string[] args)
        {
            var ilStrip = new ILStrip();
            ilStrip.LogLine = (format, arg) => { Console.WriteLine(format, arg); };

            foreach (var arg in args)
            {
                var match = Regex.Match(arg, @"^(?:/|-)(\w+)(?::?)(.*)$");
                if (match.Success)
                {
                    var paramName = match.Groups[1].Value.ToLower();
                    if (paramName == "out")
                        ilStrip.OutputFileName = match.Groups[2].Value;
                    else if (paramName == "keeptypes")
                        ilStrip.KeepTypes = match.Groups[2].Value;
                    else if (paramName == "keepresources")
                        ilStrip.KeepResources = match.Groups[2].Value;
                    else if (paramName == "removeresources")
                        ilStrip.RemoveResources = match.Groups[2].Value;
                    else if (paramName == "renameresources")
                        ilStrip.RenameResources = match.Groups[2].Value;
                    else if (paramName == "v")
                        ilStrip.Verbose++;
                }
                else
                {
                    ilStrip.InputFileName = arg;
                }
            }
            
            if (string.IsNullOrEmpty(ilStrip.InputFileName))
            {
                Console.Error.WriteLine(ilStrip.CopyrightNotice);
                Console.Error.WriteLine("Usage: ilstrip inputfilename [/out:outputfilename] /keeptypes:regex[,regex] /keepresources:regex[,regex] /removeresources:regex[,regex] /renameresources:regex[,regex]");
                return 666;
            }

            ilStrip.Execute();
            return 0;
        }
    }
}
