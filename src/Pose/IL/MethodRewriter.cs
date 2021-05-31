using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Mono.Reflection;

using Pose.Extensions;
using Pose.Helpers;
using Pose.IL.DebugHelpers;

namespace Pose.IL
{
    internal class MethodRewriter
    {
        private MethodBase m_method;

        private Type m_owningType;

        private bool m_isInterfaceDispatch;

        private int m_exceptionBlockLevel;

        private TypeInfo m_constrainedType;

        private static List<OpCode> s_IngoredOpCodes = new List<OpCode> { OpCodes.Endfilter, OpCodes.Endfinally };

        public static MethodRewriter CreateRewriter(MethodBase method, bool isInterfaceDispatch)
        {
            return new MethodRewriter { m_method = method, m_owningType = method.DeclaringType, m_isInterfaceDispatch = isInterfaceDispatch };
        }

        public MethodBase Rewrite()
        {
            List<Type> parameterTypes = new List<Type>();
            if (!m_method.IsStatic)
            {
                Type thisType = m_isInterfaceDispatch ? typeof(object) : m_owningType;
                if (!m_isInterfaceDispatch && m_owningType.IsValueType)
                {
                    thisType = thisType.MakeByRefType();
                }

                parameterTypes.Add(thisType);
            }

            parameterTypes.AddRange(m_method.GetParameters().Select(p => p.ParameterType));
            Type returnType = m_method.IsConstructor ? typeof(void) : (m_method as MethodInfo).ReturnType;

            DynamicMethod dynamicMethod = new DynamicMethod(
                StubHelper.CreateStubNameFromMethod("impl", m_method),
                returnType,
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            var methodBody = m_method.GetMethodBody();
            var locals = methodBody.LocalVariables;
            var targetInstructions = new Dictionary<int, Label>();
            var handlers = new List<ExceptionHandler>();

            var ilGenerator = dynamicMethod.GetILGenerator();
            var instructions = m_method.GetInstructions();

            foreach (var clause in methodBody.ExceptionHandlingClauses)
            {
                ExceptionHandler handler = new ExceptionHandler();
                handler.Flags = clause.Flags;
                handler.CatchType = clause.Flags == ExceptionHandlingClauseOptions.Clause ? clause.CatchType : null;
                handler.TryStart = clause.TryOffset;
                handler.TryEnd = clause.TryOffset + clause.TryLength;
                handler.FilterStart = clause.Flags == ExceptionHandlingClauseOptions.Filter ? clause.FilterOffset : -1;
                handler.HandlerStart = clause.HandlerOffset;
                handler.HandlerEnd = clause.HandlerOffset + clause.HandlerLength;
                handlers.Add(handler);
            }

            foreach (var local in locals)
                ilGenerator.DeclareLocal(local.LocalType, local.IsPinned);

            var ifTargets = instructions
                .Where(i => (i.Operand as Instruction) != null)
                .Select(i => (i.Operand as Instruction));

            foreach (Instruction instruction in ifTargets)
                targetInstructions.TryAdd(instruction.Offset, ilGenerator.DefineLabel());

            var switchTargets = instructions
                .Where(i => (i.Operand as Instruction[]) != null)
                .Select(i => (i.Operand as Instruction[]));

            foreach (Instruction[] _instructions in switchTargets)
            {
                foreach (Instruction _instruction in _instructions)
                    targetInstructions.TryAdd(_instruction.Offset, ilGenerator.DefineLabel());
            }

#if DEBUG
            Debug.WriteLine("\n" + m_method);
#endif

            foreach (var instruction in instructions)
            {
#if DEBUG
                Debug.WriteLine(instruction);
#endif

                EmitILForExceptionHandlers(ilGenerator, instruction, handlers);

                if (targetInstructions.TryGetValue(instruction.Offset, out Label label))
                    ilGenerator.MarkLabel(label);

                if (s_IngoredOpCodes.Contains(instruction.OpCode)) continue;

                switch (instruction.OpCode.OperandType)
                {
                    case OperandType.InlineNone:
                        EmitILForInlineNone(ilGenerator, instruction);
                        break;
                    case OperandType.InlineI:
                        EmitILForInlineI(ilGenerator, instruction);
                        break;
                    case OperandType.InlineI8:
                        EmitILForInlineI8(ilGenerator, instruction);
                        break;
                    case OperandType.ShortInlineI:
                        EmitILForShortInlineI(ilGenerator, instruction);
                        break;
                    case OperandType.InlineR:
                        EmitILForInlineR(ilGenerator, instruction);
                        break;
                    case OperandType.ShortInlineR:
                        EmitILForShortInlineR(ilGenerator, instruction);
                        break;
                    case OperandType.InlineString:
                        EmitILForInlineString(ilGenerator, instruction);
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        EmitILForInlineBrTarget(ilGenerator, instruction, targetInstructions);
                        break;
                    case OperandType.InlineSwitch:
                        EmitILForInlineSwitch(ilGenerator, instruction, targetInstructions);
                        break;
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineVar:
                        EmitILForInlineVar(ilGenerator, instruction);
                        break;
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                        EmitILForInlineMember(ilGenerator, instruction);
                        break;
                    default:
                        throw new NotSupportedException(instruction.OpCode.OperandType.ToString());
                }
            }

#if DEBUG
            var ilBytes = ilGenerator.GetILBytes();
            var browsableDynamicMethod = new BrowsableDynamicMethod(dynamicMethod, new DynamicMethodBody(ilBytes, locals));
            Debug.WriteLine("\n" + dynamicMethod);

            foreach (var instruction in browsableDynamicMethod.GetInstructions())
            {
                Debug.WriteLine(instruction);
            }
#endif
            return dynamicMethod;
        }

        private void EmitILForExceptionHandlers(ILGenerator ilGenerator, Instruction instruction, List<ExceptionHandler> handlers)
        {
            var tryBlocks = handlers.Where(h => h.TryStart == instruction.Offset).GroupBy(h => h.TryEnd);
            foreach (var tryBlock in tryBlocks)
            {
                ilGenerator.BeginExceptionBlock();
                m_exceptionBlockLevel++;
            }

            var filterBlock = handlers.FirstOrDefault(h => h.FilterStart == instruction.Offset);
            if (filterBlock != null)
            {
                ilGenerator.BeginExceptFilterBlock();
            }

            var handler = handlers.FirstOrDefault(h => h.HandlerEnd == instruction.Offset);
            if (handler != null)
            {
                if (handler.Flags == ExceptionHandlingClauseOptions.Finally)
                {
                    // Finally blocks are always the last handler
                    ilGenerator.EndExceptionBlock();
                    m_exceptionBlockLevel--;
                }
                else if (handler.HandlerEnd == handlers.Where(h => h.TryStart == handler.TryStart && h.TryEnd == handler.TryEnd).Max(h => h.HandlerEnd))
                {
                    // We're dealing with the last catch block
                    ilGenerator.EndExceptionBlock();
                    m_exceptionBlockLevel--;
                }
            }

            var catchOrFinallyBlock = handlers.FirstOrDefault(h => h.HandlerStart == instruction.Offset);
            if (catchOrFinallyBlock != null)
            {
                if (catchOrFinallyBlock.Flags == ExceptionHandlingClauseOptions.Clause)
                {
                    ilGenerator.BeginCatchBlock(catchOrFinallyBlock.CatchType);
                }
                else if (catchOrFinallyBlock.Flags == ExceptionHandlingClauseOptions.Filter)
                {
                    ilGenerator.BeginCatchBlock(null);
                }
                else if (catchOrFinallyBlock.Flags == ExceptionHandlingClauseOptions.Finally)
                {
                    ilGenerator.BeginFinallyBlock();
                }
                else
                {
                    // No support for fault blocks
                    throw new NotSupportedException();
                }
            }
        }

        private void EmitThisPointerAccessForBoxedValueType(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Call, typeof(Unsafe).GetMethod("Unbox").MakeGenericMethod(m_method.DeclaringType));
        }

