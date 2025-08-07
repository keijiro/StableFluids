using UnityEngine;
using UnityEngine.InputSystem;

namespace StableFluids {

public sealed class FluidInputHandler
{
    #region Public properties

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }

    #endregion

    #region Private members

    Vector2 _previousInput;
    RenderTexture _targetTexture;

    #endregion

    #region Public methods

    public FluidInputHandler(RenderTexture targetTexture)
      => _targetTexture = targetTexture;

    public void UpdateTargetTexture(RenderTexture targetTexture)
      => _targetTexture = targetTexture;

    public void Update()
    {
        // Try mouse input first, then touch, fallback to no input
        if (TryUpdateMouseInput()) return;
        if (TryUpdateTouchInput()) return;
        UpdateNoInput();
    }

    #endregion

    #region Private methods

    bool TryUpdateMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;

        var mousePos = mouse.position.ReadValue();
        var position = GetNormalizedInputPosition(mousePos);
        UpdateInputState
          (position, mouse.leftButton.isPressed, mouse.rightButton.isPressed);
        return true;
    }

    bool TryUpdateTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return false;

        var touchCount = 0;
        var averagePosition = Vector2.zero;

        // Calculate average position of all active touches
        for (var i = 0; i < touchscreen.touches.Count; i++)
        {
            var touch = touchscreen.touches[i];
            if (touch.isInProgress)
            {
                averagePosition += touch.position.ReadValue();
                touchCount++;
            }
        }

        if (touchCount == 0) return false;

        averagePosition /= touchCount;
        // 1 touch = left, 2+ touches = right
        UpdateInputState
          (GetNormalizedInputPosition(averagePosition),
           touchCount == 1, touchCount >= 2);
        return true;
    }

    // Fallback when no input devices are available
    void UpdateNoInput()
      => UpdateInputState(Vector2.zero, false, false);

    // Update all input properties and calculate velocity
    void UpdateInputState(Vector2 position, bool leftPressed, bool rightPressed)
    {
        Position = position;
        LeftPressed = leftPressed;
        RightPressed = rightPressed;
        Velocity = Position - _previousInput;
        _previousInput = Position;
    }

    // Convert screen position to normalized coordinates with aspect ratio correction
    Vector2 GetNormalizedInputPosition(Vector2 screenPos)
    {
        var screenAspect = (float)Screen.width / Screen.height;
        var textureAspect = (float)_targetTexture.width / _targetTexture.height;
        var input = screenPos - new Vector2(Screen.width, Screen.height) / 2;
        var width = Screen.height * Mathf.Min(textureAspect, screenAspect);
        return input / width;
    }

    #endregion
}

} // namespace StableFluids