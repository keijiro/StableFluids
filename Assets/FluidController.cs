using UnityEngine;

namespace StableFluids {

public sealed class FluidController : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] float Viscosity { get; set; } = 1e-6f;
    [field:SerializeField] float Force { get; set; } = 300;
    [field:SerializeField] float Exponent { get; set; } = 200;

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _targetTexture;
    [SerializeField] float _simulationScale = 0.5f;
    [SerializeField] Texture2D _initialImage;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] ComputeShader _compute;
    [SerializeField, HideInInspector] Shader _shader;

    #endregion

    #region Private members

    FluidSimulation _simulation;
    Material _material;
    Vector2 _previousInput;

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

        _simulation.Viscosity = Viscosity;
        _simulation.Force = Force;
        _simulation.Exponent = Exponent;
        
        var aspectRatio = (float)_targetTexture.width / _targetTexture.height;
        var input = new Vector2(
            (Input.mousePosition.x / Screen.width - 0.5f) * aspectRatio,
            Input.mousePosition.y / Screen.height - 0.5f
        );

        Vector2 forceVector;
        if (Input.GetMouseButton(1))
            forceVector = Random.insideUnitCircle * Force * 0.025f;
        else if (Input.GetMouseButton(0))
            forceVector = (input - _previousInput) * Force;
        else
            forceVector = Vector2.zero;

        _simulation.Step(Time.deltaTime, input, forceVector);

        var offs = Vector2.one * (Input.GetMouseButton(1) ? 0 : 1e+7f);
        _material.SetVector("_ForceOrigin", input + offs);
        _material.SetFloat("_ForceExponent", Exponent);
        _material.SetTexture("_VelocityField", _simulation.VelocityField);
        
        var temp = RenderTexture.GetTemporary(_targetTexture.descriptor);
        Graphics.Blit(_targetTexture, temp, _material, 0);
        Graphics.CopyTexture(temp, _targetTexture);
        RenderTexture.ReleaseTemporary(temp);

        _previousInput = input;
    }

    #endregion
}

} // namespace StableFluids
