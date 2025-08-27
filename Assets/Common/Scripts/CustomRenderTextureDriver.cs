using UnityEngine;

public sealed class CustomRenderTextureDriver : MonoBehaviour
{
    [SerializeField] CustomRenderTexture _target = null;

    void Start()
      => _target.Initialize();

    void Update()
      => _target.Update();
}
