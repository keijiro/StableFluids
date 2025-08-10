# StableFluids

![gif](https://i.imgur.com/XLZlc2e.gif)
![gif](https://i.imgur.com/FJBiGbk.gif)

This project is a straightforward GPU-based implementation of Jos Stam's
[Stable Fluids] in Unity.

[Stable Fluids]: https://www.dgp.toronto.edu/people/stam/reality/Research/pdf/ns.pdf

[WebGL Demo](https://keijiro.github.io/StableFluids)

## System Requirements

- Unity 6

## Project Structure

While this project uses URP, the following modules depend only on the Core
Render Pipeline shader library, making them compatible with any render pipeline
(Built-in/Universal/High-Definition).

### `Assets/StableFluids`

Contains Jos Stam's Stable Fluids implementation. Access the velocity field
texture via `FluidSimulation.VelocityField` for rendering or to apply external
forces.

### `Assets/Marbling`

Contains components for the marbling demo, which advects colors based on the
velocity field.

## References

- [Stable Fluids, Jos Stam](https://www.dgp.toronto.edu/people/stam/reality/Research/pdf/ns.pdf)
- [Real-Time Fluid Dynamics for Games, Jos Stam](https://pdfs.semanticscholar.org/847f/819a4ea14bd789aca8bc88e85e906cfc657c.pdf)
- [Fast Fluid Dynamics Simulation on the GPU, Mark J. Harris](https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-38-fast-fluid-dynamics-simulation-gpu)
