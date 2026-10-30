using UnityEngine;

/// <summary>
/// A mug. Fill it at the coffee machine; a full mug that hits the floor spills
/// (a mess, and a noise). An empty one just clunks.
/// </summary>
[RequireComponent(typeof(PickupProp))]
public class Mug : MonoBehaviour
{
    public bool Full { get; private set; }
    PickupProp _prop;

    void Awake()
    {
        _prop = GetComponent<PickupProp>();
        _prop.HitHard += (p, speed) =>
        {
            if (!Full || speed < 1.6f)
                return;
            Full = false;
            AudioSource.PlayClipAtPoint(Tones.Splash, transform.position, 0.8f);
            ShiftEvents.Noise(transform.position, 10f, "a mug smashing");
            ShiftEvents.Mess(transform.position, "spilled coffee", 8f, false);
        };
    }

    public void Fill() => Full = true;
    public void Empty() => Full = false;
}
