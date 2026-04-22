using System.Collections.Generic;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// Shared data models used across all Unit classes.
    /// </summary>
    public class UnitHelper
    {
        /// <summary>
        /// Describes a single capability (method) that can be invoked by the AI.
        /// </summary>
        public class CapabilityInfo
        {
            /// <summary>Method name used to dispatch the call.</summary>
            public string Name;

            /// <summary>Human/AI-readable description of what this capability does.</summary>
            public string Description;

            /// <summary>List of parameters accepted by this capability.</summary>
            public List<ParameterInfo> Params = new List<ParameterInfo>();
        }

        /// <summary>
        /// Describes a single parameter belonging to a capability.
        /// </summary>
        public class ParameterInfo
        {
            /// <summary>Parameter name as expected in the JSON call.</summary>
            public string Name;

            /// <summary>C# type string, e.g. "string", "int", "bool".</summary>
            public string Type;

            /// <summary>Description of the parameter's purpose for AI context.</summary>
            public string Description;
        }
    }
}
