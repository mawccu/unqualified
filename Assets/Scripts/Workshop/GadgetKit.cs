using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// The gadget you packed, at work. One key (remappable, G by default) uses it;
/// the HUD shows its charge, its state and when someone is close enough to
/// notice. It only helps where it was made for: elsewhere the key explains why
/// and uses nothing.
///
/// SteadyHand (wedding): tap to switch on. While you carry something wide it
/// damps the wobble and the lean and settles it sooner, drawing charge (more
/// the harder it works). Working hard it hums, which the coordinator can hear
/// and come to look at. Seen under your tray from a few metres away, after a
/// moment's grace that the HUD warns you about, it is evidence like any other:
/// a warning and an order to put it away; ignoring that costs more. Switching
/// it off ends it. At zero charge it fades out smoothly: no fling, no drop.
///
/// WhisperBox (call center): tap at your desk on a call for a hint (a fifth of
/// the battery). It chirps: the supervisor hears it if she is close or
/// listening in. It never chooses the fix.
/// </summary>
public class GadgetKit : MonoBehaviour
{
    public const float SteadyDrain = 1f / 150f;
    public const float SteadyEffortDrain = 1f / 45f;
    public const float NoticeRange = 5.5f;
    public const float GraceSeconds = 1.5f;
    const float HumNoiseRadius = 6f;
    const float IgnoreSeconds = 6f;
    public const float WhisperHearRange = 3.5f;
    const float WhisperWarnRange = 6f;

    public static GadgetKit Instance { get; private set; }
    public GadgetInstance Gadget { get; private set; }
    public RecipeDef Recipe { get; private set; }
    /// Made for the job this shift is.
    public bool Supported { get; private set; }
    /// SteadyHand switched on.
    public bool On { get; private set; }
    /// Working right now: on, charged, under something wide you are carrying.
    public bool Active { get; private set; }
    /// Seconds the coordinator has had a clear look at it (the grace before it counts).
    public float Watched { get; private set; }
    public int Sightings { get; private set; }
    public string LastHint { get; private set; } = "";

    GameSession _session;
    string _job;
    InputAction _key;
    AudioSource _hum, _sfx;
    GameObject _rig;
    Renderer _rigLight;
    PickupProp _rigOn;
    Incident _incident;
    float _warnedAt, _nextNoise, _hud;
    bool _ignoredRaised, _over;

    /// Puts the packed gadget (if any) to work in this shift.
    public static GadgetKit Attach(GameSession session, string job)
    {
        var gadget = WorkshopLedger.EquippedBy(CrewState.Current, GameSession.OfflineActor);
        if (session == null || gadget == null || PartCatalog.Recipe(gadget.recipe) == null)
            return null;
        var kit = session.gameObject.AddComponent<GadgetKit>();
        kit._session = session;
        kit._job = job;
        kit.Gadget = gadget;
        kit.Recipe = PartCatalog.Recipe(gadget.recipe);
        kit.Supported = kit.Recipe.Job == job;
        return kit;
    }

    void Awake() => Instance = this;

    void Start()
    {
        Bind();
        GameSettings.Changed += Bind;
        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
        _hum = _session.Player.HoldPoint.gameObject.AddComponent<AudioSource>();
        _hum.clip = ProceduralAudio.Hum;
        _hum.loop = true;
        _hum.volume = 0f;
        _hum.spatialBlend = 0.6f;
        _hum.playOnAwake = false;
        if (Recipe.Id == PartCatalog.SteadyHand)
            _hum.Play();
        _session.Hud.ShowToast(Supported
            ? $"You packed the {Recipe.Name}. {GameSettings.GadgetKeyName} uses it."
            : $"You packed the {Recipe.Name}. It won't help here: it's for {PartCatalog.JobLabel(Recipe.Job)}.");
    }

    void OnDestroy()
    {
        GameSettings.Changed -= Bind;
        _key?.Dispose();
        StopWorking();
        if (Instance == this)
            Instance = null;
    }

    void Bind()
    {
        _key?.Dispose();
        _key = new InputAction("gadget", InputActionType.Button, $"<Keyboard>/{GameSettings.GadgetKey}");
        _key.AddBinding("<Gamepad>/leftShoulder");
        _key.Enable();
    }

    /// The shift ended (payout or exposure): switch off, keep what charge is left.
    public void ShiftOver()
    {
        _over = true;
        On = false;
        StopWorking();
        _session.Hud.SetGadget(null, 0f, null);
    }

    void StopWorking()
    {
        Active = false;
        if (_rigOn != null)
            _rigOn.Stabilizer = 0f;
        if (_hum != null)
            _hum.volume = 0f;
        if (_rig != null)
            _rig.SetActive(false);
    }

    bool Typing
    {
        get
        {
            var focused = _session.Hud.Overlay.panel?.focusController?.focusedElement as VisualElement;
            return focused != null && (focused is TextField || focused.GetFirstAncestorOfType<TextField>() != null);
        }
    }