        private void EmitILForInlineNone(ILGenerator ilGenerator, Instruction instruction)
        {
            ilGenerator.Emit(instruction.OpCode);
            if (m_isInterfaceDispatch && m_owningType.IsValueType && instruction.OpCode == OpCodes.Ldarg_0)
            {
                EmitThisPointerAccessForBoxedValueType(ilGenerator);
            }
        }

        private void EmitILForInlineI(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (int)instruction.Operand);

        private void EmitILForInlineI8(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (long)instruction.Operand);

        private void EmitILForShortInlineI(ILGenerator ilGenerator, Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Ldc_I4_S)
                ilGenerator.Emit(instruction.OpCode, (sbyte)instruction.Operand);
            else
                ilGenerator.Emit(instruction.OpCode, (byte)instruction.Operand);
        }

        private void EmitILForInlineR(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (double)instruction.Operand);

        private void EmitILForShortInlineR(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (float)instruction.Operand);

        private void EmitILForInlineString(ILGenerator ilGenerator, Instruction instruction)
            => ilGenerator.Emit(instruction.OpCode, (string)instruction.Operand);

        private void EmitILForInlineBrTarget(ILGenerator ilGenerator,
            Instruction instruction, Dictionary<int, Label> targetInstructions)
        {
            Label targetLabel = targetInstructions[(instruction.Operand as Instruction).Offset];

            OpCode opCode = instruction.OpCode;

            // Offset values could change and not be short form anymore
            if (opCode == OpCodes.Br_S) opCode = OpCodes.Br;
            else if (opCode == OpCodes.Brfalse_S) opCode = OpCodes.Brfalse;
            else if (opCode == OpCodes.Brtrue_S) opCode = OpCodes.Brtrue;
            else if (opCode == OpCodes.Beq_S) opCode = OpCodes.Beq;
            else if (opCode == OpCodes.Bge_S) opCode = OpCodes.Bge;
            else if (opCode == OpCodes.Bgt_S) opCode = OpCodes.Bgt;
            else if (opCode == OpCodes.Ble_S) opCode = OpCodes.Ble;
            else if (opCode == OpCodes.Blt_S) opCode = OpCodes.Blt;
            else if (opCode == OpCodes.Bne_Un_S) opCode = OpCodes.Bne_Un;
            else if (opCode == OpCodes.Bge_Un_S) opCode = OpCodes.Bge_Un;
            else if (opCode == OpCodes.Bgt_Un_S) opCode = OpCodes.Bgt_Un;
            else if (opCode == OpCodes.Ble_Un_S) opCode = OpCodes.Ble_Un;
            else if (opCode == OpCodes.Blt_Un_S) opCode = OpCodes.Blt_Un;
            else if (opCode == OpCodes.Leave_S) opCode = OpCodes.Leave;

            // Check if 'Leave' opcode is being used in an exception block,
            // only emit it if that's not the case
            if (opCode == OpCodes.Leave && m_exceptionBlockLevel > 0) return;

            ilGenerator.Emit(opCode, targetLabel);
        }

