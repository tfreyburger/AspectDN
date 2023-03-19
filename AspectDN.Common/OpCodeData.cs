// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace AspectDN.Common
{
    internal static class OpCodeDatas
    {
        static List<OpCodeData> _OpILs;
        internal static OpCodeData Get(int opValue)
        {
            if (_OpILs == null)
                _SetList();
            return _OpILs.Where(t => t.OpValue == opValue).FirstOrDefault();
        }

        internal static OpCodeData Get(OpCodeValues opCode)
        {
            if (_OpILs == null)
                _SetList();
            return _OpILs.Where(t => t.OpCodeValue == opCode).FirstOrDefault();
        }

        internal static OpCodeData Get(OpCode opCode)
        {
            if (_OpILs == null)
                _SetList();
            return Get(CecilHelper.GetOpCodeValue(opCode));
        }

        internal static OpCodeData Get(string opCodeName)
        {
            if (_OpILs == null)
                _SetList();
            return Get(CecilHelper.GetOpCodeValue(opCodeName));
        }

        static void _Add(OpCodeData opCodeData)
        {
            _OpILs.Add(opCodeData);
        }

        static void _SetList()
        {
            _OpILs = new List<OpCodeData>();
            _Add(new OpCodeData(0x00, OpCodeValues.Nop, OpCodes.Nop, ILOperandTypes.None, 0, 0));
            _Add(new OpCodeData(0x01, OpCodeValues.Break, OpCodes.Break, ILOperandTypes.None, 0, 0));
            _Add(new OpCodeData(0x02, OpCodeValues.Ldarg_0, OpCodes.Ldarg_0, ILOperandTypes.None, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld, 0));
            _Add(new OpCodeData(0x03, OpCodeValues.Ldarg_1, OpCodes.Ldarg_1, ILOperandTypes.None, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld, 1));
            _Add(new OpCodeData(0x04, OpCodeValues.Ldarg_2, OpCodes.Ldarg_2, ILOperandTypes.None, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld, 2));
            _Add(new OpCodeData(0x05, OpCodeValues.Ldarg_3, OpCodes.Ldarg_3, ILOperandTypes.None, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld, 3));
            _Add(new OpCodeData(0x06, OpCodeValues.Ldloc_0, OpCodes.Ldloc_0, ILOperandTypes.None, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld, 0));
            _Add(new OpCodeData(0x07, OpCodeValues.Ldloc_1, OpCodes.Ldloc_1, ILOperandTypes.None, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld, 1));
            _Add(new OpCodeData(0x08, OpCodeValues.Ldloc_2, OpCodes.Ldloc_2, ILOperandTypes.None, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld, 2));
            _Add(new OpCodeData(0x09, OpCodeValues.Ldloc_3, OpCodes.Ldloc_3, ILOperandTypes.None, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld, 3));
            _Add(new OpCodeData(0x0A, OpCodeValues.Stloc_0, OpCodes.Stloc_0, ILOperandTypes.None, 1, 0, OpCodeTypes.LocVar | OpCodeTypes.St, 0));
            _Add(new OpCodeData(0x0B, OpCodeValues.Stloc_1, OpCodes.Stloc_1, ILOperandTypes.None, 1, 0, OpCodeTypes.LocVar | OpCodeTypes.St, 1));
            _Add(new OpCodeData(0x0C, OpCodeValues.Stloc_2, OpCodes.Stloc_2, ILOperandTypes.None, 1, 0, OpCodeTypes.LocVar | OpCodeTypes.St, 2));
            _Add(new OpCodeData(0x0D, OpCodeValues.Stloc_3, OpCodes.Stloc_3, ILOperandTypes.None, 1, 0, OpCodeTypes.LocVar | OpCodeTypes.St, 3));
            _Add(new OpCodeData(0x0E, OpCodeValues.Ldarg_S, OpCodes.Ldarg_S, ILOperandTypes.uint8, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x0F, OpCodeValues.Ldarga_S, OpCodes.Ldarga_S, ILOperandTypes.uint8, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld | OpCodeTypes.Address));
            _Add(new OpCodeData(0x10, OpCodeValues.Starg_S, OpCodes.Starg_S, ILOperandTypes.uint8, 1, 0, OpCodeTypes.Arg | OpCodeTypes.St));
            _Add(new OpCodeData(0x11, OpCodeValues.Ldloc_S, OpCodes.Ldloc_S, ILOperandTypes.uint8, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x12, OpCodeValues.Ldloca_S, OpCodes.Ldloca_S, ILOperandTypes.uint8, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld | OpCodeTypes.Address));
            _Add(new OpCodeData(0x13, OpCodeValues.Stloc_S, OpCodes.Stloc_S, ILOperandTypes.uint8, 1, 0, OpCodeTypes.LocVar | OpCodeTypes.St));
            _Add(new OpCodeData(0x14, OpCodeValues.Ldnull, OpCodes.Ldnull, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x15, OpCodeValues.Ldc_I4_M1, OpCodes.Ldc_I4_M1, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x16, OpCodeValues.Ldc_I4_0, OpCodes.Ldc_I4_0, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x17, OpCodeValues.Ldc_I4_1, OpCodes.Ldc_I4_1, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x18, OpCodeValues.Ldc_I4_2, OpCodes.Ldc_I4_2, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x19, OpCodeValues.Ldc_I4_3, OpCodes.Ldc_I4_3, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x1A, OpCodeValues.Ldc_I4_4, OpCodes.Ldc_I4_4, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x1B, OpCodeValues.Ldc_I4_5, OpCodes.Ldc_I4_5, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x1C, OpCodeValues.Ldc_I4_6, OpCodes.Ldc_I4_6, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x1D, OpCodeValues.Ldc_I4_7, OpCodes.Ldc_I4_7, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x1E, OpCodeValues.Ldc_I4_8, OpCodes.Ldc_I4_8, ILOperandTypes.None, 0, 1));
            _Add(new OpCodeData(0x1F, OpCodeValues.Ldc_I4_S, OpCodes.Ldc_I4_S, ILOperandTypes.int8, 0, 1));
            _Add(new OpCodeData(0x20, OpCodeValues.Ldc_I4, OpCodes.Ldc_I4, ILOperandTypes.int32, 0, 1));
            _Add(new OpCodeData(0x21, OpCodeValues.Ldc_I8, OpCodes.Ldc_I8, ILOperandTypes.int64, 0, 1));
            _Add(new OpCodeData(0x22, OpCodeValues.Ldc_R4, OpCodes.Ldc_R4, ILOperandTypes.float32, 0, 1));
            _Add(new OpCodeData(0x23, OpCodeValues.Ldc_R8, OpCodes.Ldc_R8, ILOperandTypes.float64, 0, 1));
            _Add(new OpCodeData(0x25, OpCodeValues.Dup, OpCodes.Dup, ILOperandTypes.None, 1, 2));
            _Add(new OpCodeData(0x26, OpCodeValues.Pop, OpCodes.Pop, ILOperandTypes.None, 1, 0));
            _Add(new OpCodeData(0x27, OpCodeValues.Jmp, OpCodes.Jmp, ILOperandTypes.Method, 0, 0));
            _Add(new OpCodeData(0x28, OpCodeValues.Call, OpCodes.Call, ILOperandTypes.Method, -1, -1, OpCodeTypes.Call));
            _Add(new OpCodeData(0x29, OpCodeValues.Calli, OpCodes.Calli, ILOperandTypes.Signature, -1, -1, OpCodeTypes.Call));
            _Add(new OpCodeData(0x2A, OpCodeValues.Ret, OpCodes.Ret, ILOperandTypes.None, -1, 0, OpCodeTypes.Return));
            _Add(new OpCodeData(0x2B, OpCodeValues.Br_S, OpCodes.Br_S, ILOperandTypes.int8, 0, 0, OpCodeTypes.Branch | OpCodeTypes.UnCond));
            _Add(new OpCodeData(0x2C, OpCodeValues.Brfalse_S, OpCodes.Brfalse_S, ILOperandTypes.int8, 1, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x2D, OpCodeValues.Brtrue_S, OpCodes.Brtrue_S, ILOperandTypes.int8, 1, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x2E, OpCodeValues.Beq_S, OpCodes.Beq_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x2F, OpCodeValues.Bge_S, OpCodes.Bge_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x30, OpCodeValues.Bgt_S, OpCodes.Bgt_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x31, OpCodeValues.Ble_S, OpCodes.Ble_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x32, OpCodeValues.Blt_S, OpCodes.Blt_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x33, OpCodeValues.Bne_Un_S, OpCodes.Bne_Un_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x34, OpCodeValues.Bge_Un_S, OpCodes.Bge_Un_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x35, OpCodeValues.Bgt_Un_S, OpCodes.Bgt_Un_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x36, OpCodeValues.Ble_Un_S, OpCodes.Ble_Un_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x37, OpCodeValues.Blt_Un_S, OpCodes.Blt_Un_S, ILOperandTypes.int8, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x38, OpCodeValues.Br, OpCodes.Br, ILOperandTypes.int32, 0, 0, OpCodeTypes.Branch | OpCodeTypes.UnCond));
            _Add(new OpCodeData(0x39, OpCodeValues.Brfalse, OpCodes.Brfalse, ILOperandTypes.int32, 1, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x3A, OpCodeValues.Brtrue, OpCodes.Brtrue, ILOperandTypes.int32, 1, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x3B, OpCodeValues.Beq, OpCodes.Beq, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x3C, OpCodeValues.Bge, OpCodes.Bge, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x3D, OpCodeValues.Bgt, OpCodes.Bgt, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x3E, OpCodeValues.Ble, OpCodes.Ble, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x3F, OpCodeValues.Blt, OpCodes.Blt, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x40, OpCodeValues.Bne_Un, OpCodes.Bne_Un, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x41, OpCodeValues.Bge_Un, OpCodes.Bge_Un, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x42, OpCodeValues.Bgt_Un, OpCodes.Bgt_Un, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x43, OpCodeValues.Ble_Un, OpCodes.Ble_Un, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x44, OpCodeValues.Blt_Un, OpCodes.Blt_Un, ILOperandTypes.int32, 2, 0, OpCodeTypes.Branch | OpCodeTypes.Cond));
            _Add(new OpCodeData(0x45, OpCodeValues.Switch, OpCodes.Switch, ILOperandTypes.Switch, 1, 0));
            _Add(new OpCodeData(0x46, OpCodeValues.Ldind_I1, OpCodes.Ldind_I1, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x47, OpCodeValues.Ldind_U1, OpCodes.Ldind_U1, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x48, OpCodeValues.Ldind_I2, OpCodes.Ldind_I2, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x49, OpCodeValues.Ldind_U2, OpCodes.Ldind_U2, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x4A, OpCodeValues.Ldind_I4, OpCodes.Ldind_I4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x4B, OpCodeValues.Ldind_U4, OpCodes.Ldind_U4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x4C, OpCodeValues.Ldind_I8, OpCodes.Ldind_I8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x4D, OpCodeValues.Ldind_I, OpCodes.Ldind_I, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x4E, OpCodeValues.Ldind_R4, OpCodes.Ldind_R4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x4F, OpCodeValues.Ldind_R8, OpCodes.Ldind_R8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x50, OpCodeValues.Ldind_Ref, OpCodes.Ldind_Ref, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x51, OpCodeValues.Stind_Ref, OpCodes.Stind_Ref, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x52, OpCodeValues.Stind_I1, OpCodes.Stind_I1, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x53, OpCodeValues.Stind_I2, OpCodes.Stind_I2, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x54, OpCodeValues.Stind_I4, OpCodes.Stind_I4, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x55, OpCodeValues.Stind_I8, OpCodes.Stind_I8, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x56, OpCodeValues.Stind_R4, OpCodes.Stind_R4, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x57, OpCodeValues.Stind_R8, OpCodes.Stind_R8, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0x58, OpCodeValues.Add, OpCodes.Add, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x59, OpCodeValues.Sub, OpCodes.Sub, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x5A, OpCodeValues.Mul, OpCodes.Mul, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x5B, OpCodeValues.Div, OpCodes.Div, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x5C, OpCodeValues.Div_Un, OpCodes.Div_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x5D, OpCodeValues.Rem, OpCodes.Rem, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x5E, OpCodeValues.Rem_Un, OpCodes.Rem_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x5F, OpCodeValues.And, OpCodes.And, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x60, OpCodeValues.Or, OpCodes.Or, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x61, OpCodeValues.Xor, OpCodes.Xor, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x62, OpCodeValues.Shl, OpCodes.Shl, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x63, OpCodeValues.Shr, OpCodes.Shr, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x64, OpCodeValues.Shr_Un, OpCodes.Shr_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0x65, OpCodeValues.Neg, OpCodes.Neg, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x66, OpCodeValues.Not, OpCodes.Not, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x67, OpCodeValues.Conv_I1, OpCodes.Conv_I1, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x68, OpCodeValues.Conv_I2, OpCodes.Conv_I2, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x69, OpCodeValues.Conv_I4, OpCodes.Conv_I4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x6A, OpCodeValues.Conv_I8, OpCodes.Conv_I8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x6B, OpCodeValues.Conv_R4, OpCodes.Conv_R4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x6C, OpCodeValues.Conv_R8, OpCodes.Conv_R8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x6D, OpCodeValues.Conv_U4, OpCodes.Conv_U4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x6E, OpCodeValues.Conv_U8, OpCodes.Conv_U8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x6F, OpCodeValues.Callvirt, OpCodes.Callvirt, ILOperandTypes.Method, -1, -1, OpCodeTypes.Call));
            _Add(new OpCodeData(0x70, OpCodeValues.Cpobj, OpCodes.Cpobj, ILOperandTypes.Type, 2, 0));
            _Add(new OpCodeData(0x71, OpCodeValues.Ldobj, OpCodes.Ldobj, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0x72, OpCodeValues.Ldstr, OpCodes.Ldstr, ILOperandTypes.String, 0, 1));
            _Add(new OpCodeData(0x73, OpCodeValues.Newobj, OpCodes.Newobj, ILOperandTypes.Method, -1, 1, OpCodeTypes.New));
            _Add(new OpCodeData(0x74, OpCodeValues.Castclass, OpCodes.Castclass, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0x75, OpCodeValues.Isinst, OpCodes.Isinst, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0x76, OpCodeValues.Conv_R_Un, OpCodes.Conv_R_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x79, OpCodeValues.Unbox, OpCodes.Unbox, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0x7A, OpCodeValues.Throw, OpCodes.Throw, ILOperandTypes.None, int.MaxValue, 0, OpCodeTypes.Throw));
            _Add(new OpCodeData(0x7B, OpCodeValues.Ldfld, OpCodes.Ldfld, ILOperandTypes.Field, 1, 1, OpCodeTypes.Field | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x7C, OpCodeValues.Ldflda, OpCodes.Ldflda, ILOperandTypes.Field, 1, 1, OpCodeTypes.Field | OpCodeTypes.Ld | OpCodeTypes.Address));
            _Add(new OpCodeData(0x7D, OpCodeValues.Stfld, OpCodes.Stfld, ILOperandTypes.Field, 2, 0, OpCodeTypes.Field | OpCodeTypes.St));
            _Add(new OpCodeData(0x7E, OpCodeValues.Ldsfld, OpCodes.Ldsfld, ILOperandTypes.Field, 0, 1, OpCodeTypes.Field | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x7F, OpCodeValues.Ldsflda, OpCodes.Ldsflda, ILOperandTypes.Field, 0, 1, OpCodeTypes.Field | OpCodeTypes.Ld | OpCodeTypes.Address));
            _Add(new OpCodeData(0x80, OpCodeValues.Stsfld, OpCodes.Stsfld, ILOperandTypes.Field, 1, 0, OpCodeTypes.Field | OpCodeTypes.St));
            _Add(new OpCodeData(0x81, OpCodeValues.Stobj, OpCodes.Stobj, ILOperandTypes.Type, 2, 0));
            _Add(new OpCodeData(0x82, OpCodeValues.Conv_Ovf_I1_Un, OpCodes.Conv_Ovf_I1_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x83, OpCodeValues.Conv_Ovf_I2_Un, OpCodes.Conv_Ovf_I2_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x84, OpCodeValues.Conv_Ovf_I4_Un, OpCodes.Conv_Ovf_I4_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x85, OpCodeValues.Conv_Ovf_I8_Un, OpCodes.Conv_Ovf_I8_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x86, OpCodeValues.Conv_Ovf_U1_Un, OpCodes.Conv_Ovf_U1_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x87, OpCodeValues.Conv_Ovf_U2_Un, OpCodes.Conv_Ovf_U2_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x88, OpCodeValues.Conv_Ovf_U4_Un, OpCodes.Conv_Ovf_U4_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x89, OpCodeValues.Conv_Ovf_U8_Un, OpCodes.Conv_Ovf_U8_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x8A, OpCodeValues.Conv_Ovf_I_Un, OpCodes.Conv_Ovf_I_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x8B, OpCodeValues.Conv_Ovf_U_Un, OpCodes.Conv_Ovf_U_Un, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x8C, OpCodeValues.Box, OpCodes.Box, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0x8D, OpCodeValues.Newarr, OpCodes.Newarr, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0x8E, OpCodeValues.Ldlen, OpCodes.Ldlen, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0x8F, OpCodeValues.Ldelema, OpCodes.Ldelema, ILOperandTypes.Type, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x90, OpCodeValues.Ldelem_I1, OpCodes.Ldelem_I1, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x91, OpCodeValues.Ldelem_U1, OpCodes.Ldelem_U1, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x92, OpCodeValues.Ldelem_I2, OpCodes.Ldelem_I2, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x93, OpCodeValues.Ldelem_U2, OpCodes.Ldelem_U2, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x94, OpCodeValues.Ldelem_I4, OpCodes.Ldelem_I4, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x95, OpCodeValues.Ldelem_U4, OpCodes.Ldelem_U4, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x96, OpCodeValues.Ldelem_I8, OpCodes.Ldelem_I8, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x97, OpCodeValues.Ldelem_I, OpCodes.Ldelem_I, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x98, OpCodeValues.Ldelem_R4, OpCodes.Ldelem_R4, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x99, OpCodeValues.Ldelem_R8, OpCodes.Ldelem_R8, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x9A, OpCodeValues.Ldelem_Ref, OpCodes.Ldelem_Ref, ILOperandTypes.None, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0x9B, OpCodeValues.Stelem_I, OpCodes.Stelem_I, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0x9C, OpCodeValues.Stelem_I1, OpCodes.Stelem_I1, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0x9D, OpCodeValues.Stelem_I2, OpCodes.Stelem_I2, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0x9E, OpCodeValues.Stelem_I4, OpCodes.Stelem_I4, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0x9F, OpCodeValues.Stelem_I8, OpCodes.Stelem_I8, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0xA0, OpCodeValues.Stelem_R4, OpCodes.Stelem_R4, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0xA1, OpCodeValues.Stelem_R8, OpCodes.Stelem_R8, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0xA2, OpCodeValues.Stelem_Ref, OpCodes.Stelem_Ref, ILOperandTypes.None, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0xA3, OpCodeValues.Ldelem_Any, OpCodes.Ldelem_Any, ILOperandTypes.Type, 2, 1, OpCodeTypes.Elem | OpCodeTypes.Ld));
            _Add(new OpCodeData(0xA4, OpCodeValues.Stelem_Any, OpCodes.Stelem_Any, ILOperandTypes.Type, 3, 0, OpCodeTypes.Elem | OpCodeTypes.St));
            _Add(new OpCodeData(0xA5, OpCodeValues.Unbox_Any, OpCodes.Unbox_Any, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0xB3, OpCodeValues.Conv_Ovf_I1, OpCodes.Conv_Ovf_I1, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xB4, OpCodeValues.Conv_Ovf_U1, OpCodes.Conv_Ovf_U1, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xB5, OpCodeValues.Conv_Ovf_I2, OpCodes.Conv_Ovf_I2, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xB6, OpCodeValues.Conv_Ovf_U2, OpCodes.Conv_Ovf_U2, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xB7, OpCodeValues.Conv_Ovf_I4, OpCodes.Conv_Ovf_I4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xB8, OpCodeValues.Conv_Ovf_U4, OpCodes.Conv_Ovf_U4, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xB9, OpCodeValues.Conv_Ovf_I8, OpCodes.Conv_Ovf_I8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xBA, OpCodeValues.Conv_Ovf_U8, OpCodes.Conv_Ovf_U8, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xC2, OpCodeValues.Refanyval, OpCodes.Refanyval, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0xC3, OpCodeValues.Ckfinite, OpCodes.Ckfinite, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xC6, OpCodeValues.Mkrefany, OpCodes.Mkrefany, ILOperandTypes.Type, 1, 1));
            _Add(new OpCodeData(0xD0, OpCodeValues.Ldtoken, OpCodes.Ldtoken, ILOperandTypes.Type_Field_Method, 0, 1));
            _Add(new OpCodeData(0xD1, OpCodeValues.Conv_U2, OpCodes.Conv_U2, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xD2, OpCodeValues.Conv_U1, OpCodes.Conv_U1, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xD3, OpCodeValues.Conv_I, OpCodes.Conv_I, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xD4, OpCodeValues.Conv_Ovf_I, OpCodes.Conv_Ovf_I, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xD5, OpCodeValues.Conv_Ovf_U, OpCodes.Conv_Ovf_U, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xD6, OpCodeValues.Add_Ovf, OpCodes.Add_Ovf, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xD7, OpCodeValues.Add_Ovf_Un, OpCodes.Add_Ovf_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xD8, OpCodeValues.Mul_Ovf, OpCodes.Mul_Ovf, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xD9, OpCodeValues.Mul_Ovf_Un, OpCodes.Mul_Ovf_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xDA, OpCodeValues.Sub_Ovf, OpCodes.Sub_Ovf, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xDB, OpCodeValues.Sub_Ovf_Un, OpCodes.Sub_Ovf_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xDC, OpCodeValues.Endfinally, OpCodes.Endfinally, ILOperandTypes.None, int.MaxValue, 0));
            _Add(new OpCodeData(0xDD, OpCodeValues.Leave, OpCodes.Leave, ILOperandTypes.int32, int.MaxValue, 0, OpCodeTypes.Leave));
            _Add(new OpCodeData(0xDE, OpCodeValues.Leave_S, OpCodes.Leave_S, ILOperandTypes.int8, int.MaxValue, 0, OpCodeTypes.Leave));
            _Add(new OpCodeData(0xDF, OpCodeValues.Stind_I, OpCodes.Stind_I, ILOperandTypes.None, 2, 0));
            _Add(new OpCodeData(0xE0, OpCodeValues.Conv_U, OpCodes.Conv_U, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xFE00, OpCodeValues.Arglist, OpCodes.Arglist, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xFE01, OpCodeValues.Ceq, OpCodes.Ceq, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xFE02, OpCodeValues.Cgt, OpCodes.Cgt, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xFE03, OpCodeValues.Cgt_Un, OpCodes.Cgt_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xFE04, OpCodeValues.Clt, OpCodes.Clt, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xFE05, OpCodeValues.Clt_Un, OpCodes.Clt_Un, ILOperandTypes.None, 2, 1));
            _Add(new OpCodeData(0xFE06, OpCodeValues.Ldftn, OpCodes.Ldftn, ILOperandTypes.Method, 0, 1));
            _Add(new OpCodeData(0xFE07, OpCodeValues.Ldvirtftn, OpCodes.Ldvirtftn, ILOperandTypes.Method, 1, 1));
            _Add(new OpCodeData(0xFE09, OpCodeValues.Ldarg, OpCodes.Ldarg, ILOperandTypes.uint32, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld));
            _Add(new OpCodeData(0xFE0A, OpCodeValues.Ldarga, OpCodes.Ldarga, ILOperandTypes.uint32, 0, 1, OpCodeTypes.Arg | OpCodeTypes.Ld | OpCodeTypes.Address));
            _Add(new OpCodeData(0xFE0B, OpCodeValues.Starg, OpCodes.Starg, ILOperandTypes.uint32, 1, 0, OpCodeTypes.Arg | OpCodeTypes.St));
            _Add(new OpCodeData(0xFE0C, OpCodeValues.Ldloc, OpCodes.Ldloc, ILOperandTypes.uint32, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld));
            _Add(new OpCodeData(0xFE0D, OpCodeValues.Ldloca, OpCodes.Ldloca, ILOperandTypes.uint32, 0, 1, OpCodeTypes.LocVar | OpCodeTypes.Ld | OpCodeTypes.Address));
            _Add(new OpCodeData(0xFE0E, OpCodeValues.Stloc, OpCodes.Stloc, ILOperandTypes.uint32, 1, 0, OpCodeTypes.LocVar | OpCodeTypes.St));
            _Add(new OpCodeData(0xFE0F, OpCodeValues.Localloc, OpCodes.Localloc, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xFE11, OpCodeValues.Endfilter, OpCodes.Endfilter, ILOperandTypes.None, 1, 0));
            _Add(new OpCodeData(0xFE12, OpCodeValues.Unaligned, OpCodes.Unaligned, ILOperandTypes.uint8, 0, 0));
            _Add(new OpCodeData(0xFE13, OpCodeValues.Volatile, OpCodes.Volatile, ILOperandTypes.None, 0, 0));
            _Add(new OpCodeData(0xFE14, OpCodeValues.Tail, OpCodes.Tail, ILOperandTypes.None, 0, 0, OpCodeTypes.None));
            _Add(new OpCodeData(0xFE15, OpCodeValues.Initobj, OpCodes.Initobj, ILOperandTypes.Type, 1, 0));
            _Add(new OpCodeData(0xFE16, OpCodeValues.Constrained, OpCodes.Constrained, ILOperandTypes.Type, 0, 0));
            _Add(new OpCodeData(0xFE17, OpCodeValues.Cpblk, OpCodes.Cpblk, ILOperandTypes.None, 3, 0));
            _Add(new OpCodeData(0xFE18, OpCodeValues.Initblk, OpCodes.Initblk, ILOperandTypes.None, 3, 0));
            _Add(new OpCodeData(0xFE1A, OpCodeValues.Rethrow, OpCodes.Rethrow, ILOperandTypes.None, 0, 0, OpCodeTypes.Throw));
            _Add(new OpCodeData(0xFE1C, OpCodeValues.Sizeof, OpCodes.Sizeof, ILOperandTypes.Type, 0, 1));
            _Add(new OpCodeData(0xFE1D, OpCodeValues.Refanytype, OpCodes.Refanytype, ILOperandTypes.None, 1, 1));
            _Add(new OpCodeData(0xFE1E, OpCodeValues.Readonly, OpCodes.Readonly, ILOperandTypes.None, 0, 0));
        }
    }

    internal class OpCodeData
    {
        internal int OpValue { get; }

        internal OpCode OpCode { get; }

        internal OpCodeValues OpCodeValue { get; }

        internal ILOperandTypes OperandType { get; }

        internal int Pop { get; }

        internal int Push { get; }

        internal OpCodeTypes OpCodeType { get; }

        internal int Index { get; }

        internal bool IsBranch => (OpCodeType & OpCodeTypes.Branch) == OpCodeTypes.Branch || OpCodeType == OpCodeTypes.Leave;
        internal bool IsEnd => OpCode == OpCodes.Ret || OpCode == OpCodes.Throw || OpCode == OpCodes.Rethrow;
        internal OpCodeTypes BranchType => OpCodeType ^ OpCodeTypes.Branch;
        internal bool IsAddress => (OpCodeType & OpCodeTypes.Address) == OpCodeTypes.Address;
        internal OpCodeData(int opValue, OpCodeValues opCodeValue, OpCode opCode, ILOperandTypes operanType, int pop, int push, OpCodeTypes opCodeType = OpCodeTypes.None, int index = -1)
        {
            this.OpValue = opValue;
            this.OpCodeValue = opCodeValue;
            this.OpCode = opCode;
            this.OperandType = operanType;
            this.Pop = pop;
            this.Push = push;
            this.OpCodeType = opCodeType;
            this.Index = index;
        }
    }

    internal enum OpCodeTypes : uint
    {
        None = 0,
        Branch = 1,
        Call = 2,
        Arg = 8,
        LocVar = 16,
        Field = 32,
        Throw = 64,
        New = 128,
        Elem = 256,
        Ld = 512,
        St = 1024,
        Cond = 2048,
        UnCond = 4096,
        Leave = 8192,
        Address = 16384,
        Return = 32768
    }

    internal enum ILOperandTypes
    {
        None,
        Type,
        Field,
        float32,
        int32,
        float64,
        int64,
        int8,
        Method,
        Signature,
        String,
        Switch,
        uint32,
        uint8,
        Type_Field_Method
    }
}