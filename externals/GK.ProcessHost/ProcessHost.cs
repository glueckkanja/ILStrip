using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace GK
{
    public class ProcessHost
    {
        public Process Process { get; private set; }
        public StreamWriter StdIn { get { return Process.StandardInput; } }
        public int ExitCode { get { return Process.ExitCode; } }

        public ProcessStartInfo StartInfo { get { return Process.StartInfo; } }
        public string FileName { get { return StartInfo.FileName; } set { StartInfo.FileName = value.Trim(new char[] { '\"' }); } }
        public string Arguments { get { return StartInfo.Arguments; } set { StartInfo.Arguments = value; } }

        public Dictionary<string, string> LaunchersByExtension { get; set; }
        public int[] ValidExitCodes { get; set; }


        public ProcessHost(string commandLine = null)
        {
            Process = new Process();
            StartInfo.UseShellExecute = false;

            CommandLine = commandLine;

            LaunchersByExtension = new Dictionary<string, string>() {
                { "bat", @"cmd /c """"{0}"" {1}""" },
                { "cmd", @"cmd /c """"{0}"" {1}""" },
                { "ps1", @"GkPs ""{0}"" {1}" }
            };

            ValidExitCodes = new int[] { 0 };
        }

        public string CommandLine
        {
            get { return this.ToString(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    FileName = "";
                    Arguments = "";
                }
                else
                {
                    var cmdLineSplit = SplitCommandLine(value);
                    FileName = cmdLineSplit[0];
                    Arguments = cmdLineSplit[1];
                }
            }
        }

        public virtual int Execute()
        {
            ExecuteStart();
            return ExecuteWaitForExit();
        }

        internal virtual void ExecuteStart()
        {
            var extension = Path.GetExtension(FileName).TrimStart(new char[] { '.' }).ToLower();
            if (LaunchersByExtension != null && LaunchersByExtension.ContainsKey(extension))
                CommandLine = string.Format(LaunchersByExtension[extension], FileName, Arguments);
            
            try
            {
                Process.Start();
            }
            catch (Exception e)
            {
                throw new ProcessHostException("Exception starting process: " + this.ToString(), e);
            }
        }

        internal virtual int ExecuteWaitForExit()
        {
            Process.WaitForExit();

            if (ValidExitCodes != null && !ValidExitCodes.Contains(Process.ExitCode))
                throw new ProcessHostException(string.Format("Process exited with return code {0}: {1}",
                    Process.ExitCode, this.ToString()));

            return Process.ExitCode;
        }

        public override string ToString()
        {
            var result = StartInfo.FileName;
            if (result.IndexOf(' ') >= 0)
                result = "\"" + result + "\"";
            if (!string.IsNullOrEmpty(StartInfo.Arguments))
                result += " " + StartInfo.Arguments;

            return result;
        }

        public static int Run(string commandLine, params int[] validExitCodes)
        {
            if (validExitCodes.Length == 0)
                validExitCodes = new int[] { 0 };

            var procHost = new ProcessHost(commandLine);
            procHost.ValidExitCodes = validExitCodes;
            procHost.Execute();
            return procHost.ExitCode;
        }

        public static string[] SplitCommandLine(string commandLine)
        {
            var cmdLineSplit = Regex.Match(commandLine, @"^(?<cmd>""[^""]*""|\S*)\s*(?<args>.*)?$");
            return new[] { cmdLineSplit.Groups["cmd"].Value, cmdLineSplit.Groups["args"].Value };
        }
    }

    
    public class ProcessHostRedirect : ProcessHost
    {
        public enum OutputDataType { StdOut, StdErr, CmdLine }

        public bool LogCommandLine { get; set; }
        public bool TeeToConsole { get; set; }
        public bool PassEndOfStream { get; set; }

        public delegate void OutputDataEventHandler(OutputDataType dataType, string line);
        public OutputDataEventHandler OutputDataHandlerRaw;
        public OutputDataEventHandler OutputDataHandler;

        public string CommandLinePrefix { get; set; }
        public string StdOutPrefix { get; set; }
        public string StdErrPrefix { get; set; }

        public StringBuilder OutputDataBuffer { get; private set; }


        public ProcessHostRedirect(string commandLine = null)
            : base(commandLine)
        {
            LogCommandLine = true;
            TeeToConsole = false;
            PassEndOfStream = false;

            CommandLinePrefix = "COMMAND LINE: ";
            StdOutPrefix = "";
            StdErrPrefix = "ERROR: ";

            OutputDataBuffer = new StringBuilder();


            StartInfo.RedirectStandardOutput = StartInfo.RedirectStandardError = true;
            Process.OutputDataReceived += (sender, e) => { OutputDataHandlerRaw(OutputDataType.StdOut, e.Data); };
            Process.ErrorDataReceived += (sender, e) => { OutputDataHandlerRaw(OutputDataType.StdErr, e.Data); };

            OutputDataHandlerRaw = DefaultRawOutputDataHandler;
            OutputDataHandler = StringBuilderOutputDataHandler;
        }

        public void DefaultRawOutputDataHandler(OutputDataType dataType, string line)
        {
            if (line == null)
            {
                // ignore end of stream
                if (!PassEndOfStream)
                    return;
            }
            else
            {
                switch (dataType)
                {
                    case OutputDataType.StdOut:
                        line = string.Concat(StdOutPrefix, line);
                        break;

                    case OutputDataType.StdErr:
                        line = string.Concat(StdErrPrefix, line);
                        break;

                    case OutputDataType.CmdLine:
                        line = string.Concat(CommandLinePrefix, line);
                        break;
                };

                if (TeeToConsole)
                    Console.WriteLine(line);
            }

            OutputDataHandler(dataType, line);
        }


        public void StringBuilderOutputDataHandler(OutputDataType dataType, string line)
        {
            lock (OutputDataBuffer)
                OutputDataBuffer.AppendLine(line);
        }

        internal override void ExecuteStart()
        {
            if (OutputDataHandlerRaw != null && LogCommandLine)
                OutputDataHandlerRaw(OutputDataType.CmdLine, FileName + (Arguments != "" ? " " + Arguments : ""));

            base.ExecuteStart();

            if (OutputDataHandlerRaw != null && StartInfo.RedirectStandardOutput)
                Process.BeginOutputReadLine();
            if (OutputDataHandlerRaw != null && StartInfo.RedirectStandardError)
                Process.BeginErrorReadLine();
        }

        internal override int ExecuteWaitForExit()
        {
            try
            {
                base.ExecuteWaitForExit();
            }
            finally
            {
                if (OutputDataHandlerRaw != null && StartInfo.RedirectStandardOutput)
                    Process.CancelOutputRead();
                if (OutputDataHandlerRaw != null && StartInfo.RedirectStandardError)
                    Process.CancelErrorRead();
            }

            return ExitCode;
        }


        public class OutputData
        {
            public OutputDataType Type { get; set; }
            public string Line { get; set; }
        }

        public IEnumerable<OutputData> ExecuteToEnum(Action<ProcessHostRedirect> startAction = null)
        {
            PassEndOfStream = true;

            var outDataQueue = new Queue<OutputData>();
            var enumOutDataReady = new AutoResetEvent(false);

            OutputDataHandler = (dataType, line) =>
            {
                lock (outDataQueue)
                    outDataQueue.Enqueue(new OutputData { Type = dataType, Line = line });
                enumOutDataReady.Set();
            };

            ExecuteStart();

            if (startAction != null)
                startAction(this);

            bool eofStdOut = false, eofStdErr = false;
            while (!eofStdOut || !eofStdErr)
            {
                enumOutDataReady.WaitOne();

                while (true)
                {
                    OutputData outData = null;
                    lock (outDataQueue)
                    {
                        if (outDataQueue.Count > 0)
                            outData = outDataQueue.Dequeue();
                    }

                    if (outData == null)
                        break;

                    if (outData.Line == null)
                    {
                        if (outData.Type == OutputDataType.StdOut)
                            eofStdOut = true;
                        if (outData.Type == OutputDataType.StdErr)
                            eofStdErr = true;
                    }
                    else
                    {
                        yield return outData;
                    }
                }
            }

            ExecuteWaitForExit();
        }

        public IEnumerable<string> ExecuteShellInteractiveCmdExe()
        {
            return ExecuteShellInteractive("prompt {0}$_\r\n");
        }

        public IEnumerable<string> ExecuteShellInteractiveBash()
        {
            return ExecuteShellInteractive("PS1='{0}\\n'\n");
        }

        public IEnumerable<string> ExecuteShellInteractive(string changePromptCommand, string prompt = null)
        {
            if (string.IsNullOrEmpty(prompt))
                prompt = string.Format("PROMPT-{0}-PROMPT", Guid.NewGuid().ToString());

            StartInfo.RedirectStandardInput = true;

            var commandResult = new StringBuilder();
            foreach (var outputData in ExecuteToEnum((host) => host.StdIn.Write(string.Format(changePromptCommand, prompt))))
            {
                if (outputData.Line == prompt)
                {
                    yield return commandResult.ToString();
                    commandResult.Length = 0;
                }
                else
                {
                    commandResult.AppendLine(outputData.Line);
                }
            }
        }
    }


    public class ProcessHostException : Exception
    {
        public ProcessHostException()
            : base() { }
        public ProcessHostException(string message)
            : base(message) { }
        public ProcessHostException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