    void Update()
    {
        if (_over || _session == null)
            return;
        bool paused = _session.MenuOpen || _session.Travelling || _session.Hud.PayoutOpen;
        if (!paused && _key.WasPressedThisFrame() && !Typing)
            Press();
        if (Recipe.Id == PartCatalog.SteadyHand)
            UpdateSteadyHand(paused ? 0f : Time.deltaTime);
        _hud -= Time.unscaledDeltaTime;
        if (_hud <= 0f)
        {
            _hud = 0.1f;
            DrawHud();
        }
    }

    /// The gadget key, as the player pressed it (tests call this too).
    public void Press()
    {
        if (_over)
            return;
        if (!Supported)
        {
            _session.Hud.ShowToast($"The {Recipe.Name} does nothing here: it's for {PartCatalog.JobLabel(Recipe.Job)}. No charge used.");
            return;
        }
        if (Gadget.charge <= 0.001f)
        {
            _session.Hud.ShowToast($"The {Recipe.Name}'s battery is flat. It charges on the rack at home.");
            return;
        }
        if (Recipe.Id == PartCatalog.SteadyHand)
        {
            On = !On;
            _sfx.PlayOneShot(Tones.Click, 0.6f);
            _session.Hud.ShowToast(On ? "SteadyHand on: it works while you carry something wide." : "SteadyHand off.");
        }
        else if (Recipe.Id == PartCatalog.WhisperBox)
            Whisper();
    }

    // SteadyHand -------------------------------------------------------------------------

    CoordinatorMind Mind => ShiftEvents.Mind;

    bool ExposedNow => !_over && On && Active;

    Vector3 RigPosition => _rig != null && _rig.activeSelf ? _rig.transform.position : _session.Player.HoldPoint.position;

    void UpdateSteadyHand(float dt)
    {
        var held = _session.Player.HeldProp;
        bool wide = held != null && held.Category == PropCategory.Wide;
        if (_rigOn != null && _rigOn != held)
            _rigOn.Stabilizer = 0f;
        Active = On && wide && Gadget.charge > 0f;
        if (Active)
        {
            held.Stabilizer = 1f;
            Gadget.charge = Mathf.Max(0f, Gadget.charge - (SteadyDrain + held.Correction * SteadyEffortDrain) * dt);
            if (Gadget.charge <= 0f)
            {
                On = false;
                Active = false;
                _session.Hud.ShowToast("SteadyHand battery flat: back to your own two hands.");
            }
        }
        if (held != null && !Active)
            held.Stabilizer = 0f;
        ShowRig(Active ? held : null);

        // The hum: louder the harder it works. Hard work carries.
        float effort = Active ? held.Correction : 0f;
        _hum.volume = Mathf.MoveTowards(_hum.volume, effort * 0.55f, dt * 3f);
        _hum.pitch = 1f + effort * 0.4f;
        if (Active && effort > 0.35f && Time.time >= _nextNoise && Mind != null)
        {
            _nextNoise = Time.time + 3f;
            Mind.Suspicion.Noise(RigPosition, HumNoiseRadius);
        }

        // Being seen with it: a moment's grace, then it is evidence.
        if (Mind == null)
            return;
        Vector3 at = RigPosition;
        bool seen = Active && Vector3.Distance(Mind.Eye, at) <= NoticeRange && Mind.CanSee(at);
        Watched = seen ? Watched + dt : Mathf.Max(0f, Watched - dt);
        if (_incident != null && _incident.Resolved)
            _incident = null;
        if (seen && Watched >= GraceSeconds && _incident == null)
        {
            Sightings++;
            Gadget.timesSeen++;
            _incident = Mind.Suspicion.Report($"gadget_{Sightings}", "a homemade gadget under the tray", at, 12f + 9f * (Sightings - 1), false, () => ExposedNow);
            _warnedAt = Time.time;
            _ignoredRaised = false;
            _session.Hud.ShowSubtitle("Coordinator", Sightings switch
            {
                1 => "What is that thing under the tray? Switch it off and put it away. Now.",
                2 => "That thing again? I said put it away.",
                _ => "That's it. I'm writing this down.",
            }, 3.2f);
            Debug.Log($"[gadget] the coordinator saw the SteadyHand (sighting {Sightings})");
        }
        if (_incident != null && !_incident.Resolved && !_ignoredRaised && Time.time - _warnedAt > IgnoreSeconds && ExposedNow && Mind.CanSee(at))
        {
            _ignoredRaised = true;
            Mind.Suspicion.Raise($"gadget_ignored_{Sightings}", "kept using the gadget after being told to put it away", 15f);
            _session.Hud.ShowSubtitle("Coordinator", "I told you to put that away.", 2.4f);
        }
    }

