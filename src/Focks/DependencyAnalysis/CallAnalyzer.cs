using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Focks.IL;
using Focks.Extensions;

namespace Focks.DependencyAnalysis
{
    internal class CallAnalyzer
    {
        private MethodBase _root;

        private CallAnalyzer() { }

        public static CallAnalyzer CreateAnalyzer(MethodBase root)
        {
            return new CallAnalyzer { _root = root };
        }

        public CallGraph GenerateCallGraph()
        {
            CallGraph callGraph = new CallGraph();
            GetDependencyNode(callGraph, _root, null);
            return callGraph;
        }

        public CallGraph GenerateCallGraph(List<CallNode> nodes)
        {
            CallGraph callGraph = new CallGraph();

            foreach (CallNode node in nodes)
                GetDependantNodesForNode(callGraph, node);

            return callGraph;
        }

        private CallNode GetNodeFromGraph(CallGraph callGraph, MethodBase method) => callGraph.FirstOrDefault(n => n.Name == method.ToFullString());

        private void GetDependencyNode(CallGraph callGraph, MethodBase method, CallNode parentNode)
        {
            CallNode node = GetNodeFromGraph(callGraph, method);
            if (node != null)
            {
                if (!parentNode.Dependencies.Exists(d => d.Name == node.Name))
                    parentNode.Dependencies.Add(node);

                if (!node.Dependants.Exists(d => d.Name == parentNode.Name))
                    node.Dependants.Add(parentNode);

                return;
            }

            node = new CallNode { Name = method.ToFullString(), Method = method };
            if (parentNode != null)
                node.Dependants.Add(parentNode);
            else
                node.Root = true;

            parentNode?.Dependencies.Add(node);
            callGraph.Add(node);

            MethodDisassembler disassembler = new MethodDisassembler(method);
            List<MethodBase> dependencies = null;

            try
            {
                dependencies = disassembler.GetMethodDependencies();
            }
            catch { return; }

            foreach (var dependency in dependencies)
                GetDependencyNode(callGraph, dependency, node);
        }

        private void GetDependantNodesForNode(CallGraph callGraph, CallNode node)
        {
            foreach (var dep in node.Dependants)
            {
                if (!callGraph.Exists(d => d.Name == dep.Name))
                {
                    callGraph.Add(dep);
                    GetDependantNodesForNode(callGraph, dep);
                }
            }
        }
    }
}