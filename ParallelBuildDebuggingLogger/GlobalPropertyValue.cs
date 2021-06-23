using System;
using System.Collections.Generic;

namespace ParallelBuildDebuggingLogger
{
    record GlobalPropertyValue : IComparable<GlobalPropertyValue>
    {
        public string Name;
        public string Value;

        public static GlobalPropertyValue FromKeyValuePair(KeyValuePair<string, string> kvp)
        {
            return new GlobalPropertyValue { Name = kvp.Key, Value = kvp.Value };
        }

        public int CompareTo(GlobalPropertyValue other)
        {
            var nameCompare = Name.CompareTo(other.Name);

            if (nameCompare == 0)
            {
                return Value.CompareTo(Value);
            }

            return nameCompare;
        }
    }
}
