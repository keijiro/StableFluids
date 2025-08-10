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
    RenderTexture _targetTexture;
    bool _wasInputActive;

    #endregion

    #region Public methods

    public MarblingInputHandler(RenderTexture targetTexture)
      => _targetTexture = targetTexture;

    public void UpdateTargetTexture(RenderTexture targetTexture)
      => _targetTexture = targetTexture;

    public void Update()
    {
        // Check for actual input from either device
        var hasMouseInput = CheckMouseInput();
        var hasTouchInput = CheckTouchInput();
        
        // Prioritize touch over mouse if both are active
        if (hasTouchInput)
            ApplyTouchInput();
        else if (hasMouseInput)
            ApplyMouseInput();
        else
            UpdateNoInput();
    }

    #endregion

    #region Private methods

    bool CheckMouseInput()
    {
        var mouse = Mouse.current;
        return mouse != null && 
               (mouse.leftButton.isPressed || mouse.rightButton.isPressed);
    }
    
    void ApplyMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        var mousePos = mouse.position.ReadValue();
        var position = GetNormalizedInputPosition(mousePos);
        UpdateInputState
          (position, mouse.leftButton.isPressed, mouse.rightButton.isPressed);
    }

    bool CheckTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return false;
        
        for (var i = 0; i < touchscreen.touches.Count; i++)
            if (touchscreen.touches[i].isInProgress)
                return true;
        
        return false;
    }
    
    void ApplyTouchInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

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

        if (touchCount == 0) return;

        averagePosition /= touchCount;
        // 1 touch = left, 2+ touches = right
        UpdateInputState
          (GetNormalizedInputPosition(averagePosition),
           touchCount == 1, touchCount >= 2);
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
        
        var isInputActive = leftPressed || rightPressed;
        
        // Reset velocity on first frame of input
        if (isInputActive && !_wasInputActive)
            Velocity = Vector2.zero;
        else if (isInputActive)
            Velocity = Position - _previousInput;
        else
            Velocity = Vector2.zero;
        
        _previousInput = Position;
        _wasInputActive = isInputActive;
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

} // namespace StableFluids.Marbling