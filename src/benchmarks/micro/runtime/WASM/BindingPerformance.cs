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

public class BindingPerformance
{
    [GlobalSetup]
    public void Setup()
    {
        try {
            // FIXME: Calling methods from binding_support obliterates the runtime
            var res = Interop.Runtime.InvokeJS(
@"try { console.log(""// js:"", Module.mono_method_resolve(""BenchmarkExports.VoidAction"")); } catch (exc) { console.log(""// error:"", exc.toString()); }",
    out int whatever
            );
            Console.WriteLine("// invokejs: " + res);
        } catch (Exception exc) {
            Console.WriteLine("// uncaught: {0}", exc);
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_Void ()
    {
        return;
        var res = Interop.Runtime.InvokeJS(
@"var args = [];
for (var i = 0; i < 10000; i++)
    Module.mono_call_static_method(""BenchmarkExports.VoidAction"", args, """");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_Sum ()
    {
        return;
        var res = Interop.Runtime.InvokeJS(
@"var args = [1, 2];
for (var i = 0; i < 10000; i++)
    Module.mono_call_static_method(""BenchmarkExports.Sum"", args, ""ii"");
", out int exceptionalResult
        );
        return;
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ConcatString ()
    {
        return;
        var res = Interop.Runtime.InvokeJS(
@"var args = [""hello"", "" world""];
for (var i = 0; i < 10000; i++)
    Module.mono_call_static_method(""BenchmarkExports.ConcatString"", args, ""ss"");
", out int exceptionalResult
        );
        return;
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ReturnString ()
    {
        return;
        var res = Interop.Runtime.InvokeJS(
@"var args = [""string literal with embedded null \0\0 ok""];
for (var i = 0; i < 10000; i++)
    Module.mono_call_static_method(""BenchmarkExports.ReturnString"", args, ""s"");
", out int exceptionalResult
        );
        return;
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }
}
