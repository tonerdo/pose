using System;
using System.Collections.Generic;
using System.Linq;

using Focks.DependencyAnalysis;
using Focks.Extensions;
using Focks.IL;

namespace Focks
{
    public class IsolationContext
    {
        public IsolationContext(Action entry, params Shim[] shims)
        {
            CallAnalyzer analyzer = CallAnalyzer.CreateAnalyzer(entry.Method, shims);
            CallGraph callGraph = analyzer.GenerateCallGraph();

            List<CallNode> shimNodes = new List<CallNode>();
            foreach (var shim in shims)
            {
                CallNode node = callGraph.FirstOrDefault(g => g.Name == shim.Original.ToFullString());
                if (node != null)
                    shimNodes.Add(node);
            }

            CallGraph subGraph = analyzer.GenerateCallGraph(shimNodes);
            CallGraphRewriter rewriter = CallGraphRewriter.CreateRewriter(subGraph, shims);
            var entrypoint = rewriter.Rewrite();

            object[] args = new object[shims.Length + 1];
            args[0] = entry.Target;
            for (int i = 0; i < shims.Length; i++)
                args[i + 1] = shims[i].Replacement.Target;

            entrypoint.Invoke(null, args);
        }
    }
}