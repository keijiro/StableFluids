using UnityEngine;
using UnityEngine.InputSystem;

namespace StableFluids {

public sealed class FluidController : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] float Viscosity { get; set; } = 1e-6f;
    [field:SerializeField] float Force { get; set; } = 300;
    [field:SerializeField] float Exponent { get; set; } = 200;

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _targetTexture = null;
    [SerializeField] float _simulationScale = 0.5f;
    [SerializeField] Texture2D _initialImage = null;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] ComputeShader _compute = null;
    [SerializeField, HideInInspector] Shader _shader = null;

    #endregion

    #region Private members

    FluidSimulation _simulation;
    FluidInputHandler _input;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        Debug.Assert(_targetTexture != null, "Target texture is not set.");

        var w = Mathf.RoundToInt(_targetTexture.width * _simulationScale);
        var h = Mathf.RoundToInt(_targetTexture.height * _simulationScale);

        _simulation = new FluidSimulation(_compute, w, h);
        _input = new FluidInputHandler(_targetTexture);
        _material = new Material(_shader);

        if (_initialImage != null) Graphics.Blit(_initialImage, _targetTexture);
    }

    void OnDestroy()
    {
        _simulation?.Dispose();
        Destroy(_material);
    }

    void Update()
    {
        if (_simulation == null) return;

        // Simulation parameters
        _simulation.Viscosity = Viscosity;

        _input.Update();

        // Run simulation pre-step (advection + diffusion)
        _simulation.PreStep(Time.deltaTime);

        // Apply forces based on input
        if (_input.RightPressed)
        {
            var randomForce = Random.insideUnitCircle * Force * 0.025f;
            _simulation.ApplyPointForce(_input.Position, randomForce, Exponent);
        }
        else if (_input.LeftPressed)
        {
            var dragForce = _input.Velocity * Force;
            _simulation.ApplyPointForce(_input.Position, dragForce, Exponent);
        }

        // Run simulation post-step (projection)
        _simulation.PostStep(Time.deltaTime);

        var offs = Vector2.one * (_input.RightPressed ? 0 : 1e+7f);
        _material.SetVector("_ForceOrigin", _input.Position + offs);
        _material.SetFloat("_ForceExponent", Exponent);
        _material.SetTexture("_VelocityField", _simulation.VelocityField);

        var temp = RenderTexture.GetTemporary(_targetTexture.descriptor);
        Graphics.Blit(_targetTexture, temp, _material);
        Graphics.CopyTexture(temp, _targetTexture);
        RenderTexture.ReleaseTemporary(temp);
    }

    #endregion
}

} // namespace StableFluids
