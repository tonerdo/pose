using System.Collections.Generic;
using System.Reflection;

namespace Focks.DependencyAnalysis
{
    internal class CallNode
    {
        public string Name;
        public MethodBase Method;
        public CallGraph Dependants = new CallGraph();
        public CallGraph Dependencies = new CallGraph();
    }
}