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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int InvokeJSFunction(
            string internedFunctionName, int argumentCount,
            IntPtr type1, IntPtr arg1,
            IntPtr type2, IntPtr arg2,
            IntPtr type3, IntPtr arg3
        );

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetGlobalObject(string globalName, out int exceptionalResult);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object InvokeJSWithArgs(int jsObjHandle, string method, object[] parms, out int exceptionalResult);
    }
}

namespace BP {
    public class BenchmarkTestStructMarshaler {
        public static BenchmarkTestStruct FromJavaScript (int i) {
            return new BenchmarkTestStruct { I = i };
        }

        public static int ToJavaScript (in BenchmarkTestStruct cts) {
            return cts.I;
        }
    }

    public struct BenchmarkTestStruct {
        public int I;
    }

    public class BenchmarkTestStructWithFilterMarshaler {
        public static string FromJavaScriptPreFilter () => "return (value + 0.1)";
        public static string ToJavaScriptPostFilter () => "return (value | 0)";

        public static BenchmarkTestStructWithFilter FromJavaScript (double d) {
            return new BenchmarkTestStructWithFilter { D = d };
        }

        public static double ToJavaScript (in BenchmarkTestStructWithFilter cts) {
            return cts.D;
        }
    }

    public struct BenchmarkTestStructWithFilter {
        public double D;
    }

    public class BenchmarkTestClassWithFilterMarshaler {
        public static string FromJavaScriptPreFilter () => "return (value + 0.1)";
        public static string ToJavaScriptPostFilter () => "return (value | 0)";

        public static BenchmarkTestClassWithFilter FromJavaScript (double d) {
            return new BenchmarkTestClassWithFilter { D = d };
        }

        public static double ToJavaScript (BenchmarkTestClassWithFilter cc) {
            return cc.D;
        }
    }

    public class BenchmarkTestClassWithFilter {
        public double D;
    }

    public static class BenchmarkExports {
        public static void AcceptCustomStruct (BenchmarkTestStruct s) {
            ;
        }

        public static void AcceptCustomStructWithFilter (BenchmarkTestStructWithFilter s) {
            ;
        }

        public static BenchmarkTestStruct ReturnCustomStruct (BenchmarkTestStruct s) {
            return s;
        }

        public static BenchmarkTestStructWithFilter ReturnCustomStructWithFilter (BenchmarkTestStructWithFilter s) {
            return s;
        }

        public static BenchmarkTestClassWithFilter ReturnCustomClassWithFilter (BenchmarkTestClassWithFilter s) {
            return s;
        }

        public static double ReturnDouble (double d) {
            return d;
        }

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
    static string InvokeJSExpression, InvokeJSExpressionDifferentValue;

    const int InvokeIterationCountSmall = 100,
        InvokeIterationCountLarge = 100,
        // FIXME: If I make this much larger the runtime just silently crashes
        InvokeIterationCountHuge = 10000;

    static BindingPerformance () {
        var sb = new System.Text.StringBuilder();
        // We want to be absolutely certain that this string is not turned into a literal
        //  even if the compiler and JIT get very clever, so this is our best attempt
        sb.Append("if (globalThis['nonexistent'] !== undefined)");
        sb.Append(' ');
        sb.Append("throw new Error('what'");
        sb.Append(new string(')', 1));
        InvokeJSExpression = sb.ToString();

        // Because of how String.IsInterned looks, the above string may end up being compared
        //  against strings in the intern table with the same hashcode.
        // In order to separate the cost of that out, we create a new string with a different
        //  hashcode and value so that it shouldn't get compared against the interned literal.
        sb.Clear();
        sb.Append("if (globalThis['nonexistent'] !== undefined)");
        sb.Append(' ');
        sb.Append("throw new Error('WHAT'");
        sb.Append(new string(')', 1));
        InvokeJSExpressionDifferentValue = sb.ToString();

        if (InvokeJSExpression.GetHashCode() == InvokeJSExpressionDifferentValue.GetHashCode())
            throw new Exception("Both expressions' hashcodes are the same");

        // HACK: I don't like this any more than you do
        RegisterCustomMarshaler<BP.BenchmarkTestStruct, BP.BenchmarkTestStructMarshaler>();
        RegisterCustomMarshaler<BP.BenchmarkTestStructWithFilter, BP.BenchmarkTestStructWithFilterMarshaler>();
        RegisterCustomMarshaler<BP.BenchmarkTestClassWithFilter, BP.BenchmarkTestClassWithFilterMarshaler>();

        int temp;
        Interop.Runtime.InvokeJS("globalThis.perftest1arg = function (n) { globalThis.perftest_n = n | 0; };", out temp);
        // HACK: Alternate name so we can measure the positive impact of string interning on the method name
        Interop.Runtime.InvokeJS("globalThis.perftest1arg2 = globalThis.perftest1arg;", out temp);
        Interop.Runtime.InvokeJS("globalThis.perfteststr = function (s) { globalThis.perftest_s = s; };", out temp);
    }

