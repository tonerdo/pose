using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pose.IL;

namespace Pose
{
    public static class PoseContext
    {
        internal static Shim[] Shims { private set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            Shims = shims;
            Type delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
            MethodRewriter rewriter = MethodRewriter.CreateRewriter(entryPoint.Method);
            ((MethodInfo)(rewriter.Rewrite())).CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
        }
    }
}