# Pipelines

![](Docs\Pipelines.png)

## Description

A simple editor of pipes for Unity 3D engine.

## Goals

Library...

- generates mesh (with normals and tangents) of a pipe or multiple pipes.
- utilizes Unity Jobs System for multithreading and Burst Compiler for optimization.

## Instructions

> Some colors in GIFs can buggy due to the compression.

![](Docs\Animation1.gif)

`MonoPipe` has the following elements:

**Nodes** are points (`x`,`y` and `z`) in scene space between each, pipe segments are generated. Every node has also radius (`w`) of corresponding bending.

**Cut Max Angle** is a approximate angular size of all bends' slices spanning between cuts.

**Minimal Bend Angle** is a limit angle. If bend has smaller angle, it will not be generated.

The library also contains automatic `HorizontalMultiPipe`.

> `HorizontalMultiPipe` does not support changing a number of pipes or nodes in runtime.
>
> Pipes will not appear if the input is invalid (e.g. pipes create angle that disallows making horizontal layout).

![](Docs\Animation2.gif)

Both components update each frame unless disabled.

## Implementation

The core consists of 2 burstable jobs: First one takes *nodes* in form of `NativeArray<float4>` and calculates *cuts*. Second job takes the cuts and creates mesh by building circles around the cuts. 

Memory buffers, the jobs operate on, are taken care off by `PipeJobDispatcher`.

Such system is extremely modular, and thus extensible. Job can be easily modified, as well as buffers management.

The system utilizes Jobs and Burst compiler without any workarounds. Generating mesh does not allocate any memory. On my PC for 120 concurrently rebuilt simple pipes and safety locks enabled, burst enhances FPS from 35 to 75.

And final screenshot of the profiler :>

![](Docs\Profiler.png)

## License

License is available in LICENSE file.
