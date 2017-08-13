using System;
using Focks.DependencyAnalysis;

namespace Focks
{
    public class IsolationContext
    {
        public IsolationContext(Action entry, params Shim[] shims)
        {
            Analyzer analyzer = Analyzer.CreateAnalyzer(entry.Method);
            CallGraph callGraph = analyzer.GenerateCallGraph();
        }
    }
}