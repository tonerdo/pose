using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

using Focks.Extensions;
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
            {
                if (methodInfo.IsForValueType())
                    signatureParamTypes.Add(methodInfo.DeclaringType.MakeByRefType());
                else
                    signatureParamTypes.Add(methodInfo.DeclaringType);
            }

            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.AddRange(signatureParamTypes);
            parameterTypes.Add(typeof(RuntimeMethodHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
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
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            for (int i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForRefTypeConstructor(ConstructorInfo constructorInfo)
        {
            ParameterInfo[] parameters = constructorInfo.GetParameters();

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();

            signatureParamTypes.Add(constructorInfo.DeclaringType);
            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.Add(typeof(RuntimeMethodHandle));
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_{0}_{1}", constructorInfo.DeclaringType, constructorInfo.Name),
                constructorInfo.DeclaringType,
                parameterTypes.ToArray());

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.DeclareLocal(constructorInfo.DeclaringType);
            ilGenerator.DeclareLocal(typeof(ConstructorInfo));
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
            ilGenerator.Emit(OpCodes.Call, typeof(FormatterServices).GetMethod("GetUninitializedObject"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(ConstructorInfo));
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            for (int i = 0; i < parameters.Length; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForValTypeConstructor(ConstructorInfo constructorInfo, OpCode opCode)
        {
            ParameterInfo[] parameters = constructorInfo.GetParameters();

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();

            signatureParamTypes.Add(constructorInfo.DeclaringType.MakeByRefType());
            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));
            if (opCode == OpCodes.Newobj)
                parameterTypes.AddRange(parameters.Select(p => p.ParameterType));
            else
                parameterTypes.AddRange(signatureParamTypes);
            parameterTypes.Add(typeof(RuntimeMethodHandle));
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_{0}_{1}", constructorInfo.DeclaringType, constructorInfo.Name),
                opCode == OpCodes.Newobj ? constructorInfo.DeclaringType : typeof(void),
                parameterTypes.ToArray());

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.DeclareLocal(constructorInfo.DeclaringType);
            ilGenerator.DeclareLocal(typeof(ConstructorInfo));
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            if (opCode == OpCodes.Newobj)
            {
                ilGenerator.Emit(OpCodes.Ldloca, 0);
                ilGenerator.Emit(OpCodes.Initobj, constructorInfo.DeclaringType);
            }
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(ConstructorInfo));
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Callvirt, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_2);
            int count = signatureParamTypes.Count;
            if (opCode == OpCodes.Newobj)
            {
                ilGenerator.Emit(OpCodes.Ldloca, 0);
                count = count - 1;
            }
            for (int i = 0; i < count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), signatureParamTypes.ToArray(), null);
            if (opCode == OpCodes.Newobj)
                ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForMethodPointer(MethodInfo methodInfo)
        {
            List<Type> parameterTypes = new List<Type>();
            parameterTypes.Add(typeof(RuntimeMethodHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
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

        public static DynamicMethod GenerateStubForShim(MethodInfo methodInfo, int index)
        {
            Shim shim = IsolationContext.Shims[index];
            ParameterInfo[] parameters = methodInfo.GetParameters();
            List<Type> parameterTypes = new List<Type>();
            if (!methodInfo.IsStatic)
            {
                if (methodInfo.IsForValueType())
                    parameterTypes.Add(methodInfo.DeclaringType.MakeByRefType());
                else
                    parameterTypes.Add(methodInfo.DeclaringType);
            }

            parameterTypes.AddRange(parameters.Select(p => p.ParameterType));

            DynamicMethod stub = new DynamicMethod(
                string.Format("shim_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                methodInfo.ReturnType,
                parameterTypes.ToArray());

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, index);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimInstance"));
            for (int i = 0; i < parameterTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);

            ilGenerator.Emit(OpCodes.Call, shim.Replacement.Method);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }
    }
}