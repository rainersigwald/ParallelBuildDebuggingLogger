using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ParallelBuildDebuggingLogger
{
    public class Class1 : Logger
    {
        private StringBuilder log;

        private Dictionary<int, ProjectBuildInfo> buildInfos = new Dictionary<int, ProjectBuildInfo>();

        public override void Initialize(IEventSource eventSource)
        {
            log = new StringBuilder();

            eventSource.ProjectStarted += ProjectStartedHandler;
        }

        private void ProjectStartedHandler(object sender, ProjectStartedEventArgs projectStartedEventArgs)
        {
            var info = new ProjectBuildInfo(projectStartedEventArgs, buildInfos);

            buildInfos.Add(info.ProjectInstanceId, info);

            Console.WriteLine($"Project {info.ProjectInstanceId} built by project {info.ParentProjectInstanceId} with property diff {string.Join("; ", info.UniqueProperties.Select(up => $"{up.Key} = {up.Value}"))}");
        }
    }
}
