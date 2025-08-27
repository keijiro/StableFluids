using UnityEngine;

public sealed class FrameRateConfig : MonoBehaviour
{
    void StartIO()
      => Application.targetFrameRate = 60;
}
