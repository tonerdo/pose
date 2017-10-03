using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using Focks.Extensions;
using Focks.Helpers;
using Mono.Reflection;

namespace Focks.IL
{
    internal class MethodRewriter
    {
        private MethodBase _method;

        private MethodRewriter() { }

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
                parameterTypes.ToArray());

            MethodDisassembler disassembler = new MethodDisassembler(_method);
            IList<LocalVariableInfo> locals = _method.GetMethodBody().LocalVariables;
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            var instructions = disassembler.GetILInstructions();
            Dictionary<int, Label> targetInstructions = new Dictionary<int, Label>();

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

            foreach (var instruction in instructions)
            {
                if (targetInstructions.TryGetValue(instruction.Offset, out Label label))
                    ilGenerator.MarkLabel(label);

                switch (instruction.OpCode.OperandType)
                {
                    case OperandType.InlineNone:
                        ilGenerator.Emit(instruction.OpCode);
                        break;
                    case OperandType.InlineI:
                        ilGenerator.Emit(instruction.OpCode, (int)instruction.Operand);
                        break;
                    case OperandType.InlineI8:
                        ilGenerator.Emit(instruction.OpCode, (long)instruction.Operand);
                        break;
                    case OperandType.ShortInlineI:
                        if (instruction.OpCode == OpCodes.Ldc_I4_S)
                            ilGenerator.Emit(instruction.OpCode, (sbyte)instruction.Operand);
                        else
                            ilGenerator.Emit(instruction.OpCode, (byte)instruction.Operand);
                        break;
                    case OperandType.InlineR:
                        ilGenerator.Emit(instruction.OpCode, (double)instruction.Operand);
                        break;
                    case OperandType.ShortInlineR:
                        ilGenerator.Emit(instruction.OpCode, (float)instruction.Operand);
                        break;
                    case OperandType.InlineString:
                        ilGenerator.Emit(instruction.OpCode, (string)instruction.Operand);
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        Label targetLabel = targetInstructions[(instruction.Operand as Instruction).Offset];
                        // Offset values could change and not be short form anymore
                        if (instruction.OpCode == OpCodes.Br_S)
                            ilGenerator.Emit(OpCodes.Br, targetLabel);
                        else if (instruction.OpCode == OpCodes.Brfalse_S)
                            ilGenerator.Emit(OpCodes.Brfalse, targetLabel);
                        else if (instruction.OpCode == OpCodes.Brtrue_S)
                            ilGenerator.Emit(OpCodes.Brtrue, targetLabel);
                        else if (instruction.OpCode == OpCodes.Beq_S)
                            ilGenerator.Emit(OpCodes.Beq, targetLabel);
                        else if (instruction.OpCode == OpCodes.Bge_S)
                            ilGenerator.Emit(OpCodes.Bge, targetLabel);
                        else if (instruction.OpCode == OpCodes.Bgt_S)
                            ilGenerator.Emit(OpCodes.Bgt, targetLabel);
                        else if (instruction.OpCode == OpCodes.Ble_S)
                            ilGenerator.Emit(OpCodes.Ble, targetLabel);
                        else if (instruction.OpCode == OpCodes.Blt_S)
                            ilGenerator.Emit(OpCodes.Blt, targetLabel);
                        else if (instruction.OpCode == OpCodes.Bne_Un_S)
                            ilGenerator.Emit(OpCodes.Bne_Un, targetLabel);
                        else if (instruction.OpCode == OpCodes.Bge_Un_S)
                            ilGenerator.Emit(OpCodes.Bge_Un, targetLabel);
                        else if (instruction.OpCode == OpCodes.Bgt_Un_S)
                            ilGenerator.Emit(OpCodes.Bgt_Un, targetLabel);
                        else if (instruction.OpCode == OpCodes.Ble_Un_S)
                            ilGenerator.Emit(OpCodes.Ble_Un, targetLabel);
                        else if (instruction.OpCode == OpCodes.Blt_Un_S)
                            ilGenerator.Emit(OpCodes.Blt_Un, targetLabel);
                        else if (instruction.OpCode == OpCodes.Leave_S)
                            ilGenerator.Emit(OpCodes.Leave, targetLabel);
                        else
                            ilGenerator.Emit(instruction.OpCode, targetLabel);
                        break;
                    case OperandType.InlineSwitch:
                        Instruction[] switchInstructions = (Instruction[])instruction.Operand;
                        Label[] targetLabels = new Label[switchInstructions.Length];
                        for (int i = 0; i < switchInstructions.Length; i++)
                            targetLabels[i] = targetInstructions[switchInstructions[i].Offset];
                        ilGenerator.Emit(instruction.OpCode, targetLabels);
                        break;
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineVar:
                        int index = 0;
                        if (instruction.OpCode.Name.Contains("loc"))
                            index = ((LocalVariableInfo)instruction.Operand).LocalIndex;
                        else
                        {
                            index = ((ParameterInfo)instruction.Operand).Position;
                            index = _method.IsStatic ? index : index + 1;
                        }

                        if (instruction.OpCode.OperandType == OperandType.ShortInlineVar)
                            ilGenerator.Emit(instruction.OpCode, (byte)index);
                        else
                            ilGenerator.Emit(instruction.OpCode, (short)index);
                        break;
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                        MemberInfo memberInfo = (MemberInfo)instruction.Operand;
                        if (memberInfo.MemberType == MemberTypes.Field)
                        {
                            FieldInfo fieldInfo = (MemberInfo)instruction.Operand as FieldInfo;
                            ilGenerator.Emit(instruction.OpCode, fieldInfo);
                        }
                        else if (memberInfo.MemberType == MemberTypes.TypeInfo
                            || memberInfo.MemberType == MemberTypes.NestedType)
                        {
                            TypeInfo typeInfo = (MemberInfo)instruction.Operand as TypeInfo;
                            ilGenerator.Emit(instruction.OpCode, typeInfo);
                        }
                        else if (memberInfo.MemberType == MemberTypes.Constructor)
                        {
                            ConstructorInfo constructorInfo = memberInfo as ConstructorInfo;
                            MethodBody methodBody = constructorInfo.GetMethodBody();
                            if (methodBody == null)
                            {
                                ilGenerator.Emit(instruction.OpCode, constructorInfo);
                                continue;
                            }

                            if (instruction.OpCode != OpCodes.Newobj && instruction.OpCode != OpCodes.Call)
                            {
                                ilGenerator.Emit(instruction.OpCode, constructorInfo);
                                continue;
                            }

                            ilGenerator.Emit(OpCodes.Ldtoken, constructorInfo);
                            ilGenerator.Emit(OpCodes.Ldtoken, constructorInfo.DeclaringType);
                            ilGenerator.Emit(OpCodes.Call, Stubs.GenerateStubForConstructor(constructorInfo, instruction.OpCode, constructorInfo.IsForValueType()));
                        }
                        else if (memberInfo.MemberType == MemberTypes.Method)
                        {
                            MethodInfo methodInfo = memberInfo as MethodInfo;
                            MethodBody methodBody = methodInfo.GetMethodBody();
                            if (methodBody == null)
                            {
                                ilGenerator.Emit(instruction.OpCode, methodInfo);
                                continue;
                            }

                            if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                            {
                                int shimIndex = Array.FindIndex(IsolationContext.Shims, s => s.Original == methodInfo);
                                DynamicMethod stub = default(DynamicMethod);

                                if (shimIndex != -1)
                                {
                                    stub = Stubs.GenerateStubForShim(methodInfo, shimIndex);
                                }
                                else
                                {
                                    stub = instruction.OpCode == OpCodes.Call ?
                                        Stubs.GenerateStubForMethod(methodInfo) : Stubs.GenerateStubForVirtualMethod(methodInfo);
                                    ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
                                    ilGenerator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
                                }

                                ilGenerator.Emit(OpCodes.Call, stub);
                            }
                            else if (instruction.OpCode == OpCodes.Ldftn)
                            {
                                DynamicMethod stub = Stubs.GenerateStubForMethodPointer(methodInfo);
                                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
                                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
                                ilGenerator.Emit(OpCodes.Call, stub);
                            }
                            else
                            {
                                ilGenerator.Emit(instruction.OpCode, methodInfo);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            return dynamicMethod;
        }
    }
}