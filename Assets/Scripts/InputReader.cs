using System;
using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputActions;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReader")]
public class InputReader : ScriptableObject, IPlayerActions, IUIActions
{
    PlayerInputActions playerInputActions;

    public event Action<Vector2> mouseEvent;
    public event Action ClickEvent;

    private void OnEnable()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInputActions();
            playerInputActions.Player.SetCallbacks(this);
            playerInputActions.UI.SetCallbacks(this);
        }


        playerInputActions.Player.Enable();

    }

    private void OnDisable()
    {
        playerInputActions.Player.SetCallbacks(null);
        playerInputActions.Disable();
    }
    public void OnCancel(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        //playerInputActions.Player.Enable();
       // Debug.Log("in look");
        if (context.phase == InputActionPhase.Performed)
        {
            //Debug.Log("INteracted");
            mouseEvent?.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            ClickEvent?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnShowInv(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        Debug.Log("Scrolling");
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnUIInteraction(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }

    public void OnCloseInv(InputAction.CallbackContext context)
    {
        throw new NotImplementedException();
    }
}
