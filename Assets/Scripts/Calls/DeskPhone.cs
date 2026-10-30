using System;
using UnityEngine;

/// <summary>
/// The phone on a support desk. Its lamp shows the call state so nobody has to
/// guess (red flashing: ringing; green: on a call; amber pulsing: on hold; green
/// and white: muted; blue flashing: transferring). It rings out loud, murmurs
/// while the customer talks, and its handset goes to whoever picks it up.
/// </summary>
public class DeskPhone : MonoBehaviour
{
    public enum Lamp { Off, Ringing, Active, Hold, Muted, Transfer }

    static readonly Color Red = new(1f, 0.12f, 0.08f), Green = new(0.2f, 1f, 0.35f), Amber = new(1f, 0.62f, 0.1f), Blue = new(0.25f, 0.55f, 1f);

    [SerializeField] Renderer lamp;
    [SerializeField] Transform handset;
    [SerializeField] Transform holdButton, muteButton, transferButton;

    public Lamp State { get; private set; }
    public Transform Handset => handset;
    public Transform HoldButton => holdButton;
    public Transform MuteButton => muteButton;
    public Transform TransferButton => transferButton;
    public bool Lifted => _holder != null;

    AudioSource _ring, _line;
    Material _lamp;
    Vector3 _cradlePos;
    Quaternion _cradleRot;
    Func<Pose> _holder;
    float _lift, _transferUntil, _talkUntil;
    readonly float[] _press = new float[3];
    Vector3[] _buttonRest;

    public void Configure(Renderer lampRenderer, Transform handsetTransform, Transform hold, Transform mute, Transform transfer)
    {
        lamp = lampRenderer;
        handset = handsetTransform;
        holdButton = hold;
        muteButton = mute;
        transferButton = transfer;
    }

    void Awake()
    {
        _ring = gameObject.AddComponent<AudioSource>();
        _ring.clip = Tones.Ring;
        _ring.loop = true;
        _ring.spatialBlend = 1f;
        _ring.minDistance = 1.2f;
        _ring.maxDistance = 18f;
        _ring.playOnAwake = false;
        _line = gameObject.AddComponent<AudioSource>();
        _line.clip = Tones.Murmur;
        _line.loop = true;
        _line.spatialBlend = 1f;
        _line.minDistance = 0.3f;
        _line.maxDistance = 3f;
        _line.volume = 0.5f;
        _line.playOnAwake = false;
        if (lamp != null)
            _lamp = lamp.material;
        if (handset != null)
        {
            _cradlePos = handset.localPosition;
            _cradleRot = handset.localRotation;
        }
        _buttonRest = new[] { Rest(holdButton), Rest(muteButton), Rest(transferButton) };
        SetLamp(Lamp.Off);
    }

    static Vector3 Rest(Transform t) => t != null ? t.localPosition : Vector3.zero;

    /// Shows a call's state (null: no call).
    public void Show(CallCase call)
    {
        Lamp next = Lamp.Off;
        if (Time.time < _transferUntil)
            next = Lamp.Transfer;
        else if (call != null)
            next = call.State switch
            {
                CallState.Ringing => Lamp.Ringing,
                CallState.OnHold => Lamp.Hold,
                CallState.Active => call.Muted ? Lamp.Muted : Lamp.Active,
                _ => Lamp.Off,
            };
        if (next != State)
            SetLamp(next);
    }

    /// Flash blue for a moment: a call is being passed on from this desk.
    public void FlashTransfer()
    {
        _transferUntil = Time.time + 1.6f;
        SetLamp(Lamp.Transfer);
    }

    /// The customer is talking: a murmur from the earpiece for a moment.
    public void Murmur(float seconds) => _talkUntil = Time.time + seconds;

    void SetLamp(Lamp state)
    {
        State = state;
        if (state == Lamp.Ringing)
        {
            if (!_ring.isPlaying)
                _ring.Play();
        }
        else
            _ring.Stop();
    }

    /// Someone picks the handset up; it follows the pose they hold it at.
    public void PickUp(Func<Pose> holder) => _holder = holder;

    /// Back in the cradle.
    public void HangUp() => _holder = null;

    public void Press(int button)
    {
        _press[button] = 1f;
        _ring.PlayOneShot(Tones.Click, 0.7f);
    }

    public Transform Button(int index) => index switch { 0 => holdButton, 1 => muteButton, _ => transferButton };

    void Update()
    {
        if (_lamp != null)
        {
            float t = Time.time;
            (Color colour, float level) = State switch
            {
                Lamp.Ringing => (Red, Mathf.Repeat(t, 0.5f) < 0.25f ? 1f : 0.05f),
                Lamp.Active => (Green, 0.8f),
                Lamp.Muted => (Color.Lerp(Green, Color.white, 0.6f), Mathf.Repeat(t, 1.2f) < 0.9f ? 0.8f : 0.2f),
                Lamp.Hold => (Amber, 0.25f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 4f))),
                Lamp.Transfer => (Blue, Mathf.Repeat(t, 0.3f) < 0.15f ? 1f : 0.1f),
                _ => (Color.black, 0f),
            };
            _lamp.SetColor("_BaseColor", Color.Lerp(new Color(0.15f, 0.15f, 0.15f), colour, level));
            _lamp.SetColor("_EmissionColor", colour * level * 4f);
        }

        bool talking = Time.time < _talkUntil && Lifted;
        if (talking && !_line.isPlaying)
            _line.Play();
        else if (!talking && _line.isPlaying)
            _line.Stop();

        if (handset != null)
        {
            _lift = Mathf.MoveTowards(_lift, Lifted ? 1f : 0f, Time.deltaTime * 3.5f);
            var parent = handset.parent;
            var cradle = new Pose(parent.TransformPoint(_cradlePos), parent.rotation * _cradleRot);
            var held = _holder != null ? _holder() : cradle;
            float k = Mathf.SmoothStep(0f, 1f, _lift);
            // Up and over the desk, not through it.
            var arc = Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.12f);
            handset.SetPositionAndRotation(Vector3.Lerp(cradle.position, held.position, k) + arc, Quaternion.Slerp(cradle.rotation, held.rotation, k));
        }

        for (int i = 0; i < 3; i++)
        {
            _press[i] = Mathf.MoveTowards(_press[i], 0f, Time.deltaTime * 6f);
            var b = Button(i);
            if (b != null)
                b.localPosition = _buttonRest[i] + Vector3.down * (0.006f * _press[i]);
        }
    }
}
