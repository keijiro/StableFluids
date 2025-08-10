using UnityEngine;
using UnityEngine.InputSystem;

namespace StableFluids.Marbling {

public sealed class MarblingInputHandler
{
    #region Public properties

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public bool LeftPressed { get; private set; }
    public bool RightPressed { get; private set; }

    #endregion

    #region Private members

    Vector2 _previousInput;
    float _targetAspectRatio;
    bool _wasInputActive;

    #endregion

    #region Public methods

    public MarblingInputHandler(RenderTexture targetTexture)
      => _targetAspectRatio = (float)targetTexture.width / targetTexture.height;

    public void Update()
    {
        // Get input from touch or mouse (touch has priority)
        var (hasInput, position, left, right) = GetActiveInput();

        if (hasInput)
            UpdateInputState(position, left, right);
        else
            UpdateInputState(Vector2.zero, false, false);
    }

    #endregion

    #region Private methods

    (bool hasInput, Vector2 position, bool left, bool right) GetActiveInput()
    {
        // Prioritize touch input for WebGL compatibility
        var touchInput = GetTouchInput();
        if (touchInput.hasInput) return touchInput;

        var mouseInput = GetMouseInput();
        if (mouseInput.hasInput) return mouseInput;

        return (false, Vector2.zero, false, false);
    }

    (bool hasInput, Vector2 position, bool left, bool right) GetMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return (false, Vector2.zero, false, false);

        var left = mouse.leftButton.isPressed;
        var right = mouse.rightButton.isPressed;

        if (!left && !right) return (false, Vector2.zero, false, false);

        var position = GetNormalizedInputPosition(mouse.position.ReadValue());
        return (true, position, left, right);
    }

    (bool hasInput, Vector2 position, bool left, bool right) GetTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return (false, Vector2.zero, false, false);

        // Calculate average position of all active touches
        var activeTouches = 0;
        var sumPosition = Vector2.zero;

        foreach (var touch in touchscreen.touches)
        {
            if (!touch.isInProgress) continue;
            sumPosition += touch.position.ReadValue();
            activeTouches++;
        }

        if (activeTouches == 0) return (false, Vector2.zero, false, false);

        // Map touch count to button states: 1 touch = left, 2+ touches = right
        var position = GetNormalizedInputPosition(sumPosition / activeTouches);
        return (true, position, activeTouches == 1, activeTouches >= 2);
    }

    void UpdateInputState(Vector2 position, bool leftPressed, bool rightPressed)
    {
        Position = position;
        LeftPressed = leftPressed;
        RightPressed = rightPressed;

        // Reset velocity on first frame of input to avoid large initial jumps
        var isActive = leftPressed || rightPressed;
        Velocity = isActive && _wasInputActive ? position - _previousInput : Vector2.zero;

        _previousInput = position;
        _wasInputActive = isActive;
    }

    Vector2 GetNormalizedInputPosition(Vector2 screenPos)
    {
        // Convert screen position to normalized coordinates with aspect ratio correction
        var screenAspect = (float)Screen.width / Screen.height;
        var offset = screenPos - new Vector2(Screen.width, Screen.height) / 2;
        var width = Screen.height * Mathf.Min(_targetAspectRatio, screenAspect);
        return offset / width;
    }

    #endregion
}

} // namespace StableFluids.Marbling