# Firefly

This is an practice of using geometry shader to recontruct particle mesh for implementing special effects.

The original idea is from [*Firefly*](https://github.com/keijiro/Firefly) which is using of Unity ECS, C# Job
System and the Burst compiler, this project moves the calculations into geometry shader and compute shader.

The basic flow listed below represents how it works. At First I convert input source mesh to ComputeBuffer and 
calculate the particle motion through ComputeShader, and then sending those buffers into geometry shader 
to reconstruct butterfly particle mesh.

```mermaid
flowchart TD
SourceMesh-->|Store triangles with its center as barycenter of each face|VertexData
VertexData-->DrawMesh
DrawMesh-->GeometryStage
VertexData-->|Used as base vertices|GeometryStage
ParticleData-->|Calculate pos and velocity in compute shader|FixUpdate
FixUpdate-->|Used to reconstruct butterfly vertices|GeometryStage
Editor-->|Parameters from inspector|ParticleVariantData
ParticleVariantData-->|Used to calculate particle size and lifetime|GeometryStage
GeometryStage-->Output
```
I also implemented a cpu version in order to correct the calculation of particle motion. To mix particle velocity 
and inverse direction of the particle with the time direction, we can easily control the forward/reverse motion effects.

![Forward](Docs/forward.gif "Forward")
![Reverse](Docs/reverse.gif "Reverse")

The cpu version is very slow when applying source mesh with huge vertives. After I move those works into gpu, then we 
can apply on the huge mesh with better performance as well :-)

![Reverse](Docs/geometry-shader-impl.gif "Implement with geometry shader")

# Environment
- Unity 2019.4.1f1

# Reference
- https://github.com/keijiro/Firefly
