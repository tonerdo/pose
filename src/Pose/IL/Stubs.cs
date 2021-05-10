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
        private static MethodInfo s_getMethodFromHandleMethod;

        private static MethodInfo s_createRewriterMethod;

        private static MethodInfo s_rewriteMethod;

        private static MethodInfo s_getMethodPointerMethod;

        private static MethodInfo s_devirtualizeMethodMethod;

        static Stubs()
        {
            s_getMethodFromHandleMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) });
            s_createRewriterMethod = typeof(MethodRewriter).GetMethod("CreateRewriter", new Type[] { typeof(MethodBase), typeof(bool) });
            s_rewriteMethod = typeof(MethodRewriter).GetMethod("Rewrite");
            s_getMethodPointerMethod = typeof(StubHelper).GetMethod("GetMethodPointer");
            s_devirtualizeMethodMethod = typeof(StubHelper).GetMethod("DevirtualizeMethod", new Type[] { typeof(object), typeof(MethodInfo) });
        }

        public static DynamicMethod GenerateStubForDirectCall(MethodInfo method)
        {
            List<Type> signatureParamTypes = new List<Type>();
            if (!method.IsStatic)
            {
                Type thisType = method.DeclaringType;
                if (thisType.IsValueType)
                {
                    thisType = thisType.MakeByRefType();
                }

                signatureParamTypes.Add(thisType);
            }

            signatureParamTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));

            DynamicMethod stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub", method),
                method.ReturnType,
                signatureParamTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();

            if (method.GetMethodBody() == null || StubHelper.IsIntrinsic(method))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                for (int i = 0; i < signatureParamTypes.Count; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }

                ilGenerator.Emit(OpCodes.Call, method);
                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(IntPtr));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, method);
            ilGenerator.Emit(OpCodes.Ldtoken, method.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, s_getMethodFromHandleMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));

            // Rewrite method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, s_createRewriterMethod);
            ilGenerator.Emit(OpCodes.Call, s_rewriteMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));

            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, s_getMethodPointerMethod);
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Setup stack and make indirect call
            for (int i = 0; i < signatureParamTypes.Count; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, signatureParamTypes.ToArray(), null);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);

            return stub;
        }

        public static DynamicMethod GenerateStubForVirtualCall(MethodInfo method, TypeInfo constrainedType)
        {
            Type thisType = constrainedType.MakeByRefType();
            MethodInfo actualMethod = StubHelper.DevirtualizeMethod(constrainedType, method);

            List<Type> signatureParamTypes = new List<Type>();
            signatureParamTypes.Add(thisType);
            signatureParamTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));

            DynamicMethod stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_virt", method),
                method.ReturnType,
                signatureParamTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);
            
            ILGenerator ilGenerator = stub.GetILGenerator();

            ilGenerator.DeclareLocal(typeof(IntPtr));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, actualMethod);
            ilGenerator.Emit(OpCodes.Ldtoken, actualMethod.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, s_getMethodFromHandleMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));

            // Rewrite method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, s_createRewriterMethod);
            ilGenerator.Emit(OpCodes.Call, s_rewriteMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));

            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, s_getMethodPointerMethod);
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Setup stack and make indirect call
            for (int i = 0; i < signatureParamTypes.Count; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
                if (i == 0)
                {
                    if (!constrainedType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Ldind_Ref);
                        signatureParamTypes[i] = constrainedType;
                    }
                    else
                    {
                        if (actualMethod.DeclaringType != constrainedType)
                        {
                            ilGenerator.Emit(OpCodes.Ldobj, constrainedType);
                            ilGenerator.Emit(OpCodes.Box, constrainedType);
                            signatureParamTypes[i] = actualMethod.DeclaringType;
                        }
                    }
                }
            }
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, signatureParamTypes.ToArray(), null);

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);

            return stub;
        }

        public static DynamicMethod GenerateStubForVirtualCall(MethodInfo method)
        {
            Type thisType = method.DeclaringType.IsInterface ? typeof(object) : method.DeclaringType;

            List<Type> signatureParamTypes = new List<Type>();
            signatureParamTypes.Add(thisType);
            signatureParamTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));

            DynamicMethod stub = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("stub_virt", method),
                method.ReturnType,
                signatureParamTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            ILGenerator ilGenerator = stub.GetILGenerator();

            if ((method.GetMethodBody() == null && !method.IsAbstract) || StubHelper.IsIntrinsic(method))
            {
                // Method has no body or is a compiler intrinsic,
                // simply forward arguments to original or shim
                for (int i = 0; i < signatureParamTypes.Count; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }

                ilGenerator.Emit(OpCodes.Callvirt, method);
                ilGenerator.Emit(OpCodes.Ret);
                return stub;
            }

            ilGenerator.DeclareLocal(typeof(MethodInfo));
            ilGenerator.DeclareLocal(typeof(IntPtr));

            Label rewriteLabel = ilGenerator.DefineLabel();
            Label returnLabel = ilGenerator.DefineLabel();

            // Inject method info into instruction stream
            ilGenerator.Emit(OpCodes.Ldtoken, method);
            ilGenerator.Emit(OpCodes.Ldtoken, method.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, s_getMethodFromHandleMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Resolve virtual method to object type
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Call, s_devirtualizeMethodMethod);

            // Rewrite resolved method
            ilGenerator.MarkLabel(rewriteLabel);
            ilGenerator.Emit(method.DeclaringType.IsInterface ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Call, s_createRewriterMethod);
            ilGenerator.Emit(OpCodes.Call, s_rewriteMethod);
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));

            // Retrieve pointer to rewritten method
            ilGenerator.Emit(OpCodes.Call, s_getMethodPointerMethod);
            ilGenerator.Emit(OpCodes.Stloc_1);

            // Setup stack and make indirect call
            for (int i = 0; i < signatureParamTypes.Count; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i);
            }
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, method.ReturnType, signatureParamTypes.ToArray(), null);

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