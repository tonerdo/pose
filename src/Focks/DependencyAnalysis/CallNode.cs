using System.Collections.Generic;
using System.Reflection;

namespace Focks.DependencyAnalysis
{
    class CallNode
    {
        public string Name;
        public MethodBase Method;
        public List<CallNode> Dependants = new List<CallNode>();
        public List<CallNode> Dependencies = new List<CallNode>();
    }
}