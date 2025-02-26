using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed unsafe class DatabaseColumnsGenerator : MonoBehaviour
{
    [SerializeField] string _pathToStruct;

    public void Generate()
    {
        string structName = Path.GetFileNameWithoutExtension(_pathToStruct);
        string directoryName = Path.GetDirectoryName(_pathToStruct).Replace('\\', '/');

        string fileContent = File.ReadAllText(_pathToStruct);
        fileContent = CleanReadonly(fileContent);

        string structContent = ExtractStruct(fileContent, structName);
        var fields = ParseCSharpStruct(structContent);

        string outputContent = GenerateOutputFile(structName, fields);
        string outputPath = $"{directoryName}/{structName}Columns.cs";

        File.WriteAllText(outputPath, outputContent);

        Debug.Log($"Created database columns: {outputPath}");
    }

    static string ExtractStruct(string fileContent, string structName)
    {
        string pattern = $@"public struct {Regex.Escape(structName)}\s*\{{[^}}]*\}}";
        var match = Regex.Match(fileContent, pattern, RegexOptions.Singleline);

        if (!match.Success)
            throw new Exception($"Struct named {structName} not found in the file");

        return match.Value;
    }

    static string CleanReadonly(string structContent)
    {
        return Regex.Replace(structContent, @"\breadonly\b ", "");
    }

    static (string Type, string Name)[] ParseCSharpStruct(string structContent)
    {
        var matches = Regex.Matches(structContent, @"public\s+([^\s]+)\s+([^\s;]+)\s*;");

        if (matches.Count == 0)
            throw new Exception("No public fields found in the struct");

        var fields = new (string Type, string Name)[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            fields[i] = (matches[i].Groups[1].Value, matches[i].Groups[2].Value);
        }

        return fields;
    }

    static string GenerateOutputFile(string structName, (string Type, string Name)[] fields)
    {
        var output = $@"using Ces.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[NoAlias]
public unsafe struct {structName}Columns : IDatabaseColumns<{structName}, {structName}Columns>
{{
";
        foreach (var (type, name) in fields)
        {
            output += $"    [NativeDisableUnsafePtrRestriction, NoAlias]{Environment.NewLine}    public {type}* {name};{Environment.NewLine}{Environment.NewLine}";
        }

        output += $@"    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsCreated() => {fields[0].Name} != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Allocate(Allocator allocator, int capacity)
    {{
";
        foreach (var (type, name) in fields)
        {
            output += $"        {name} = CesMemoryUtility.AllocateCacheDefault<{type}>(capacity, allocator, default);{Environment.NewLine}";
        }

        output += $@"    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose(Allocator allocator)
    {{
";
        foreach (var (_, name) in fields)
        {
            output += $"        CesMemoryUtility.FreeAndNullify(ref {name}, allocator);{Environment.NewLine}";
        }

        output += $@"    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly {structName} Get(int index) => new()
    {{
";
        foreach (var (_, name) in fields)
        {
            output += $"        {name} = {name}[index],{Environment.NewLine}";
        }

        output += $@"    }};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Set(int index, {structName} instance)
    {{
";
        foreach (var (_, name) in fields)
        {
            output += $"        {name}[index] = instance.{name};{Environment.NewLine}";
        }

        output += $@"    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Move(int from, int to)
    {{
";
        foreach (var (_, name) in fields)
        {
            output += $"        {name}[to] = {name}[from];{Environment.NewLine}";
        }

        output += $@"    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Copy(in {structName}Columns from, int capacity)
    {{
";
        foreach (var (_, name) in fields)
        {
            output += $"        CesMemoryUtility.Copy(capacity, {name}, from.{name});{Environment.NewLine}";
        }

        output += $"    }}{Environment.NewLine}}}{Environment.NewLine}";

        return output;
    }
}

[CustomEditor(typeof(DatabaseColumnsGenerator))]
public sealed class DatabaseTableColumnsGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate"))
        {
            var dtcg = (DatabaseColumnsGenerator)target;
            dtcg.Generate();
        }
    }
}