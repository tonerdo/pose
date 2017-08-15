using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using Focks.DependencyAnalysis;
using Focks.Extensions;

namespace Focks.IL
{
    internal class CallGraphRewriter
    {
        private CallGraph _callGraph;
        private Shim[] _shims;
        private CallGraphRewriter() { }

        public static CallGraphRewriter CreateRewriter(CallGraph callGraph, Shim[] shims)
        {
            return new CallGraphRewriter { _callGraph = callGraph, _shims = shims };
        }

        public DynamicMethod Rewrite()
        {
            Dictionary<string, DynamicMethod> mapping = new Dictionary<string, DynamicMethod>();
            foreach (Shim shim in _shims)
            {
                MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(shim);
                DynamicMethod dynamicMethod = methodRewriter.RewriteMethodSignature(_shims);
                dynamicMethod = methodRewriter.RewriteMethodBody(dynamicMethod, mapping, _shims);
                mapping.Add(shim.Original.ToFullString(), dynamicMethod);
            }

            MethodRewriter[] rewriters = new MethodRewriter[_callGraph.Count];
            for (int i = 0; i < _callGraph.Count; i++)
            {
                CallNode node = _callGraph[i];
                MethodRewriter methodRewriter = MethodRewriter.CreateRewriter(node.Method);
                DynamicMethod dynamicMethod = methodRewriter.RewriteMethodSignature(_shims);
                mapping.Add(node.Method.ToFullString(), dynamicMethod);
                rewriters[i] = methodRewriter;
            }

            for (int i = 0; i < rewriters.Length; i++)
            {
                DynamicMethod dynamicMethod = mapping.ElementAt(i + _shims.Length).Value;
                rewriters[i].RewriteMethodBody(dynamicMethod, mapping, _shims);
            }

            return mapping[_callGraph.Single(c => c.Root).Method.ToFullString()];
        }
    }
}