    /// The device itself, clipped under what you carry, with a blinking light.
    void ShowRig(PickupProp under)
    {
        if (under == null)
        {
            if (_rig != null)
                _rig.SetActive(false);
            _rigOn = null;
            return;
        }
        if (_rig == null)
        {
            var art = WorkshopArt.Load();
            _rig = WorkshopArt.Show(art != null ? art.GadgetModel(PartCatalog.SteadyHand) : null, null, Vector3.zero, Quaternion.identity)
                ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            _rig.name = "SteadyHandRig";
            _rigLight = WorkshopArt.Light(_rig.transform, new Vector3(0f, 0.022f, 0.057f), new Vector3(0.012f, 0.008f, 0.006f));
        }
        _rigOn = under;
        _rig.SetActive(true);
        WorkshopArt.Tint(_rigLight, Mathf.Repeat(Time.time * 3f, 1f) > 0.5f ? new Color(0.3f, 1f, 0.4f) : new Color(0.05f, 0.2f, 0.08f));
    }

    /// The rig follows what it is clipped under (not parented: the prop's own meshes,
    /// its ghost preview and its scale stay its own).
    void LateUpdate()
    {
        if (_rig == null || !_rig.activeSelf || _rigOn == null)
            return;
        var t = _rigOn.transform;
        _rig.transform.SetPositionAndRotation(t.position - t.up * 0.032f, t.rotation);
    }

    // WhisperBox -------------------------------------------------------------------------

    void Whisper()
    {
        var shift = CallCenterShift.Instance;
        if (shift == null)
            return;
        var desk = shift.DeskOf(shift.PlayerId);
        if (desk == null || !desk.Occupied)
        {
            _session.Hud.ShowToast("Sit at your desk first. It listens to your headset. No charge used.");
            return;
        }
        var call = shift.CallOf(shift.PlayerId);
        if (call == null || !call.Live || call.State == CallState.Ringing)
        {
            _session.Hud.ShowToast("No call to whisper about. No charge used.");
            return;
        }
        if (!WorkshopLedger.Spend(Gadget, WorkshopLedger.WhisperCost))
        {
            _session.Hud.ShowToast("Not enough charge left for a hint.");
            return;
        }
        var hint = WhisperHints.For(call);
        LastHint = hint.Text;
        SupportConsole.Instance?.ShowWhisper(hint);
        AudioSource.PlayClipAtPoint(ProceduralAudio.Chirp, desk.transform.position + Vector3.up, 0.8f);
        // She hears it if she is close, or listening in on you.
        var boss = shift.Boss;
        Vector3 d = boss.transform.position - desk.transform.position;
        d.y = 0f;
        if (boss.ListeningTo == shift.PlayerId || d.magnitude <= WhisperHearRange)
        {
            Sightings++;
            Gadget.timesSeen++;
            boss.Suspicion.Raise($"whisperbox_{Sightings}", "a beeping gadget in your headset", 10f + 8f * (Sightings - 1));
            boss.Remark(Sightings switch
            {
                1 => "What was that beep? Is that thing in your ear? Take it out.",
                2 => "Again with the beeping. Last warning.",
                _ => "Right. That's going in your file.",
            });
            Debug.Log($"[gadget] the supervisor heard the WhisperBox (time {Sightings})");
        }
    }

    // HUD --------------------------------------------------------------------------------

    void DrawHud()
    {
        string key = $"<b>[{GameSettings.GadgetKeyName}]</b>";
        string state, warning = null;
        if (!Supported)
            state = "<color=#FF8A6B>useless here</color>";
        else if (Gadget.charge <= 0.001f)
            state = "<color=#FF8A6B>battery flat</color>";
        else if (Recipe.Id == PartCatalog.SteadyHand)
        {
            state = !On ? "OFF" : Active ? "<color=#8CE073>ON, working</color>" : "ON, waiting for a tray";
            if (Active && Watched > 0f)
                warning = $"She's looking at your tray! Switch it off ({GameSettings.GadgetKeyName})";
            else if (Active && Mind != null && Mind.Doing.StartsWith("about to check"))
                warning = "An inspection is coming: she'll see it up close";
            else if (Active && Mind != null && Vector3.Distance(Mind.Eye, RigPosition) <= NoticeRange + 2f)
                warning = "The coordinator is close";
        }
        else
        {
            int hints = Mathf.FloorToInt(Gadget.charge / WorkshopLedger.WhisperCost + 1e-3f);
            state = $"{hints} hint{(hints == 1 ? "" : "s")} left";
            var shift = CallCenterShift.Instance;
            var desk = shift != null ? shift.DeskOf(shift.PlayerId) : null;
            if (shift != null && desk != null)
            {
                Vector3 d = shift.Boss.transform.position - desk.transform.position;
                d.y = 0f;
                if (shift.Boss.ListeningTo == shift.PlayerId)
                    warning = "She's listening in: she'll hear the chirp";
                else if (shift.Boss.Doing.Contains("listen") || d.magnitude <= WhisperWarnRange)
                    warning = "Supervisor close: she'd hear the chirp";
            }
        }
        _session.Hud.SetGadget($"{key}  {Recipe.Name}   {state}", Gadget.charge, warning);
    }
}
