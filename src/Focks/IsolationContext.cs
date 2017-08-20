using System;
using System.Collections.Generic;
using System.Linq;

using Focks.IL;

namespace Focks
{
    public class IsolationContext
    {
        internal static Type EntryPointType;

        public IsolationContext(Action entryPoint, params Shim[] shims)
        {
            EntryPointType = entryPoint.Target.GetType();
            Type delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            MethodRewriter rewriter = MethodRewriter.CreateRewriter(entryPoint.Method);
            rewriter.Rewrite().CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
    }
}