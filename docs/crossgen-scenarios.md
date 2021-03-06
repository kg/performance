
# Crossgen Scenarios
An introduction of how to run scenario tests can be found in [Scenarios Tests Guide](./scenarios-workflow.md). The current document has specific instruction to run:

- [Crossgen Throughput Scenario](#crossgen-throughput)
- [Crossgen2 Throughput Scenario](#crossgen2-throughput)


## Crossgen Throughput Scenario
**Crossgen Throughput** is a scenario test that measures the throughput of [crossgen compilation](https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/crossgen.md). To be more specific, our test *implicitly* calls
```
.\crossgen.exe <assembly to compile>
``` 
with other applicable arguments and measures its throughput.

We will walk through crossgen compiling `System.Private.Xml.dll` as an example.

### Prerequisites
- python3 or newer
- dotnet runtime 3.0 or newer
- terminal/command prompt **in Admin Mode** (for collecting kernel traces)
- clean state of the test machine (anti-virus scan is off and no other user program's running -- to minimize the influence of environment on the test)

### Step 0 Generate Core Root
To run the test, we need to generate [Core_Root](https://github.com/dotnet/runtime/blob/master/docs/workflow/testing/using-corerun.md) for the crossgen tool itself and other runtime assmblies as compilation input. Core_Root is an intermediate output from the runtime build, which contains runtime assemblies and tools.

You can skip this step if you already have Core_Root. To generate Core_Root directory, first clone [dotnet/runtime repo](https://github.com/dotnet/runtime) and follow [the instruction of building coreclr tests](https://github.com/dotnet/runtime/blob/master/docs/workflow/testing/coreclr/windows-test-instructions.md), which creates Core_Root directory.

If the build's successful, you should have Core_Root with the path like:
```
 runtime\artifacts\tests\coreclr\<OS>.<Arch>.<BuildType>\Tests\Core_Root
```

### Step 1 Initialize Environment
Same instruction of [Scenario Tests Guide - Step 1](./scenarios-workflow.md#step-1-initialize-environment).
### Step 2 Run Precommand
For **Crossgen Throughput** scenario, unlike other scenarios there's no need to run any precommand (`pre.py`). Just switch to the test asset directory:
```
cd crossgen
```
### Step 3 Run Test
Now run the test, in our example we use `System.Private.Xml.dll` under Core_Root as the input assembly to compile, and you can replace it with other assemblies **under Core_Root**.

```
python3 test.py crossgen --core-root <path to core_root>\Core_Root --test-name System.Private.Xml.dll
```
This will run the test harness [Startup Tool](https://github.com/dotnet/performance/tree/master/src/tools/ScenarioMeasurement/Startup), which runs crossgen compilation in several iterations and measures its throughput. The result will be something like this:

```
[2020/09/25 09:54:48][INFO] Parsing traces\Crossgen Throughput - System.Private.Xml.etl
[2020/09/25 09:54:49][INFO] Crossgen Throughput - System.Private.Xml.dll
[2020/09/25 09:54:49][INFO] Metric         |Average        |Min            |Max
[2020/09/25 09:54:49][INFO] ---------------|---------------|---------------|---------------
[2020/09/25 09:54:49][INFO] Process Time   |7295.825 ms    |7295.825 ms    |7295.825 ms
[2020/09/25 09:54:49][INFO] Time on Thread |7276.019 ms    |7276.019 ms    |7276.019 ms
```


### Step 4 Run Postcommand
Same instruction of [Scenario Tests Guide - Step 4](./scenarios-workflow.md#step-4-run-postcommand).
 
## Crossgen2 Throughput Scenario
Compared to `Crossgen Throughput` scenario, `Crossgen2 Throughput` Scenario measures more metrics, which are:
- Process Time (Throughput)
- Loading Interval
- Emitting Interval
- Jit Interval
- Compilation Interval
  
Steps to run **Crossgen2 Throughput** scenario are very similar to those of **Crossgen Throughput**. Inn addition to compilation of a single file, composite compilation is enabled in crossgen2, so the test command is different.

### Prerequisites
Same as [Crossgen Throughput Prerequisites](#prerequisites)
### Step 0 Generate Core_Root
Same as [Crossgen Throughput Step 0](#step-0-generate-core-root)
### Step 1 Initialize Environment
Same as [Crossgen Throughput Step 1](#step-1-initialize-environment)
### Step 2 Run Precommand
Same as **Crossgen Throughput** scenario, there's no need to run any precommand (`pre.py`). Just switch to the test asset directory:
```
cd crossgen2
```
### Step 3 Run Test
For scenario which compiles a **single assembly**, we use `System.Private.Xml.dll` as an example, you can replace it with other assembly **under Core_Root**:
```
python3 test.py crossgen2 --core-root <path to core_root>\Core_Root --single System.Private.Xml.dll
```

For scenario which does **composite compilation**, we try to compile the majority of runtime assemblies represented by [framework-r2r.dll.rsp](https://github.com/dotnet/performance/blob/master/src/scenarios/crossgen2/framework-r2r.dll.rsp):
```
python3 test.py crossgen2 --core-root <path to core_root>\Core_Root --composite <repo root>/src/scenarios/crossgen2/framework-r2r.dll.rsp
```
Note that for the composite scenario, the command line can exceed the maximum length if it takes a list of paths to assemblies, so an `.rsp` file is used to avoid it.  `--composite <rsp file>` option refers to a rsp file that contains a list of assemblies to compile. A sample file [framework-r2r.dll.rsp](https://github.com/dotnet/performance/blob/master/src/scenarios/crossgen2/framework-r2r.dll.rsp) can be found under `crossgen2\` folder.
 
The test command runs the test harness [Startup Tool](https://github.com/dotnet/performance/tree/master/src/tools/ScenarioMeasurement/Startup), which runs crossgen2 compilation in several iterations and measures its throughput. The result should partially look like:
 ```
 [2020/09/25 10:25:09][INFO] Merging traces\Crossgen2 Throughput - Single - System.Private.perflabkernel.etl,traces\Crossgen2 Throughput - Single - System.Private.perflabuser.etl...
[2020/09/25 10:25:11][INFO] Trace Saved to traces\Crossgen2 Throughput - Single - System.Private.etl
[2020/09/25 10:25:11][INFO] Parsing traces\Crossgen2 Throughput - Single - System.Private.etl
[2020/09/25 10:25:15][INFO] Crossgen2 Throughput - Single - System.Private.CoreLib
[2020/09/25 10:25:15][INFO] Metric               |Average        |Min            |Max
[2020/09/25 10:25:15][INFO] ---------------------|---------------|---------------|---------------
[2020/09/25 10:25:15][INFO] Process Time         |13550.728 ms   |13550.728 ms   |13550.728 ms
[2020/09/25 10:25:15][INFO] Loading Interval     |1090.205 ms    |1090.205 ms    |1090.205 ms
[2020/09/25 10:25:15][INFO] Emitting Interval    |1330.489 ms    |1330.489 ms    |1330.489 ms
[2020/09/25 10:25:15][INFO] Jit Interval         |9464.402 ms    |9464.402 ms    |9464.402 ms
[2020/09/25 10:25:15][INFO] Compilation Interval |12827.350 ms   |12827.350 ms   |12827.350 ms
 ```
 ### Step 4 Run Postcommand
Same instruction of [Scenario Tests Guide - Step 4](./scenarios-workflow.md#step-4-run-postcommand).

## Command Matrix
For the purpose of quick reference, the commands can be summarized into the following matrix:
| Scenario                               | Asset Directory | Precommand | Testcommand                                                                      | Postcommand | Supported Framework | Supported Platform      |
|----------------------------------------|-----------------|------------|----------------------------------------------------------------------------------|-------------|---------------------|-------------------------|
| Crossgen Throughput                    | crossgen        | N/A        | test.py crossgen --core-root <path to Core_Root> --test-name <assembly name> | post.py     | N/A                 | Windows-x64;Windows-x86 |
| Crossgen2 Throughput (single assembly) | crossgen2       | N/A        | test.py crossgen2 --core-root <path to Core_Root> --single <assembly name>       | post.py     | N/A                 | Windows-x64;Linux       |
| Crossgen2 Throughput (composite)       | crossgen2       | N/A        | test.py crossgen2 --core-root <path to Core_Root> --composite <path to .rsp>     | post.py     | N/A                 | Windows-x64;Linux       |

## Relevant Links
[Crossgen2 Compilation Structure Enhancements](https://github.com/dotnet/runtime/blob/master/docs/design/features/crossgen2-compilation-structure-enhancements.md)
