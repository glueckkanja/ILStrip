using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GK
{
    public abstract class BuildTaskBase : ITask
    {
        public delegate void LogLineDelegate(string format, params object[] arg);
        public LogLineDelegate LogLine { get; set; }
        
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        public BuildTaskBase()
        {
            LogLine = (format, args) =>
            {
                var buildMessageEventArgs = new BuildMessageEventArgs(
                    string.Format(format, args), null, this.GetType().Name, MessageImportance.High);
                BuildEngine.LogMessageEvent(buildMessageEventArgs);
            };
        }

        public abstract bool Execute();
    }
}
