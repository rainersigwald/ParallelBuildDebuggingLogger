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

        private Dictionary<string, SortedSet<GlobalPropertyValue>> _globalPropertySubsets;

        public IEnumerable<GlobalPropertyValue> UniqueProperties
        {
            get
            {
                var properties = new SortedSet<GlobalPropertyValue>(GlobalProperties.Select(GlobalPropertyValue.FromKeyValuePair));
                var commonSubset = _globalPropertySubsets[StartedEventArgs.ProjectFile];

                properties.ExceptWith(commonSubset);

                return properties;
            }
        }
        public Dictionary<string, string> RemovedProperties { get; set; } = new Dictionary<string, string>();

        public ProjectBuildInfo(ProjectStartedEventArgs projectStartedEventArgs, IReadOnlyDictionary<int, ProjectBuildInfo> otherProjects, Dictionary<string, SortedSet<GlobalPropertyValue>> globalPropertySubsets)
        {
            StartedEventArgs = projectStartedEventArgs;
            ParentProjectInstanceId = projectStartedEventArgs.ParentProjectBuildEventContext.ProjectInstanceId;
            ProjectInstanceId = projectStartedEventArgs.BuildEventContext.ProjectInstanceId;
            GlobalProperties = projectStartedEventArgs.GlobalProperties ?? new Dictionary<string, string>();

            _globalPropertySubsets = globalPropertySubsets;

            if (GlobalProperties == null)
            {
                return;
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
                upDescription = $" + <{string.Join("; ", UniqueProperties.Select(up => $"{up.Name} = {( (up.Name == "CurrentSolutionConfigurationContents" || up.Name == "RestoreGraphProjectInput" ) ? "{elided}" : up.Value)}"))}>";
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