        private void EmitILForInlineSwitch(ILGenerator ilGenerator,
            Instruction instruction, Dictionary<int, Label> targetInstructions)
        {
            Instruction[] switchInstructions = (Instruction[])instruction.Operand;
            Label[] targetLabels = new Label[switchInstructions.Length];
            for (int i = 0; i < switchInstructions.Length; i++)
                targetLabels[i] = targetInstructions[switchInstructions[i].Offset];
            ilGenerator.Emit(instruction.OpCode, targetLabels);
        }

        private void EmitILForInlineVar(ILGenerator ilGenerator, Instruction instruction)
        {
            int index = 0;
            if (instruction.OpCode.Name.Contains("loc"))
            {
                index = ((LocalVariableInfo)instruction.Operand).LocalIndex;
            }
            else
            {
                index = ((ParameterInfo)instruction.Operand).Position;
                index += m_method.IsStatic ? 0 : 1;
            }

            if (instruction.OpCode.OperandType == OperandType.ShortInlineVar)
                ilGenerator.Emit(instruction.OpCode, (byte)index);
            else
                ilGenerator.Emit(instruction.OpCode, (ushort)index);

            if (m_isInterfaceDispatch && m_owningType.IsValueType && instruction.OpCode.Name.StartsWith("ldarg") && index == 0)
            {
                EmitThisPointerAccessForBoxedValueType(ilGenerator);
            }
        }

