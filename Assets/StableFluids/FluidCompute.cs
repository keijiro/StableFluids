using UnityEngine;

namespace StableFluids {

static class FluidCompute
{
    public static class Kernels
    {
        public const int Advect = 0;
        public const int Force = 1;
        public const int PSetup = 2;
        public const int PFinish = 3;
        public const int Jacobi1 = 4;
        public const int Jacobi2 = 5;
    }

    public static class PropertyIds
    {
        public static readonly int Time = Shader.PropertyToID("Time");
        public static readonly int DeltaTime = Shader.PropertyToID("DeltaTime");
        public static readonly int U_in = Shader.PropertyToID("U_in");
        public static readonly int W_out = Shader.PropertyToID("W_out");
        public static readonly int Alpha = Shader.PropertyToID("Alpha");
        public static readonly int Beta = Shader.PropertyToID("Beta");
        public static readonly int B2_in = Shader.PropertyToID("B2_in");
        public static readonly int X2_in = Shader.PropertyToID("X2_in");
        public static readonly int X2_out = Shader.PropertyToID("X2_out");
        public static readonly int ForceOrigin = Shader.PropertyToID("ForceOrigin");
        public static readonly int ForceExponent = Shader.PropertyToID("ForceExponent");
        public static readonly int ForceVector = Shader.PropertyToID("ForceVector");
        public static readonly int W_in = Shader.PropertyToID("W_in");
        public static readonly int DivW_out = Shader.PropertyToID("DivW_out");
        public static readonly int P_out = Shader.PropertyToID("P_out");
        public static readonly int B1_in = Shader.PropertyToID("B1_in");
        public static readonly int X1_in = Shader.PropertyToID("X1_in");
        public static readonly int X1_out = Shader.PropertyToID("X1_out");
        public static readonly int P_in = Shader.PropertyToID("P_in");
        public static readonly int U_out = Shader.PropertyToID("U_out");
    }
}

} // namespace StableFluids
