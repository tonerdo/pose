using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using Pose.Extensions;
using Pose.Helpers;

using Mono.Reflection;

namespace Pose.IL
{
    internal class MethodRewriter
    {
        private MethodBase _method;

        private int exceptionBlockLevel;

        private static List<OpCode> s_IngoredPrefixOpCodes = new List<OpCode> { OpCodes.Tailcall };

        private static List<OpCode> s_IngoredOpCodes = new List<OpCode> { OpCodes.Endfilter, OpCodes.Endfinally };

        public static MethodRewriter CreateRewriter(MethodBase method)
        {
            return new MethodRewriter { _method = method };
        }

        public MethodBase Rewrite()
        {
            List<Type> parameterTypes = new List<Type>();
            if (!_method.IsStatic)
            {
                if (_method.IsForValueType())
                    parameterTypes.Add(_method.DeclaringType.MakeByRefType());
                else
                    parameterTypes.Add(_method.DeclaringType);
            }

            parameterTypes.AddRange(_method.GetParameters().Select(p => p.ParameterType));
            Type returnType = _method.IsConstructor ? typeof(void) : (_method as MethodInfo).ReturnType;

            DynamicMethod dynamicMethod = new DynamicMethod(
                string.Format("dynamic_{0}_{1}", _method.DeclaringType, _method.Name),
                returnType,
                parameterTypes.ToArray(),
                StubHelper.GetOwningModule(),
                true);

            var methodBody = _method.GetMethodBody();
            var locals = methodBody.LocalVariables;
            var targetInstructions = new Dictionary<int, Label>();
            var handlers = new List<ExceptionHandler>();

            var ilGenerator = dynamicMethod.GetILGenerator();
            var instructions = _method.GetInstructions();

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
            Debug.WriteLine("\n" + _method.Name);
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
                        throw new NotSupportedException();
                }
            }

#if DEBUG
            var bakeByteArray = typeof(ILGenerator).GetMethod("BakeByteArray", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(bakeByteArray != null);

            byte[] ilBytes = (byte[])bakeByteArray.Invoke(ilGenerator, null);
            Debug.Assert(ilBytes != null && ilBytes.Length > 0);

            var debuggableDynamicMethod = new DebuggableDynamicMethod(dynamicMethod, ilGenerator, new DynamicMethodBody(ilBytes, locals));

            Debug.WriteLine("\n" + debuggableDynamicMethod.Name);

            foreach (var instruction in debuggableDynamicMethod.GetInstructions())
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
                exceptionBlockLevel++;
            }

            var filterBlock = handlers.FirstOrDefault(h => h.FilterStart == instruction.Offset);
            if (filterBlock != null)
            {
                ilGenerator.BeginExceptFilterBlock();
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

            var handler = handlers.FirstOrDefault(h => h.HandlerEnd == instruction.Offset);
            if (handler != null)
            {
                if (handler.Flags == ExceptionHandlingClauseOptions.Finally)
                {
                    // Finally blocks are always the last handler
                    ilGenerator.EndExceptionBlock();
                    exceptionBlockLevel--;
                }
                else if (handler.HandlerEnd == handlers.Where(h => h.TryStart == handler.TryStart && h.TryEnd == handler.TryEnd).Max(h => h.HandlerEnd))
                {
                    // We're dealing with the last catch block
                    ilGenerator.EndExceptionBlock();
                    exceptionBlockLevel--;
                }
            }
        }

        private void EmitILForInlineNone(ILGenerator ilGenerator, Instruction instruction)
        {
            if (s_IngoredPrefixOpCodes.Contains(instruction.OpCode))
                return;

            ilGenerator.Emit(instruction.OpCode);
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
            if (opCode == OpCodes.Leave && exceptionBlockLevel > 0) return;

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
                index = ((LocalVariableInfo)instruction.Operand).LocalIndex;
            else
            {
                index = ((ParameterInfo)instruction.Operand).Position;
                index += _method.IsStatic ? 0 : 1;
            }

            if (instruction.OpCode.OperandType == OperandType.ShortInlineVar)
                ilGenerator.Emit(instruction.OpCode, (byte)index);
            else
                ilGenerator.Emit(instruction.OpCode, (short)index);
        }

        private void EmitILForConstructor(ILGenerator ilGenerator, Instruction instruction, MemberInfo memberInfo)
        {
            ConstructorInfo constructorInfo = memberInfo as ConstructorInfo;
            if (PoseContext.StubCache.TryGetValue(constructorInfo, out DynamicMethod stub))
            {
                ilGenerator.Emit(OpCodes.Ldtoken, constructorInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, constructorInfo.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, stub);
                return;
            }

            MethodBody methodBody = constructorInfo.GetMethodBody();
            if (methodBody == null)
            {
                ilGenerator.Emit(instruction.OpCode, constructorInfo);
                return;
            }

            if (instruction.OpCode != OpCodes.Newobj && instruction.OpCode != OpCodes.Call)
            {
                ilGenerator.Emit(instruction.OpCode, constructorInfo);
                return;
            }

            stub = Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType());
            ilGenerator.Emit(OpCodes.Ldtoken, constructorInfo);
            ilGenerator.Emit(OpCodes.Ldtoken, constructorInfo.DeclaringType);
            ilGenerator.Emit(OpCodes.Call, stub);
            PoseContext.StubCache.TryAdd(constructorInfo, stub);
        }

        private void EmitILForMethod(ILGenerator ilGenerator, Instruction instruction, MemberInfo memberInfo)
        {
            MethodInfo methodInfo = memberInfo as MethodInfo;
            if (PoseContext.StubCache.TryGetValue(methodInfo, out DynamicMethod stub))
            {
                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, stub);
                return;
            }

            MethodBody methodBody = methodInfo.GetMethodBody();
            if (methodBody == null && !methodInfo.IsAbstract)
            {
                ilGenerator.Emit(instruction.OpCode, methodInfo);
                return;
            }

            if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
            {
                stub = instruction.OpCode == OpCodes.Call ?
                    Stubs.GenerateStubForMethod(methodInfo) : Stubs.GenerateStubForVirtualMethod(methodInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, stub);
                PoseContext.StubCache.TryAdd(methodInfo, stub);
            }
            else if (instruction.OpCode == OpCodes.Ldftn)
            {
                stub = Stubs.GenerateStubForMethodPointer(methodInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
                ilGenerator.Emit(OpCodes.Call, stub);
                PoseContext.StubCache.TryAdd(methodInfo, stub);
            }
            else
            {
                ilGenerator.Emit(instruction.OpCode, methodInfo);
            }
        }

        private void EmitILForInlineMember(ILGenerator ilGenerator, Instruction instruction)
        {
            MemberInfo memberInfo = (MemberInfo)instruction.Operand;
            if (memberInfo.MemberType == MemberTypes.Field)
            {
                ilGenerator.Emit(instruction.OpCode, (MemberInfo)instruction.Operand as FieldInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.TypeInfo
                || memberInfo.MemberType == MemberTypes.NestedType)
            {
                ilGenerator.Emit(instruction.OpCode, (MemberInfo)instruction.Operand as TypeInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Constructor)
            {
                ilGenerator.Emit(instruction.OpCode, (MemberInfo)instruction.Operand as ConstructorInfo);
            }
            else if (memberInfo.MemberType == MemberTypes.Method)
            {
                ilGenerator.Emit(instruction.OpCode, (MemberInfo)instruction.Operand as MethodInfo);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}