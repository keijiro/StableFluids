using UnityEngine;

namespace StableFluids
{
    public class Fluid : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] Vector2Int _dimensions = new Vector2Int(256, 256);
        [SerializeField] float _viscosity = 0.01f;

        #endregion

        #region Internal resources

        [SerializeField, HideInInspector] ComputeShader _compute;
        [SerializeField, HideInInspector] Shader _shader;

        #endregion

        #region Private members

        Material _shaderSheet;

        static class Kernels
        {
            public const int Advect = 0;
            public const int Force = 1;
            public const int PSetup = 2;
            public const int PFinish = 3;
            public const int Jacobi1 = 4;
            public const int Jacobi2 = 5;
        }

        int ThreadCountX { get { return _dimensions.x / 8; } }
        int ThreadCountY { get { return _dimensions.y / 8; } }

        // Vector field buffers
        static class VFB
        {
            public static RenderTexture V1;
            public static RenderTexture V2;
            public static RenderTexture V3;
            public static RenderTexture P1;
            public static RenderTexture P2;
        }

        RenderTexture AllocateBuffer(int componentCount)
        {
            var format = RenderTextureFormat.ARGBHalf;
            if (componentCount == 1) format = RenderTextureFormat.RHalf;
            if (componentCount == 2) format = RenderTextureFormat.RGHalf;

            var rt = new RenderTexture(_dimensions.x, _dimensions.y, 0, format);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }
        RenderTexture AllocateBuffer2(int componentCount)
        {
            var format = RenderTextureFormat.ARGBHalf;
            if (componentCount == 1) format = RenderTextureFormat.RHalf;
            if (componentCount == 2) format = RenderTextureFormat.RGHalf;

            var rt = new RenderTexture(1920, 1080, 0, format);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        RenderTexture _colorRT1;
        RenderTexture _colorRT2;

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _dimensions = Vector2Int.Max(Vector2Int.one * 8, _dimensions);
        }

        void Start()
        {
            _shaderSheet = new Material(_shader);

            VFB.V1 = AllocateBuffer(2);
            VFB.V2 = AllocateBuffer(2);
            VFB.V3 = AllocateBuffer(2);
            VFB.P1 = AllocateBuffer(1);
            VFB.P2 = AllocateBuffer(1);

            _colorRT1 = AllocateBuffer2(4);
            _colorRT2 = AllocateBuffer2(4);
        }

        void OnDestroy()
        {
            Destroy(_shaderSheet);

            Destroy(VFB.V1);
            Destroy(VFB.V2);
            Destroy(VFB.V3);
            Destroy(VFB.P1);
            Destroy(VFB.P2);

            Destroy(_colorRT1);
            Destroy(_colorRT2);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var dx = 1.0f / _dimensions.y;

            _compute.SetFloat("Time", Time.time);
            _compute.SetFloat("DeltaTime", dt);

            // Advection
            _compute.SetTexture(Kernels.Advect, "U_in", VFB.V1);
            _compute.SetTexture(Kernels.Advect, "W_out", VFB.V2);
            _compute.Dispatch(Kernels.Advect, ThreadCountX, ThreadCountY, 1);

            // Diffuse setup
            var dif_alpha = dx * dx / (_viscosity * dt);
            _compute.SetFloat("Alpha", dif_alpha);
            _compute.SetFloat("Beta", 4 + dif_alpha);
            Graphics.CopyTexture(VFB.V2, VFB.V1);
            _compute.SetTexture(Kernels.Jacobi2, "B2_in", VFB.V1);

            // Jacobi iteration
            for (var i = 0; i < 20; i++)
            {
                _compute.SetTexture(Kernels.Jacobi2, "X2_in", VFB.V2);
                _compute.SetTexture(Kernels.Jacobi2, "X2_out", VFB.V3);
                _compute.Dispatch(Kernels.Jacobi2, ThreadCountX, ThreadCountY, 1);

                _compute.SetTexture(Kernels.Jacobi2, "X2_in", VFB.V3);
                _compute.SetTexture(Kernels.Jacobi2, "X2_out", VFB.V2);
                _compute.Dispatch(Kernels.Jacobi2, ThreadCountX, ThreadCountY, 1);
            }

            // Add external force
            _compute.SetTexture(Kernels.Force, "W_out", VFB.V2);
            _compute.Dispatch(Kernels.Force, ThreadCountX, ThreadCountY, 1);

            // Projection setup
            _compute.SetTexture(Kernels.PSetup, "W_in", VFB.V2);
            _compute.SetTexture(Kernels.PSetup, "DivW_out", VFB.V3);
            _compute.SetTexture(Kernels.PSetup, "P_out", VFB.P1);
            _compute.Dispatch(Kernels.PSetup, ThreadCountX, ThreadCountY, 1);

            // Jacobi iteration
            _compute.SetFloat("Alpha", -dx * dx);
            _compute.SetFloat("Beta", 4);
            _compute.SetTexture(Kernels.Jacobi1, "B1_in", VFB.V3);

            for (var i = 0; i < 20; i++)
            {
                _compute.SetTexture(Kernels.Jacobi1, "X1_in", VFB.P1);
                _compute.SetTexture(Kernels.Jacobi1, "X1_out", VFB.P2);
                _compute.Dispatch(Kernels.Jacobi1, ThreadCountX, ThreadCountY, 1);

                _compute.SetTexture(Kernels.Jacobi1, "X1_in", VFB.P2);
                _compute.SetTexture(Kernels.Jacobi1, "X1_out", VFB.P1);
                _compute.Dispatch(Kernels.Jacobi1, ThreadCountX, ThreadCountY, 1);
            }

            // Projection finish
            _compute.SetTexture(Kernels.PFinish, "W_in", VFB.V2);
            _compute.SetTexture(Kernels.PFinish, "P_in", VFB.P1);
            _compute.SetTexture(Kernels.PFinish, "U_out", VFB.V1);
            _compute.Dispatch(Kernels.PFinish, ThreadCountX, ThreadCountY, 1);

            // Apply the velocity field to the color map.
            _shaderSheet.SetTexture("_VelocityField", VFB.V1);
            Graphics.Blit(_colorRT1, _colorRT2, _shaderSheet, 0);

            var temp = _colorRT1;
            _colorRT1 = _colorRT2;
            _colorRT2 = temp;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(_colorRT1, destination, _shaderSheet, 1);
        }

        #endregion
    }
}
