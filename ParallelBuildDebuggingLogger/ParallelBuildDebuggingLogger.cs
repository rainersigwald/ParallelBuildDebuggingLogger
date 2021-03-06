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
                globalPropertySubsets.Add(projectStartedEventArgs.ProjectFile, new SortedSet<GlobalPropertyValue>(projectStartedEventArgs.GlobalProperties?.Select(GlobalPropertyValue.FromKeyValuePair) ?? Array.Empty<GlobalPropertyValue>()));
            }

            projectStartedEvents.Add(projectStartedEventArgs);
        }

        public override void Shutdown()
        {
            using var file = new StreamWriter("PBDL.html", append: false);

            file.WriteLine("<html>");
            file.WriteLine($"<style>{ParallelBuildDebuggingLogger_Resources.Stylesheet}</style>");
            file.WriteLine($"<script>{ParallelBuildDebuggingLogger_Resources.Javascript}</script>");
            file.WriteLine("<body>");
            file.WriteLine("<input type=\"text\" id=\"searchbox\" onkeyup=\"filter()\" placeholder=\"Filter by project path or properties\" title=\"Type in a name\">");
            file.WriteLine("<input type=\"checkbox\" id=\"showreenter\" onclick=\"filter()\" checked><label for=\"showreenter\">Show reëntered projects</label>");
            file.WriteLine("<ul id=\"projects\">");

            // TODO: anchor for id -1 ("start of build")

            foreach (var projectStartedEvent in projectStartedEvents)
            {
                var info = new ProjectBuildInfo(projectStartedEvent, buildInfos, globalPropertySubsets);

                if (buildInfos.ContainsKey(info.ProjectInstanceId))
                {
                    file.WriteLine($"<li class=\"reentered\"><a href=\"#{info.ParentProjectInstanceId}\">Reentering</a> project {info.AnnotatedName} from project {info.ProjectIdLink} -- {(info.StartedEventArgs.TargetNames.Length > 0 ? $"targets <span class=\"targetnames\">{info.StartedEventArgs.TargetNames}</span>" : "default targets")}</li>");
                }
                else
                {
                    buildInfos.Add(info.ProjectInstanceId, info);
                    file.WriteLine($"<li id=\"{info.ProjectInstanceId}\">Project {info.AnnotatedName} built by project {info.ProjectIdLink} -- {(info.StartedEventArgs.TargetNames.Length > 0 ? $"targets <span class=\"targetnames\">{info.StartedEventArgs.TargetNames}</span>" : "default targets")}</li>");
                }
            }

            file.WriteLine("</ul>");
            file.WriteLine("</body>");
            file.WriteLine("</html>");
        }
    }
}