    private static void RegisterCustomMarshaler<T, TMarshaler> () {
        var taqn = typeof(T).AssemblyQualifiedName;
        var maqn = typeof(TMarshaler).AssemblyQualifiedName;
        var js = $"MONO.mono_wasm_register_custom_marshaler('{taqn}', '{maqn}')";
        var res = Interop.Runtime.InvokeJS(js, out int exceptionalResult);
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

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
for (var i = 0; i < " + InvokeIterationCountSmall + @"; i++)
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
for (var i = 0; i < " + InvokeIterationCountSmall + @"; i++)
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
for (var i = 0; i < " + InvokeIterationCountSmall + @"; i++)
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
@"var args = [""string literal with embedded null \0\0 hmm""];
for (var i = 0; i < " + InvokeIterationCountSmall + @"; i++)
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
@"var args = [""string literal with embedded null \0\0 yey""];
for (var i = 0; i < " + InvokeIterationCountSmall + @"; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", args, ""S"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

/*
    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallMethod_ReturnSymbol ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var args = [Symbol.for(""string literal with embedded null \0\0 sym"")];
for (var i = 0; i < 1000; i++)
    Module.mono_call_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", args, ""S"");
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }
    */

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_Void ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:VoidAction"", """");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
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
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(1, 2);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_ReturnString ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var literal1 = ""string literal with embedded null \0\0 wow"";
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", ""s"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(literal1);
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
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
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
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    invokeMethod(methodPtr, 0, buffer + 16, buffer);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeJS_NoResult_NonLiteralString_SameValue ()
    {
        var res = Interop.Runtime.InvokeJS(InvokeJSExpression, out int exceptionalResult);
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeJS_NoResult_NonLiteralString_DifferentValue ()
    {
        var res = Interop.Runtime.InvokeJS(InvokeJSExpressionDifferentValue, out int exceptionalResult);
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

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_ReturnDouble ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var val = 2345.678;
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnDouble"", ""d"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(val);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

#if TRUE
    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_ReturnStringAutoSignature ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var literal1 = ""string literal with embedded null \0\0 zow"";
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnString"", ""a"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(literal1);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_PassStructWithManagedConverter ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var val = 1234;
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:AcceptCustomStruct"", ""a"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(val);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_PassStructWithManagedConverterAndFilter ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var val = 2345.678;
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:AcceptCustomStructWithFilter"", ""a"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(val);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_ReturnStructWithManagedConverterAndFilter ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var val = 2345.678;
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnCustomStructWithFilter"", ""a"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(val);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void CallBoundMethod_ReturnClassWithManagedConverterAndFilter ()
    {
        var res = Interop.Runtime.InvokeJS(
@"var val = 2345.678;
var bound = Module.mono_bind_static_method(""[MicroBenchmarks] BP.BenchmarkExports:ReturnCustomClassWithFilter"", ""a"");
for (var i = 0; i < " + InvokeIterationCountLarge + @"; i++)
    bound(val);
", out int exceptionalResult
        );
        if (exceptionalResult != 0)
            throw new Exception("InvokeJS failed " + res);
    }
#endif

#if TRUE
    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeMethodIntViaInvokeJS ()
    {
        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            Interop.Runtime.InvokeJS("globalThis.perftest1arg(7);", out int temp);
            if (temp != 0)
                throw new Exception("InvokeJS failed");
        }

        if (Interop.Runtime.InvokeJS("globalThis.perftest_n", out int res) != "7")
            throw new Exception("Incorrect result");
        else if (res != 0)
            throw new Exception("InvokeJS failed");
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public unsafe void InvokeMethodIntViaNewInvokeIcall ()
    {
        int v = 7;
        var thandle = typeof(int).TypeHandle.Value;
        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            var code = Interop.Runtime.InvokeJSFunction(
                "perftest1arg", 1,
                // We could hoist the AsPointer out of the loop to try and optimize it, but
                //  it doesn't seem to make a difference and the new API is really fast anyway.
                thandle, (IntPtr)Unsafe.AsPointer(ref v),
                IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero
            );
            if (code != 0)
                throw new Exception("InvokeJSFunction failed");
        }
        
        if (Interop.Runtime.InvokeJS("globalThis.perftest_n", out int res) != "7")
            throw new Exception("Incorrect result");
        else if (res != 0)
            throw new Exception("InvokeJS failed");
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeMethodIntViaInvokeJSWithArgs () {
        int exception;
        var thisHandle = GetGlobalThisAsJSObjectHandle();
        var args = new object[] { 7 };

        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            object res = Interop.Runtime.InvokeJSWithArgs(thisHandle, "perftest1arg", args, out exception);
            if (exception != 0)
                throw new Exception("InvokeJSWithArgs failed");
        }
        
        if (Interop.Runtime.InvokeJS("globalThis.perftest_n", out exception) != "7")
            throw new Exception("Incorrect result");
        else if (exception != 0)
            throw new Exception("InvokeJS failed");        
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeMethodIntViaInvokeJSWithArgs_InternedName () {
        int exception;
        var thisHandle = GetGlobalThisAsJSObjectHandle();
        var args = new object[] { 7 };
        // NOTE: This isn't actually necessary for string literals, but let's be explicit
        var name = String.Intern("perftest1arg2");
        // HACK: Ensure that the literal has been interned on the JS side as well so that InvokeJSWithArgs
        //  will not need to copy the string when marshaling the method name.
        // InvokeJS automatically will add its argument to the intern table if appropriate.
        Interop.Runtime.InvokeJS(name, out exception);

        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            object res = Interop.Runtime.InvokeJSWithArgs(thisHandle, name, args, out exception);
            if (exception != 0)
                throw new Exception("InvokeJSWithArgs failed");
        }
        
        if (Interop.Runtime.InvokeJS("globalThis.perftest_n", out exception) != "7")
            throw new Exception("Incorrect result");
        else if (exception != 0)
            throw new Exception("InvokeJS failed");        
    }
    
    const string invokeTestString = "aaaa the quick brown fox judged my sphinx of quartz. hear my vow, oh lazy dogs zzzz";

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeMethodStringViaInvokeJS ()
    {
        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            Interop.Runtime.InvokeJS($"globalThis.perfteststr('{invokeTestString}');", out int temp);
            if (temp != 0)
                throw new Exception("InvokeJS failed");
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public unsafe void InvokeMethodStringViaNewInvokeIcall ()
    {
        var s = String.Intern(invokeTestString);
        GCHandle hStr = GCHandle.Alloc(s);
        IntPtr pStr = *(IntPtr*)Unsafe.AsPointer(ref s), thandle = typeof(string).TypeHandle.Value;
        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            var code = Interop.Runtime.InvokeJSFunction(
                "perfteststr", 1,
                // We could hoist the AsPointer out of the loop to try and optimize it, but
                //  it doesn't seem to make a difference and the new API is really fast anyway.
                thandle, pStr,
                IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero
            );
            if (code != 0)
                throw new Exception("InvokeJSFunction failed");
        }
    }

    [Benchmark]
    [BenchmarkCategory(Categories.Runtime, Categories.OnlyWASM)]
    public void InvokeMethodStringViaInvokeJSWithArgs () {
        int exception;
        var thisHandle = GetGlobalThisAsJSObjectHandle();
        var args = new object[] { invokeTestString };

        for (var i = 0; i < InvokeIterationCountHuge; i++) {
            object res = Interop.Runtime.InvokeJSWithArgs(thisHandle, "perfteststr", args, out exception);
            if (exception != 0)
                throw new Exception("InvokeJSWithArgs failed");
        }
    }


    private static int GetGlobalThisAsJSObjectHandle () {
        int exception;
        object thisHandleObj = Interop.Runtime.GetGlobalObject(null, out exception);

        if ((exception != 0) || (thisHandleObj == null))
            throw new Exception($"Error obtaining a handle to globalThis");

        var p = thisHandleObj.GetType().GetProperty("JSHandle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p == null)
            throw new Exception($"No JSHandle property found on type {thisHandleObj.GetType().FullName}");
        var thisHandleBoxed = p.GetValue(thisHandleObj);
        if (thisHandleBoxed == null)
            throw new Exception($"JSHandle was null");
        return (int)thisHandleBoxed;
    }

#endif
}