        private void EmitILForType(ILGenerator ilGenerator, Instruction instruction, TypeInfo typeInfo)
        {
            if (instruction.OpCode == OpCodes.Constrained)
            {
                m_constrainedType = typeInfo;
                return;
            }

            ilGenerator.Emit(instruction.OpCode, typeInfo);
        }

        private void EmitILForConstructor(ILGenerator ilGenerator, Instruction instruction, ConstructorInfo constructorInfo)
        {
            if (constructorInfo.InCoreLibrary())
            {
                // Don't attempt to rewrite unaccessible constructors in System.Private.CoreLib/mscorlib
                if (!constructorInfo.DeclaringType.IsPublic) goto forward;
                if (!constructorInfo.IsPublic && !constructorInfo.IsFamily && !constructorInfo.IsFamilyOrAssembly) goto forward;
            }

            if (instruction.OpCode == OpCodes.Call)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectCall(constructorInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Newobj)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForObjectInitialization(constructorInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Ldftn)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectLoad(constructorInfo));
                return;
            }

            // If we get here, then we haven't accounted for an opcode.
            // Throw exception to make this obvious.
            throw new NotSupportedException(instruction.OpCode.Name);

        forward:
            ilGenerator.Emit(instruction.OpCode, constructorInfo);
        }

        private void EmitILForMethod(ILGenerator ilGenerator, Instruction instruction, MethodInfo methodInfo)
        {
            if (methodInfo.InCoreLibrary())
            {
                // Don't attempt to rewrite unaccessible methods in System.Private.CoreLib/mscorlib
                if (!methodInfo.DeclaringType.IsPublic) goto forward;
                if (!methodInfo.IsPublic && !methodInfo.IsFamily && !methodInfo.IsFamilyOrAssembly) goto forward;
            }

            if (instruction.OpCode == OpCodes.Call)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectCall(methodInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Callvirt)
            {
                if (m_constrainedType != null)
                {
                    ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualCall(methodInfo, m_constrainedType));
                    m_constrainedType = null;
                    return;
                }

                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualCall(methodInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Ldftn)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForDirectLoad(methodInfo));
                return;
            }

            if (instruction.OpCode == OpCodes.Ldvirtftn)
            {
                ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForVirtualLoad(methodInfo));
                return;
            }

        forward:
            ilGenerator.Emit(instruction.OpCode, methodInfo);
        }

        private void EmitILForInlineMember(ILGenerator ilGenerator, Instruction instruction)
        {
            MemberInfo memberInfo = (MemberInfo)instruction.Operand;
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                ilGenerator.Emit(instruction.OpCode, memberInfo as FieldInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.TypeInfo
                || memberInfo.MemberType == MemberTypes.NestedType)
            {
                EmitILForType(ilGenerator, instruction, memberInfo as TypeInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Constructor)
            {
                EmitILForConstructor(ilGenerator, instruction, memberInfo as ConstructorInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Method)
            {
                EmitILForMethod(ilGenerator, instruction, memberInfo as MethodInfo);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}