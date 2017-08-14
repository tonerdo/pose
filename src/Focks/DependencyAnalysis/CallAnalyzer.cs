using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Focks.IL;
using static Focks.Utilities;

namespace Focks.DependencyAnalysis
{
    internal class CallAnalyzer
    {
        private MethodBase _root;
        private CallGraph _callGraph;
        private CallGraph _subGraph;

        private CallAnalyzer()
        {
            _callGraph = new CallGraph();
            _subGraph = new CallGraph();
        }

        public static CallAnalyzer CreateAnalyzer(MethodBase root)
        {
            return new CallAnalyzer { _root = root };
        }

        public CallGraph GenerateCallGraph()
        {
            GetDependencyNode(_root, null);
            return _callGraph;
        }

        public CallGraph GenerateCallSubGraph(List<CallNode> nodes)
        {
            foreach (CallNode node in nodes)
                GetDependantNodesForNode(node);
            
            return _subGraph;
        }

        private CallNode GetNodeFromGraph(MethodBase method) => _callGraph.FirstOrDefault(n => n.Name == BuildMethodString(method));

        private void GetDependencyNode(MethodBase method, CallNode parentNode)
        {
            CallNode node = GetNodeFromGraph(method);
            if (node != null)
            {
                if (!parentNode.Dependencies.Exists(d => d.Name == node.Name))
                    parentNode.Dependencies.Add(node);

                if (!node.Dependants.Exists(d => d.Name == parentNode.Name))
                    node.Dependants.Add(parentNode);

                return;
            }

            node = new CallNode { Name = BuildMethodString(method), Method = method };
            if (parentNode != null)
                node.Dependants.Add(parentNode);

            parentNode?.Dependencies.Add(node);
            _callGraph.Add(node);

            MethodDisassembler disassembler = new MethodDisassembler(method);
            List<MethodBase> dependencies = null;

            try
            {
                dependencies = disassembler.GetMethodDependencies();
            }
            catch { return; }

            foreach (var dependency in dependencies)
                GetDependencyNode(dependency, node);
        }

        private void GetDependantNodesForNode(CallNode node)
        {
            foreach (var dep in node.Dependants)
            {
                if (!_subGraph.Exists(d => d.Name == dep.Name))
                {
                    _subGraph.Add(dep);
                    GetDependantNodesForNode(dep);
                }
            }
        }
    }
}