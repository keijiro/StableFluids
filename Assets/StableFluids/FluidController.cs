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

    [SerializeField, HideInInspector] Shader _kernelsShader = null;
    [SerializeField, HideInInspector] Shader _advectionShader = null;
    [SerializeField, HideInInspector] Shader _injectionShader = null;

    #endregion

    #region Private members

    FluidSimulation _simulation;
    FluidInputHandler _input;
    (Material advection, Material injection) _materials;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        Debug.Assert(_targetTexture != null, "Target texture is not set.");

        var w = Mathf.RoundToInt(_targetTexture.width * _simulationScale);
        var h = Mathf.RoundToInt(_targetTexture.height * _simulationScale);

        _simulation = new FluidSimulation(w, h, _kernelsShader);
        _input = new FluidInputHandler(_targetTexture);
        _materials.advection = new Material(_advectionShader);
        _materials.injection = new Material(_injectionShader);

        if (_initialImage != null) Graphics.Blit(_initialImage, _targetTexture);
    }

    void OnDestroy()
    {
        _simulation?.Dispose();
        Destroy(_materials.advection);
        Destroy(_materials.injection);
    }

    void Update()
    {
        if (_simulation == null) return;
        _input.Update();
        StepSimulation();
        StepVisualization();
    }

    #endregion

    #region Simulation step methods

    void StepSimulation()
    {
        // Simulation pre-step (advection + diffusion)
        _simulation.Viscosity = Viscosity;
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

        // Simulation post-step (projection)
        _simulation.PostStep(Time.deltaTime);
    }

    void StepVisualization()
    {
        var temp = RenderTexture.GetTemporary(_targetTexture.descriptor);

        // Dye injection with right-button input
        if (_input.RightPressed)
        {
            _materials.injection.color = Color.HSVToRGB(Time.time % 1, 1, 1);
            _materials.injection.SetVector("_Origin", _input.Position);
            _materials.injection.SetFloat("_Exponent", Exponent);
            Graphics.Blit(_targetTexture, temp, _materials.injection);
        }
        else
        {
            Graphics.CopyTexture(_targetTexture, temp);
        }

        // Color advection
        _materials.advection.SetTexture("_VelocityField", _simulation.VelocityField);
        Graphics.Blit(temp, _targetTexture, _materials.advection);

        RenderTexture.ReleaseTemporary(temp);
    }

    #endregion
}

} // namespace StableFluids
