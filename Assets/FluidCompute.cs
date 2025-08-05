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
}

} // namespace StableFluids
