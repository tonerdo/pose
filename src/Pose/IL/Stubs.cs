using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

using Pose.Extensions;
using Pose.Helpers;

namespace Pose.IL
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
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                methodInfo.ReturnType,
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();

            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(methodInfo.IsStatic || methodInfo.IsForValueType() ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetIndexOfMatchingShim", new Type[] { typeof(MethodBase), typeof(Object) }));
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimReplacementMethod"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimDelegateTarget"));
            for (int i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);

            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            for (int i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForVirtualMethod(MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();
            if (methodInfo.IsForValueType())
                signatureParamTypes.Add(methodInfo.DeclaringType.MakeByRefType());
            else
                signatureParamTypes.Add(methodInfo.DeclaringType);

            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));
            parameterTypes.AddRange(signatureParamTypes);
            parameterTypes.Add(typeof(RuntimeMethodHandle));
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_virt_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                methodInfo.ReturnType,
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();

            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));
            ilGenerator.DeclareLocal(typeof(Type));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(Object).GetMethod("GetType"));
            ilGenerator.Emit(OpCodes.Stloc_3);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetRuntimeMethodForVirtual"));
            ilGenerator.Emit(OpCodes.Stloc_0);

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(methodInfo.IsForValueType() ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetIndexOfMatchingShim", new Type[] { typeof(MethodBase), typeof(Type), typeof(Object) }));
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimReplacementMethod"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimDelegateTarget"));
            for (int i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);

            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            for (int i = 0; i < signatureParamTypes.Count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, methodInfo.ReturnType, signatureParamTypes.ToArray(), null);
            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForConstructor(ConstructorInfo constructorInfo, OpCode opCode, bool forValueType)
        {
            ParameterInfo[] parameters = constructorInfo.GetParameters();

            List<Type> signatureParamTypes = new List<Type>();
            List<Type> parameterTypes = new List<Type>();

            if (forValueType)
                signatureParamTypes.Add(constructorInfo.DeclaringType.MakeByRefType());
            else
                signatureParamTypes.Add(constructorInfo.DeclaringType);

            signatureParamTypes.AddRange(parameters.Select(p => p.ParameterType));

            if (opCode == OpCodes.Newobj)
                parameterTypes.AddRange(parameters.Select(p => p.ParameterType));
            else
                parameterTypes.AddRange(signatureParamTypes);
            parameterTypes.Add(typeof(RuntimeMethodHandle));
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_ctor_{0}_{1}", constructorInfo.DeclaringType, constructorInfo.Name),
                opCode == OpCodes.Newobj ? constructorInfo.DeclaringType : typeof(void),
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();

            ilGenerator.DeclareLocal(constructorInfo.DeclaringType);
            ilGenerator.DeclareLocal(typeof(ConstructorInfo));
            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 2);
            ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(ConstructorInfo));
            ilGenerator.Emit(OpCodes.Stloc_1);

            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetIndexOfMatchingShim", new Type[] { typeof(MethodBase), typeof(Object) }));
            ilGenerator.Emit(OpCodes.Stloc_3);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Ldc_I4_M1);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue_S, rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimReplacementMethod"));
            ilGenerator.Emit(OpCodes.Stloc_2);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.Emit(OpCodes.Stloc, 4);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetShimDelegateTarget"));
            for (int i = 0; i < signatureParamTypes.Count - 1; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc, 4);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, constructorInfo.DeclaringType, signatureParamTypes.Skip(1).ToArray(), null);
            ilGenerator.Emit(OpCodes.Stloc_0);
            if (opCode == OpCodes.Call)
            {
                if (forValueType)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldloc_0);
                    ilGenerator.Emit(OpCodes.Stobj, constructorInfo.DeclaringType);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldloc_0);
                    ilGenerator.Emit(OpCodes.Starg, 0);
                }
            }
            ilGenerator.Emit(OpCodes.Br_S, returnLabel);

            ilGenerator.MarkLabel(rewriteLabel);
            if (opCode == OpCodes.Newobj)
            {
                if (forValueType)
                {
                    ilGenerator.Emit(OpCodes.Ldloca, 0);
                    ilGenerator.Emit(OpCodes.Initobj, constructorInfo.DeclaringType);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg, parameterTypes.Count - 1);
                    ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                    ilGenerator.Emit(OpCodes.Call, typeof(FormatterServices).GetMethod("GetUninitializedObject"));
                    ilGenerator.Emit(OpCodes.Stloc_0);
                }
            }

            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_2);
            int count = signatureParamTypes.Count;
            if (opCode == OpCodes.Newobj)
            {
                if (forValueType)
                    ilGenerator.Emit(OpCodes.Ldloca, 0);
                else
                    ilGenerator.Emit(OpCodes.Ldloc_0);
                count = count - 1;
            }
            for (int i = 0; i < count; i++)
                ilGenerator.Emit(OpCodes.Ldarg, i);
            ilGenerator.Emit(OpCodes.Ldloc_2);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), signatureParamTypes.ToArray(), null);
            ilGenerator.MarkLabel(returnLabel);
            if (opCode == OpCodes.Newobj)
                ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }

        public static DynamicMethod GenerateStubForMethodPointer(MethodInfo methodInfo)
        {
            List<Type> parameterTypes = new List<Type>();
            parameterTypes.Add(typeof(RuntimeMethodHandle));
            parameterTypes.Add(typeof(RuntimeTypeHandle));

            DynamicMethod stub = new DynamicMethod(
                string.Format("stub_ftn_{0}_{1}", methodInfo.DeclaringType, methodInfo.Name),
                typeof(IntPtr),
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();
            ilGenerator.DeclareLocal(typeof(MethodInfo));

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase) }));
            ilGenerator.Emit(OpCodes.Call, typeof(MethodRewriter).GetMethod("Rewrite"));
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, typeof(StubHelper).GetMethod("GetMethodPointer"));
            ilGenerator.Emit(OpCodes.Ret);
            return stub;
        }
    }
}