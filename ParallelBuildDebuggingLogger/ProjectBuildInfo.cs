using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;

namespace ParallelBuildDebuggingLogger
{
    class ProjectBuildInfo
    {
        public int ProjectInstanceId { get; set; }

        public int ParentProjectInstanceId { get; set; }

        public IDictionary<string, string> GlobalProperties { get; set; }
        public Dictionary<string, string> UniqueProperties { get; set; } = new Dictionary<string, string>();

        public ProjectBuildInfo(ProjectStartedEventArgs projectStartedEventArgs, IReadOnlyDictionary<int, ProjectBuildInfo> otherProjects)
        {
            ParentProjectInstanceId = projectStartedEventArgs.ParentProjectBuildEventContext.ProjectInstanceId;
            ProjectInstanceId = projectStartedEventArgs.BuildEventContext.ProjectInstanceId;
            GlobalProperties = projectStartedEventArgs.GlobalProperties;

            foreach (var propertyName in GlobalProperties.Keys)
            {
                string parentValue;
                if (
                    otherProjects[ParentProjectInstanceId].GlobalProperties.TryGetValue(propertyName, out parentValue) &&
                    parentValue == GlobalProperties[propertyName])
                {
                    // inherited from parent; uninteresting
                }
                else
                {
                    UniqueProperties[propertyName] = GlobalProperties[propertyName];
                }
            }
        }
    }
}
