using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pose.IL;

namespace Pose
{
    public class IsolationContext
    {
        internal static Shim[] Shims;

        public IsolationContext(Action entryPoint, params Shim[] shims)
        {
            Shims = shims;
            Type delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            MethodRewriter rewriter = MethodRewriter.CreateRewriter(entryPoint.Method);
            ((MethodInfo)(rewriter.Rewrite())).CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
    }
}