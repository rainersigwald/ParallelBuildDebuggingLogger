﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ParallelBuildDebuggingLogger
{
    public class ParallelBuildDebuggingLogger : Logger
    {
        private readonly Dictionary<int, ProjectBuildInfo> buildInfos = new Dictionary<int, ProjectBuildInfo>();

        private Dictionary<string, SortedSet<GlobalPropertyValue>> globalPropertySubsets = new Dictionary<string, SortedSet<GlobalPropertyValue>>();

        private List<ProjectStartedEventArgs> projectStartedEvents = new List<ProjectStartedEventArgs>();

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
                throw new NotImplementedException();
            }
        }

        private void ProjectStartedHandler(object sender, ProjectStartedEventArgs projectStartedEventArgs)
        {
            SortedSet<GlobalPropertyValue> commonProperties;
            if (globalPropertySubsets.TryGetValue(projectStartedEventArgs.ProjectFile, out commonProperties))
            {
                commonProperties.IntersectWith(projectStartedEventArgs.GlobalProperties?.Select(GlobalPropertyValue.FromKeyValuePair) ?? new SortedSet<GlobalPropertyValue>());
            }
            else
            {
                globalPropertySubsets.Add(projectStartedEventArgs.ProjectFile, new SortedSet<GlobalPropertyValue>(projectStartedEventArgs.GlobalProperties.Select(GlobalPropertyValue.FromKeyValuePair)));
            }

            projectStartedEvents.Add(projectStartedEventArgs);
        }

        public override void Shutdown()
        {
            using var file = new StreamWriter("PBDL.html", append: false);

            file.WriteLine("<html>");
            file.WriteLine(@"<style>
.projectdescription {
  position: relative;
  display: inline-block;
  border-bottom: 1px dotted black;
}

.projectdescription .tooltiptext {
  visibility: hidden;
  width: 500px;
  background-color: #555;
  color: #fff;
  text-align: center;
  border-radius: 6px;
  padding: 5px 0;
  position: absolute;
  z-index: 1;
  top: 150%;
  left: 50%;
  margin-left: -60px;
}

.projectdescription .tooltiptext::after {
  content: "";
  position: absolute;
  bottom: 100%;
  left: 50%;
  margin-left: -5px;
  border-width: 5px;
  border-style: solid;
  border-color: transparent transparent black transparent;
}

.uniqueproperties table {
    color: #fff;
}

.projectdescription:hover .tooltiptext {
  visibility: visible;
}
</style>
");
            file.WriteLine("<body>");
            file.WriteLine("<ul>");

            // TODO: anchor for id -1 ("start of build")

            foreach (var projectStartedEvent in projectStartedEvents)
            {
                var info = new ProjectBuildInfo(projectStartedEvent, buildInfos, globalPropertySubsets);

                if (buildInfos.ContainsKey(info.ProjectInstanceId))
                {
                    file.WriteLine($"<li><a href=\"#{info.ParentProjectInstanceId}\">Reentering</a> project {info.AnnotatedName} from project {info.ProjectIdLink} -- targets '{info.StartedEventArgs.TargetNames}'</li>");
                }
                else
                {
                    buildInfos.Add(info.ProjectInstanceId, info);
                    file.WriteLine($"<li id=\"{info.ProjectInstanceId}\">Project {info.AnnotatedName} built by project {info.ProjectIdLink} -- targets '{info.StartedEventArgs.TargetNames}'</li>");
                }
            }

            file.WriteLine("</ul>");
            file.WriteLine("</body>");
            file.WriteLine("</html>");
        }
    }
}
