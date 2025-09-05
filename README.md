# ParallelBuildDebuggingLogger
MSBuild logger to assist with debugging parallel-build issues.

## Usage

Something like

```
msbuild {pathto}.binlog -l:{pathto}Parallelbuilddebugginglogger.dll -noconlog
```

Will emit a `PBDL.html` that details MSBuild's view of the different project instances that have been built.