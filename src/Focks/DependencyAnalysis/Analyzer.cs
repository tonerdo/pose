using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Mono.Reflection;
using static Focks.Utilities;

namespace Focks.DependencyAnalysis
{
    internal class Analyzer
    {
        private MethodBase _root;
        private CallGraph _callGraph;

        private Analyzer()
        {
            _callGraph = new CallGraph();
        }

        public static Analyzer CreateAnalyzer(MethodBase root)
        {
            return new Analyzer { _root = root };
        }

        public CallGraph GenerateCallGraph()
        {
            GetDependencyNode(_root, null);
            return _callGraph;
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

            IList<Instruction> instructions = null;
            try
            {
                instructions = method.GetInstructions();
            }
            catch { return; }

            var methodCalls = GetMethodCalls(instructions);

            foreach (var methodCall in methodCalls)
            {
                if (methodCall.MemberType == MemberTypes.Constructor)
                {
                    ConstructorInfo constructorInfo = methodCall as ConstructorInfo;
                    GetDependencyNode(constructorInfo, node);
                }
                else
                {
                    MethodInfo methodInfo = methodCall as MethodInfo;
                    GetDependencyNode(methodInfo, node);
                }
            }
        }

        static IEnumerable<MemberInfo> GetMethodCalls(IList<Instruction> instructions)
        {
            var methodCalls = instructions
                .Where(i => (i.Operand as MethodInfo) != null || (i.Operand as ConstructorInfo) != null)
                .Select(i => (i.Operand as MemberInfo));

            return methodCalls;
        }
    }
}