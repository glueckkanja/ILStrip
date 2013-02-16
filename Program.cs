using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ILStrip
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ILStrip - Copyright Glück & Kanja Consulting AG 2013, see https://github.com/glueckkanja/ILStrip\r\n");


            var ilStrip = new ILStrip();

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
                    else if (paramName == "v")
                        ilStrip.Verbose++;
                }
                else
                {
                    ilStrip.InputFileName = arg;
                }
            }
            if (string.IsNullOrEmpty(ilStrip.OutputFileName)) ilStrip.OutputFileName = ilStrip.InputFileName;

            if (string.IsNullOrEmpty(ilStrip.InputFileName))
            {
                Console.Error.WriteLine("Usage: ilstrip inputfilename [/out:outputfilename] /keeptypes:regex[,regex] /keepresources:regex[,regex] /removeresources:regex[,regex]");
                return;
            }

            ilStrip.Run();
        }
    }
}
