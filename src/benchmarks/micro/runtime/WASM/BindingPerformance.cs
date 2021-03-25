// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
#pragma warning disable CS0162

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

internal static partial class Interop
{
    internal static partial class Runtime
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string InvokeJS(string str, out int exceptionalResult);
    }
}

namespace BP {
    public static class BenchmarkExports {
        public static void VoidAction () {
            ;
        }

        public static int Sum (int a, int b) {
            return a + b;
        }

        public static string ConcatString (string a, string b) {
            return a + b;
        }

        public static string ReturnString (string s) {
            return s;
        }
    }
}

public class BindingPerformance
{
    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_Void ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:VoidAction"", args, """");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_Sum ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [1, 2];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:Sum"", args, ""ii"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ConcatString ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [""hello"", "" world""];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ConcatString"", args, ""ss"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ReturnString ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [""string literal with embedded null \0\0 ok""];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", args, ""s"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ReturnInternedString ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [""string literal with embedded null \0\0 ok""];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", args, ""S"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ReturnSymbol ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [Symbol.for(""string literal with embedded null \0\0 symbol"")];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", args, ""S"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_Void ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:VoidAction"", """");
for (var i = 0; i < 1000; i++)
    bound();
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_Sum ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:Sum"", ""ii"");
for (var i = 0; i < 1000; i++)
    bound(1, 2);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_ReturnInternedString ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var literal2 = ""string literal with embedded null \0\0 yay"";
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", ""S"");
for (var i = 0; i < 1000; i++)
    bound(literal2);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethodUnsafeDirect_Void ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var methodPtr = Module.mono_method_resolve(""[MicroBenchmarks] BP.BenchmarkExports:VoidAction"");
if (!methodPtr) throw new Error(""method not resolved"");
var buffer = Module._malloc(64);
Module.HEAP8.fill(0, buffer, 64);
var invokeMethod = Module.cwrap ('mono_wasm_invoke_method', 'number', ['number', 'number', 'number', 'number']);
for (var i = 0; i < 1000; i++)
    invokeMethod(methodPtr, 0, buffer + 16, buffer);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeJS_NoResult ()
    {
        var res = Interop.Runtime.InvokeJS(@"if (globalThis['nonexistent'] !== undefined) throw new Error('what')", out int exceptionalResult);
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeJS_NumericResult ()
    {
        var res = Interop.Runtime.InvokeJS(@"1 + 2", out int exceptionalResult);
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
        else if (res != "3")
            throw new Exception("InvokeJS returned invalid result " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeJS_StringResult ()
    {
        var testString = "the quick brown fox jumped over the lazy dogs. ";
        testString = testString + testString + testString + testString;
        var res = Interop.Runtime.InvokeJS($"'{testString}'", out int exceptionalResult);
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
        else if (res != testString)
            throw new Exception("InvokeJS returned invalid result " + res);
    }
}
