using UnityEngine;

namespace StableFluids.Marbling {

public sealed class MarblingController : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] public float PointForce { get; set; } = 300;
    [field:SerializeField] public float PointFalloff { get; set; } = 200;

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _colorInjection = null;
    [SerializeField] RenderTexture _forceField = null;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Shader _shader = null;

    #endregion

    #region Private members

    MarblingInputHandler _input;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _input = new MarblingInputHandler(_colorInjection);

        _material = new Material(_shader);
        _material.SetFloat("_Aspect", (float)_forceField.width / _forceField.height);

        Graphics.Blit(Texture2D.blackTexture, _colorInjection);
        Graphics.Blit(Texture2D.blackTexture, _forceField);
    }

    void OnDestroy()
      => Destroy(_material);

    void Update()
    {
        _input.Update();
        UpdateColorInjection();
        UpdateForceField();
    }

    #endregion

    #region Update methods

    void UpdateColorInjection()
    {
        if (_input.RightPressed)
        {
            _material.color = Color.HSVToRGB(Time.time % 1, 1, 1);
            _material.SetVector("_Origin", _input.Position);
            _material.SetFloat("_Falloff", PointFalloff);
            Graphics.Blit(null, _colorInjection, _material, 0);
        }
        else
        {
            Graphics.Blit(Texture2D.blackTexture, _colorInjection);
        }
    }

    void UpdateForceField()
    {
        if (_input.RightPressed)
        {
            BlitToForceField(Random.insideUnitCircle * PointForce * 0.025f);
        }
        else if (_input.LeftPressed)
        {
            BlitToForceField(_input.Velocity * PointForce);
        }
        else
        {
            Graphics.Blit(Texture2D.blackTexture, _forceField);
        }
    }

    void BlitToForceField(Vector2 force)
    {
        _material.SetVector("_Origin", _input.Position);
        _material.SetFloat("_Falloff", PointFalloff);
        _material.SetVector("_Force", force);
        Graphics.Blit(null, _forceField, _material, 1);
    }

    #endregion
}

} // namespace StableFluids.Marbling