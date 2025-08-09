using UnityEngine;

namespace StableFluids.Marbling {

public sealed class MarblingController : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] public float Viscosity { get; set; } = 1e-6f;
    [field:SerializeField] public float PointForce { get; set; } = 300;
    [field:SerializeField] public float PointFalloff { get; set; } = 200;

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _targetTexture = null;
    [SerializeField] float _simulationScale = 0.5f;
    [SerializeField] Texture2D _initialImage = null;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Shader _kernelsShader = null;
    [SerializeField, HideInInspector] Shader _marblingShader = null;

    #endregion

    #region Private members

    FluidSimulation _simulation;
    MarblingInputHandler _input;
    Material _mateiral;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        Debug.Assert(_targetTexture != null, "Target texture is not set.");

        var w = Mathf.RoundToInt(_targetTexture.width * _simulationScale);
        var h = Mathf.RoundToInt(_targetTexture.height * _simulationScale);

        _simulation = new FluidSimulation(w, h, _kernelsShader);
        _input = new MarblingInputHandler(_targetTexture);
        _mateiral = new Material(_marblingShader);

        if (_initialImage != null) Graphics.Blit(_initialImage, _targetTexture);
    }

    void OnDestroy()
    {
        _simulation?.Dispose();
        Destroy(_mateiral);
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
        _simulation.PreStep();

        // Apply forces based on input
        if (_input.RightPressed)
        {
            var force = Random.insideUnitCircle * PointForce * 0.025f;
            _simulation.ApplyPointForce(_input.Position, force, PointFalloff);
        }
        else if (_input.LeftPressed)
        {
            var force = _input.Velocity * PointForce;
            _simulation.ApplyPointForce(_input.Position, force, PointFalloff);
        }

        // Simulation post-step (projection)
        _simulation.PostStep();
    }

    void StepVisualization()
    {
        var temp = RenderTexture.GetTemporary(_targetTexture.descriptor);

        // Dye injection with right-button input
        if (_input.RightPressed)
        {
            _mateiral.color = Color.HSVToRGB(Time.time % 1, 1, 1);
            _mateiral.SetVector("_Origin", _input.Position);
            _mateiral.SetFloat("_Falloff", PointFalloff);
            _mateiral.SetPass(0); // Pass 0: Color Injection
            Graphics.Blit(_targetTexture, temp, _mateiral);
        }
        else
        {
            Graphics.CopyTexture(_targetTexture, temp);
        }

        // Color advection
        _mateiral.SetTexture("_VelocityField", _simulation.VelocityField);
        _mateiral.SetPass(1); // Pass 1: Fluid Advection
        Graphics.Blit(temp, _targetTexture, _mateiral);

        RenderTexture.ReleaseTemporary(temp);
    }

    #endregion
}

} // namespace StableFluids.Marbling