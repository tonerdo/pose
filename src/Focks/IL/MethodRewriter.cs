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

        public MethodInfo Rewrite()
        {
            MethodBody methodBody = _method.GetMethodBody();
            if (methodBody == null)
                return _method as MethodInfo;

            List<Type> parameterTypes = new List<Type>();
            if (!_method.IsStatic)
            {
                if (_method.DeclaringType.IsSubclassOf(typeof(ValueType)))
                    parameterTypes.Add(_method.DeclaringType.MakeByRefType());
                else
                    parameterTypes.Add(_method.DeclaringType);
            }

            parameterTypes.AddRange(_method.GetParameters().Select(p => p.ParameterType));
            DynamicMethod dynamicMethod = new DynamicMethod(
                _method.Name,
                (_method as MethodInfo).ReturnType,
                parameterTypes.ToArray());

            MethodDisassembler disassembler = new MethodDisassembler(_method);
            IList<LocalVariableInfo> locals = methodBody.LocalVariables;
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
                            ilGenerator.Emit(instruction.OpCode, constructorInfo);
                        }
                        else if (memberInfo.MemberType == MemberTypes.Method)
                        {
                            MethodInfo methodInfo = memberInfo as MethodInfo;
                            if (instruction.OpCode == OpCodes.Call)
                            {
                                DynamicMethod stub = Stubs.GenerateStubForMethod(methodInfo);
                                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
                                ilGenerator.Emit(instruction.OpCode, stub);
                            }
                            else if (instruction.OpCode == OpCodes.Ldftn)
                            {
                                DynamicMethod stub = Stubs.GenerateStubForMethodPointer(methodInfo);
                                ilGenerator.Emit(OpCodes.Ldtoken, methodInfo);
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