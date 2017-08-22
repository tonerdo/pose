using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Focks.Helpers;

namespace Focks.IL
{
    internal static class Stubs
    {
        public static DynamicMethod GenerateStubForMethod(MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();
            if (!methodInfo.IsStatic)
                signatureParamTypes.Add(methodInfo.DeclaringType);

            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.AddRange(signatureParamTypes);
            parameterTypes.Add(typeof(RuntimeMethodHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("dynamic_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                methodInfo.ReturnType,
                parameterTypes.ToArray());

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            for (int i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForMethodPointer(MethodInfo methodInfo)
        {
            List<Type> parameterTypes = new List<Type>();
            parameterTypes.Add(typeof(RuntimeMethodHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("dynamic_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                typeof(IntPtr),
                parameterTypes.ToArray());

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            ilGenerator.Emit(OpCodes.Ldarg, 0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }
    }
}