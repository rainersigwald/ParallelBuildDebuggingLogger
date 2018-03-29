using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace ParallelBuildDebuggingLogger
{
    class ProjectBuildInfo
    {
        public ProjectStartedEventArgs StartedEventArgs { get; private set; }

        public int ProjectInstanceId { get; set; }

        public int ParentProjectInstanceId { get; set; }

        public IDictionary<string, string> GlobalProperties { get; set; }
        public Dictionary<string, string> UniqueProperties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> RemovedProperties { get; set; } = new Dictionary<string, string>();

        public ProjectBuildInfo(ProjectStartedEventArgs projectStartedEventArgs, IReadOnlyDictionary<int, ProjectBuildInfo> otherProjects)
        {
            StartedEventArgs = projectStartedEventArgs;
            ParentProjectInstanceId = projectStartedEventArgs.ParentProjectBuildEventContext.ProjectInstanceId;
            ProjectInstanceId = projectStartedEventArgs.BuildEventContext.ProjectInstanceId;
            GlobalProperties = projectStartedEventArgs.GlobalProperties;

            if (GlobalProperties == null)
            {
                return;
            }

            foreach (var propertyName in GlobalProperties.Keys)
            {
                string parentValue;
                if (otherProjects.ContainsKey(ParentProjectInstanceId) &&
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

            if (otherProjects.TryGetValue(ParentProjectInstanceId, out ProjectBuildInfo other))
            {
                foreach (var propertyName in other.GlobalProperties.Keys)
                {
                    if (!GlobalProperties.TryGetValue(propertyName, out _))
                    {
                        RemovedProperties[propertyName] = other.GlobalProperties[propertyName];
                    }
                }
            }
        }

        public override string ToString()
        {
            string upDescription = string.Empty;
            string rpDescription = string.Empty;

            if (UniqueProperties.Any())
            {
                upDescription = $" + <{string.Join("; ", UniqueProperties.Select(up => $"{up.Key} = {up.Value}"))}>";
            }

            if (RemovedProperties.Any())
            {
                rpDescription = $" - <{string.Join("; ", RemovedProperties.Select(rp => rp.Key))}>";
            }

            return
                $"{{{ProjectInstanceId}: \"{StartedEventArgs.ProjectFile}\"{upDescription}{rpDescription}}}";
        }
    }
}
