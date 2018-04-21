using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
            eventSource.BuildFinished += BuildFinishedHandler;
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
                //Console.WriteLine($"Reentering project {info} to build targets {info.StartedEventArgs.TargetNames}");
            }
            else
            {
                buildInfos.Add(info.ProjectInstanceId, info);
                //Console.WriteLine($"Project {info} built by project {info.ParentProjectInstanceId} -- targets '{info.StartedEventArgs.TargetNames}'");
            }
        }

        private void BuildFinishedHandler(object sender, BuildFinishedEventArgs e)
        {
            Console.WriteLine("graph TB");

            var projects = new ConcurrentDictionary<string, List<ProjectBuildInfo>>();

            foreach (var buildInfo in buildInfos.Values)
            {
                projects.GetOrAdd(
                    buildInfo.StartedEventArgs.ProjectFile,
                    new List<ProjectBuildInfo>())
                    .Add(buildInfo);
            }

            foreach (var project in projects)
            {
                var path = project.Key;
                var instances = project.Value;
                if (instances.Count > 1)
                {
                    Console.WriteLine($"  subgraph {Path.GetFileName(path)}");

                    var properties = new ConcurrentDictionary<string, string[]>();
                    var differentProperties = new ConcurrentDictionary<string, string[]>();

                    for (int i = 0; i < instances.Count; i++)
                    {
                        var instance = instances[i];

                        foreach (var property in instance.GlobalProperties)
                        {
                            properties.GetOrAdd(property.Key, new string[instances.Count])[i] = property.Value;
                        }
                    }

                    foreach (var item in properties)
                    {
                        if (!item.Value.All(p => p == item.Value[0]))
                        {
                            differentProperties[item.Key] = item.Value;
                        }
                    }

                    for (int i = 0; i < instances.Count; i++)
                    {
                        Console.WriteLine($"    p{instances[i].ProjectInstanceId}[\"{string.Join(" ", differentProperties.Select(kvp => kvp.Key + "=" + (string.IsNullOrEmpty(kvp.Value[i]) ? "(null)" : kvp.Value[i])))}\"]");
                    }
                    Console.WriteLine("  end");
                }
                else
                {
                    Console.WriteLine($"  p{project.Value[0].ProjectInstanceId}[\"{Path.GetFileName(path)}\"]");
                }
            }

            foreach (var buildInfo in buildInfos.Values)
            {
                if (buildInfo.ParentProjectInstanceId < 0)
                {
                    continue;
                }

                Console.WriteLine($"  p{buildInfo.ParentProjectInstanceId} --> p{buildInfo.ProjectInstanceId}");
            }
        }
    }
}