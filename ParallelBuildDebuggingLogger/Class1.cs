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
        private Dictionary<int, ProjectBuildInfo> buildInfos = new Dictionary<int, ProjectBuildInfo>();

        private string[] targetsOfInterest;

        public override void Initialize(IEventSource eventSource)
        {
            targetsOfInterest = Parameters?.Split(';');

            eventSource.ProjectStarted += ProjectStartedHandler;
            eventSource.TargetStarted += TargetStartedHandler;
        }

        private void TargetStartedHandler(object sender, TargetStartedEventArgs e)
        {
            if (targetsOfInterest != null && targetsOfInterest.Contains(e.TargetName, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Building target '{e.TargetName}' in {buildInfos[e.BuildEventContext.ProjectInstanceId]}");
            }
        }

        private void ProjectStartedHandler(object sender, ProjectStartedEventArgs projectStartedEventArgs)
        {
            var info = new ProjectBuildInfo(projectStartedEventArgs, buildInfos);

            if (buildInfos.ContainsKey(info.ProjectInstanceId))
            {
                Console.WriteLine($"Reentering project {info} from project {info.ParentProjectInstanceId} to build targets '{info.StartedEventArgs.TargetNames}'");
            }
            else
            {
                buildInfos.Add(info.ProjectInstanceId, info);
                Console.WriteLine($"Project {info} built by project {info.ParentProjectInstanceId} -- targets '{info.StartedEventArgs.TargetNames}'");
            }

        }
    }
}
