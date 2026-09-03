using UnityEngine;

// The inventory canvas survives scene changes, so resolve the current room on click.
public class RoomNavigationButton : MonoBehaviour
{
    public void TurnLeft()
    {
        if (!WorldInteractionGate.IsBlocked) FindAnyObjectByType<NextPosition>()?.TurnLeft();
    }

    public void TurnRight()
    {
        if (!WorldInteractionGate.IsBlocked) FindAnyObjectByType<NextPosition>()?.TurnRight();
    }
}
