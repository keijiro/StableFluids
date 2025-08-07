using UnityEngine;

public sealed class AppConfig : MonoBehaviour
{
    void StartIO()
      => Application.targetFrameRate = 60;
}
