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
            int count = parameters.Length + (methodInfo.IsStatic ? 0 : 1);

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();
            if (!methodInfo.IsStatic)
                signatureParamTypes.Add(methodInfo.DeclaringType);

            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.AddRange(signatureParamTypes);
            parameterTypes.Add(typeof(Type));
            parameterTypes.Add(typeof(int));
            parameterTypes.Add(typeof(string));

            DynamicMethod stub = new DynamicMethod(
                string.Format("dynamic_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                methodInfo.ReturnType,
                parameterTypes.ToArray());

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            for (int i = count; i < parameterTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);

            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("FindMethod"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            for (int i = 0; i < count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }
    }
}