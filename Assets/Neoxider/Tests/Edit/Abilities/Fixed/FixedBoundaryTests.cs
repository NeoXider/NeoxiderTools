using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Neo.Abilities.Fixed;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     The fixed core is engine-free and float-free: its assembly references no Unity assembly, its asmdef sets
    ///     noEngineReferences, and no field, signature or IL instruction of any of its types uses float, double or
    ///     decimal.
    /// </summary>
    public sealed class FixedBoundaryTests
    {
        private static readonly Assembly Core = typeof(FixedAbilitySystem).Assembly;

        [Test]
        public void Assembly_ReferencesNoUnityAssembly()
        {
            foreach (AssemblyName reference in Core.GetReferencedAssemblies())
            {
                Assert.That(reference.Name.StartsWith("Unity", StringComparison.Ordinal), Is.False, reference.Name);
            }
        }

        [Test]
        public void Asmdef_IsEngineFreeWithoutReferences()
        {
            string path = Path.Combine(Application.dataPath, "Neoxider/Scripts/Abilities/Fixed/Neo.Abilities.Fixed.asmdef");
            Assert.That(File.Exists(path), Is.True, path);
            string text = File.ReadAllText(path);
            StringAssert.Contains("\"noEngineReferences\": true", text);
            StringAssert.Contains("\"references\": []", text);
        }

        [Test]
        public void Types_DeclareNoFloatingPoint()
        {
            List<string> offenders = new List<string>();
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type type in Core.GetTypes())
            {
                foreach (FieldInfo field in type.GetFields(all))
                {
                    Check(field.FieldType, type.FullName + "." + field.Name, offenders);
                }

                foreach (MethodBase method in Methods(type, all))
                {
                    if (method is MethodInfo info)
                    {
                        Check(info.ReturnType, type.FullName + "." + method.Name, offenders);
                    }

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        Check(parameter.ParameterType, type.FullName + "." + method.Name + "(" + parameter.Name + ")", offenders);
                    }

                    MethodBody body = method.GetMethodBody();
                    if (body == null)
                    {
                        continue;
                    }

                    foreach (LocalVariableInfo local in body.LocalVariables)
                    {
                        Check(local.LocalType, type.FullName + "." + method.Name + " local", offenders);
                    }

                    string opcode = FloatingOpcode(body.GetILAsByteArray());
                    if (opcode != null)
                    {
                        offenders.Add(type.FullName + "." + method.Name + " uses " + opcode);
                    }
                }
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        private static IEnumerable<MethodBase> Methods(Type type, BindingFlags flags)
        {
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                yield return method;
            }

            foreach (ConstructorInfo constructor in type.GetConstructors(flags))
            {
                yield return constructor;
            }
        }

        private static void Check(Type type, string where, List<string> offenders)
        {
            if (type == null)
            {
                return;
            }

            if (type.HasElementType)
            {
                Check(type.GetElementType(), where, offenders);
                return;
            }

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                offenders.Add(where + " is " + type.Name);
                return;
            }

            if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                {
                    Check(argument, where, offenders);
                }
            }
        }

        // WHY: walks the IL with the real operand sizes so operand bytes are never mistaken for opcodes.
        private static string FloatingOpcode(byte[] il)
        {
            Dictionary<short, OpCode> table = OpCodeTable();
            int position = 0;
            while (position < il.Length)
            {
                short value = il[position] == 0xFE && position + 1 < il.Length
                    ? (short)(0xFE00 | il[position + 1])
                    : il[position];
                if (!table.TryGetValue(value, out OpCode opcode))
                {
                    return "an unknown opcode 0x" + value.ToString("X");
                }

                position += opcode.Size;
                if (opcode == OpCodes.Ldc_R4 || opcode == OpCodes.Ldc_R8 || opcode == OpCodes.Conv_R4 ||
                    opcode == OpCodes.Conv_R8 || opcode == OpCodes.Conv_R_Un || opcode == OpCodes.Ldind_R4 ||
                    opcode == OpCodes.Ldind_R8 || opcode == OpCodes.Stind_R4 || opcode == OpCodes.Stind_R8 ||
                    opcode == OpCodes.Ldelem_R4 || opcode == OpCodes.Ldelem_R8 || opcode == OpCodes.Stelem_R4 ||
                    opcode == OpCodes.Stelem_R8 || opcode == OpCodes.Ckfinite)
                {
                    return opcode.Name;
                }

                position += OperandSize(opcode, il, position);
            }

            return null;
        }

        private static int OperandSize(OpCode opcode, byte[] il, int position)
        {
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineSwitch:
                    return 4 + 4 * BitConverter.ToInt32(il, position);
                default:
                    return 4;
            }
        }

        private static Dictionary<short, OpCode> _table;

        private static Dictionary<short, OpCode> OpCodeTable()
        {
            if (_table != null)
            {
                return _table;
            }

            _table = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is OpCode opcode)
                {
                    _table[opcode.Value] = opcode;
                }
            }

            return _table;
        }
    }
}
