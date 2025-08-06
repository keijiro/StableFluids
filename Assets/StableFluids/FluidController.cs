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
    Material _material;
    FluidInputHandler _input;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        Debug.Assert(_targetTexture != null, "Target texture is not set.");

        _material = new Material(_shader);

        if (_targetTexture == null) return;

        _simulation?.Dispose();

        var w = Mathf.RoundToInt(_targetTexture.width * _simulationScale);
        var h = Mathf.RoundToInt(_targetTexture.height * _simulationScale);

        _simulation = new FluidSimulation(_compute, w, h);
        _simulation.Viscosity = Viscosity;
        _simulation.Force = Force;
        _simulation.Exponent = Exponent;

        _input = new FluidInputHandler(_targetTexture);

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
        _simulation.Force = Force;
        _simulation.Exponent = Exponent;

        _input.Update();

        Vector2 forceVector;
        if (_input.RightPressed)
            forceVector = Random.insideUnitCircle * Force * 0.025f;
        else if (_input.LeftPressed)
            forceVector = _input.Velocity * Force;
        else
            forceVector = Vector2.zero;

        _simulation.Step(Time.deltaTime, _input.Position, forceVector);

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
