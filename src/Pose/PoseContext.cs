using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pose.IL;

namespace Pose
{
    public static class PoseContext
    {
        internal static Shim[] Shims { private set; get; }
        internal static Dictionary<MethodBase, DynamicMethod> StubCache { private set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            Shims = shims;
            StubCache = new Dictionary<MethodBase, DynamicMethod>();

            MethodRewriter rewriter = MethodRewriter.CreateRewriter(entryPoint.Method);
            if (entryPoint.Target == null)
                rewriter.Rewrite().DynamicInvoke();
            else
                rewriter.Rewrite().DynamicInvoke(entryPoint.Target);
        }
    }
}