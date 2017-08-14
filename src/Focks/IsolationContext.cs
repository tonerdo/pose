using System;
using System.Collections.Generic;
using System.Linq;

using Focks.DependencyAnalysis;
using static Focks.Utilities;

namespace Focks
{
    public class IsolationContext
    {
        public IsolationContext(Action entry, params Shim[] shims)
        {
            CallAnalyzer analyzer = CallAnalyzer.CreateAnalyzer(entry.Method);
            CallGraph callGraph = analyzer.GenerateCallGraph();

            List<CallNode> shimNodes = new List<CallNode>();
            foreach (var shim in shims)
            {
                CallNode node = callGraph.FirstOrDefault(g => g.Name == BuildMethodString(shim.Original));
                if (node != null)
                    shimNodes.Add(node);
            }

            CallGraph subGraph = analyzer.GenerateCallSubGraph(shimNodes);
        }
    }
}