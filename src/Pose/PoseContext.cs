using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Mono.Reflection;
using Pose.IL;

namespace Pose
{
    public static class PoseContext
    {
        internal static Shim[] Shims { private set; get; }
        internal static Dictionary<MethodBase, DynamicMethod> StubCache { private set; get; }
        internal static List<string> Instructions { set; get; }

        public static void Isolate(Action entryPoint, params Shim[] shims)
        {
            if (shims == null || shims.Length == 0)
            {
                entryPoint.Invoke();
                return;
            }

            Shims = shims;
            StubCache = new Dictionary<MethodBase, DynamicMethod>();
            Instructions = new List<string>();

            try
            {
                Type delegateType = typeof(Action<>).MakeGenericType(entryPoint.Target.GetType());
                MethodRewriter rewriter = MethodRewriter.CreateRewriter(entryPoint.Method);
                ((MethodInfo)(rewriter.Rewrite())).CreateDelegate(delegateType).DynamicInvoke(entryPoint.Target);
            }
            catch (TargetInvocationException)
            {
                foreach (string instruction in Instructions)
                    Console.WriteLine(instruction);
            }
        }

        public static void Decompile(MethodBase methodBase)
        {
            foreach (var instruction in methodBase.GetInstructions())
                Console.WriteLine(instruction);
        }
    }
}