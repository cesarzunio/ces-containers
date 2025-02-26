using Ces.Collections;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.Port;

namespace Ces.Collections
{
    public static unsafe class DatabaseSerializeUtility
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WriteDatabaseMulTablesCounts<TDatabaseTable>(in FileStream fileStream, in TDatabaseTable* databaseTables, int tablesAmount)
            where TDatabaseTable : unmanaged, IDatabaseTable
        {
            var savesTable = stackalloc int[tablesAmount];

            for (int i = 0; i < tablesAmount; i++)
            {
                savesTable[i] = databaseTables[i].GetCount();
            }

            BinarySaveUtility.WriteArraySimple(in fileStream, savesTable, tablesAmount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WriteDatabaseMulTables<TDatabaseTable>(in FileStream fileStream, in TDatabaseTable* databaseTables, int tablesAmount)
            where TDatabaseTable : unmanaged, IDatabaseTable
        {
            for (int i = 0; i < tablesAmount; i++)
            {
                BinarySaveUtility.WriteArraySimple(in fileStream, databaseTables[i].GetIndexToId(), databaseTables[i].GetCount());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReadDatabaseMulTables<TDatabaseTable>(in FileStream fileStream, in TDatabaseTable* databaseTables, int tablesAmount)
            where TDatabaseTable : unmanaged, IDatabaseTable
        {
            for (int i = 0; i < tablesAmount; i++)
            {
                BinaryReadUtility.ReadArraySimple(in fileStream, databaseTables[i].GetIndexToId(), databaseTables[i].GetCount());
            }
        }
    }
}