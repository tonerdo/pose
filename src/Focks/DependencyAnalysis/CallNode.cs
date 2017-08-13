using System.Collections.Generic;
using System.Reflection;

namespace Focks.DependencyAnalysis
{
    internal class CallNode
    {
        public string Name { get; set; }
        public MethodBase Method { get; set; }
        public CallGraph Dependants { get; set; } = new CallGraph();
        public CallGraph Dependencies { get; set; } = new CallGraph();
    }
}