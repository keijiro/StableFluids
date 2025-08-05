using UnityEngine;

namespace StableFluids {

public sealed class Fluid : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] float Viscosity { get; set; } = 1e-6f;
    [field:SerializeField] float Force { get; set; } = 300;
    [field:SerializeField] float Exponent { get; set; } = 200;

    #endregion

    #region Editable attributes

    [SerializeField] Vector2Int _resolution = new Vector2Int(512, 512);
    [SerializeField] Texture2D _initial;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] ComputeShader _compute;
    [SerializeField, HideInInspector] Shader _shader;

    #endregion

    #region Private members

    FluidSimulation _simulation;
    Material _material;
    Vector2 _previousInput;

    (RenderTexture rt1, RenderTexture rt2) _color;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _material = new Material(_shader);
        
        var resolution = new Vector2Int(
            _resolution.x,
            _resolution.y * Screen.height / Screen.width
        );
        
        _simulation = new FluidSimulation(_compute, resolution);
        _simulation.Viscosity = Viscosity;
        _simulation.Force = Force;
        _simulation.Exponent = Exponent;

        _color.rt1 = RTUtil.AllocateUavRgba(Screen.width, Screen.height);
        _color.rt2 = RTUtil.AllocateUavRgba(Screen.width, Screen.height);

        Graphics.Blit(_initial, _color.rt1);
    }

    void OnDestroy()
    {
        _simulation?.Dispose();
        
        Destroy(_material);
        Destroy(_color.rt1);
        Destroy(_color.rt2);
    }

    void Update()
    {
        _simulation.Viscosity = Viscosity;
        _simulation.Force = Force;
        _simulation.Exponent = Exponent;
        
        var input = new Vector2(
            (Input.mousePosition.x - Screen.width  * 0.5f) / Screen.height,
            (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height
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
        Graphics.Blit(_color.rt1, _color.rt2, _material, 0);

        _color = (_color.rt2, _color.rt1);

        _previousInput = input;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
      => Graphics.Blit(_color.rt1, destination, _material, 1);

    #endregion
}

} // namespace StableFluids
