using UnityEngine;

/// <summary>
/// Something you can use while holding it (a party popper, a bell): click with
/// it in hand when not aiming at a place to put it.
/// </summary>
public abstract class HeldTool : MonoBehaviour
{
    /// The prompt line while held, e.g. "Pull the string".
    public abstract string UsePrompt { get; }
    public virtual bool CanUseHeld => true;
    public abstract void UseHeld(PlayerActor user);
}
