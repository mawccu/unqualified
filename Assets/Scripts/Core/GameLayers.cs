using UnityEngine;

/// <summary>
/// Physics layers the game relies on. Their names are set in the project's tag
/// manager by the venue builder; the numbers here must match.
/// </summary>
public static class GameLayers
{
    public const int World = 0;
    public const int Prop = 8;
    public const int Socket = 9;
    public const int Player = 10;

    /// What the crosshair can land on: the room, props and sockets (never players).
    public const int AimMask = (1 << World) | (1 << Prop) | (1 << Socket);

    public static void SetRecursively(GameObject go, int layer)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
