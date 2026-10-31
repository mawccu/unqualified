using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The workshop corner of the flat as a whole: deliveries (the courier knocks,
/// the door opens, a parcel lands on the mat), parcels by the door for every
/// delivered order not yet unpacked, free charging on the rack, and the HUD card
/// while a build is going. Orders are matched by id, so a reload in the middle
/// of any of this neither loses nor duplicates anything.
/// </summary>
public class WorkshopRoom : MonoBehaviour
{
    const float ResumeDeliverySeconds = 4f;

    [SerializeField] FrontDoor door;
    [SerializeField] Transform parcelDrop;
    [SerializeField] Transform workshopArea;
    [SerializeField] Vector3 workshopSize = new(3f, 2.6f, 3.6f);
    [SerializeField] AudioSource doorAudio;

    public static WorkshopRoom Instance { get; private set; }

    static CrewState Crew => CrewState.Current;
    readonly Dictionary<string, float> _arrivals = new();
    readonly Dictionary<string, Parcel> _parcels = new();
    bool _delivering;
    float _saveTimer;
    bool _charging;

    public void Configure(FrontDoor frontDoor, Transform drop, Transform area, Vector3 areaSize, AudioSource knockAudio)
    {
        door = frontDoor;
        parcelDrop = drop;
        workshopArea = area;
        workshopSize = areaSize;
        doorAudio = knockAudio;
    }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        // Charge gained while at home is kept.
        if (_charging)
            Crew.Save();
    }

    void Start()
    {
        foreach (var order in Crew.orders)
        {
            if (order.state == PartOrder.Delivered)
                SpawnParcel(order);
            else if (order.state == PartOrder.Paid)
                _arrivals[order.id] = Time.time + ResumeDeliverySeconds;
        }
    }

    /// The shop took the money: the courier is on the way.
    public void OrderPlaced(PartOrder order) => _arrivals[order.id] = Time.time + PartCatalog.DeliverySeconds;

    bool QuietAtTheDoor
    {
        get
        {
            var director = OpeningDirector.Instance;
            return director == null || director.Stage == OpeningStage.FindJob || director.Stage == OpeningStage.Hired;
        }
    }

    void Update()
    {
        // The starter kit and anything the shop sent while we were out are on the way too.
        foreach (var order in Crew.orders)
            if (order.state == PartOrder.Paid && !_arrivals.ContainsKey(order.id))
                _arrivals[order.id] = Time.time + ResumeDeliverySeconds;
        if (!_delivering && QuietAtTheDoor)
        {
            foreach (var pair in _arrivals)
                if (Time.time >= pair.Value)
                {
                    StartCoroutine(Deliver(pair.Key));
                    break;
                }
        }

        // Free charging on the rack while anyone is home.
        _charging = Crew.gadgets.Any(g => g.charge < 1f);
        if (_charging)
        {
            WorkshopLedger.Charge(Crew, Time.deltaTime);
            _saveTimer += Time.deltaTime;
            if (_saveTimer > 5f || Crew.gadgets.All(g => g.charge >= 1f))
            {
                _saveTimer = 0f;
                Crew.Save();
            }
        }
    }

    /// The doorstep event: a knock, the door opens a crack, the parcel is pushed in.
    IEnumerator Deliver(string orderId)
    {
        _delivering = true;
        _arrivals.Remove(orderId);
        var order = Crew.orders.Find(o => o.id == orderId);
        if (order == null || order.state != PartOrder.Paid)
        {
            _delivering = false;
            yield break;
        }
        if (doorAudio != null)
            doorAudio.PlayOneShot(ProceduralAudio.Knock, 0.9f);
        yield return new WaitForSeconds(0.9f);
        door.Open();
        yield return new WaitForSeconds(0.6f);
        if (WorkshopLedger.Deliver(Crew, orderId))
        {
            Crew.Save();
            var parcel = SpawnParcel(order);
            parcel.GetComponent<AudioSource>()?.PlayOneShot(ProceduralAudio.SetDown, 0.9f);
            GameSession.Instance?.Hud.ShowToast(order.id == WorkshopLedger.StarterOrder
                ? "A box of salvaged parts from Dev, at the front door."
                : $"Delivered to the front door: order {order.id}.");
        }
        yield return new WaitForSeconds(0.8f);
        if (OpeningDirector.Instance == null || OpeningDirector.Instance.Stage != OpeningStage.Hired || !GameSession.Instance.Travelling)
            door.Close();
        _delivering = false;
    }

    Parcel SpawnParcel(PartOrder order)
    {
        if (_parcels.TryGetValue(order.id, out var existing) && existing != null)
            return existing;
        var art = WorkshopArt.Load();
        int n = _parcels.Count(p => p.Value != null && !string.IsNullOrEmpty(p.Value.OrderId));
        Vector3 at = parcelDrop.position + parcelDrop.right * (0.2f * (n % 3)) + Vector3.up * (0.19f * (n / 3));
        // The box is two looks on one body: sealed, and opened with its flaps up.
        string id = $"parcel_{order.id}";
        var root = new GameObject(id);
        root.SetActive(false);
        root.transform.SetPositionAndRotation(at, parcelDrop.rotation * Quaternion.Euler(0f, n * 17f, 0f));
        var sealedLook = art != null && art.parcel != null ? Instantiate(art.parcel, root.transform) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        sealedLook.transform.SetParent(root.transform, false);
        var openLook = art != null && art.parcelOpen != null ? Instantiate(art.parcelOpen, root.transform) : null;
        openLook?.SetActive(false);
        var parcel = root.AddComponent<Parcel>();
        parcel.Configure(order.id, sealedLook, openLook);
        PartPiece.Finish(root, sealedLook, id, "", "parcel",
            order.id == WorkshopLedger.StarterOrder ? "Dev's box of parts" : $"a parcel ({order.id})", 1.2f, PropCategory.Wide);
        _parcels[order.id] = parcel;
        return parcel;
    }

    /// Tearing a parcel open: its parts go into storage, once.
    public void Unpack(Parcel parcel, PlayerActor by)
    {
        var order = Crew.orders.Find(o => o.id == parcel.OrderId);
        if (order != null && WorkshopLedger.OpenParcel(Crew, order.id))
        {
            Crew.Save();
            parcel.GetComponent<AudioSource>()?.PlayOneShot(ProceduralAudio.Tear, 0.9f);
            string what = string.Join(", ", order.lines.Select(l => $"{l.count} × {PartCatalog.Part(l.part).Name.ToLowerInvariant()}"));
            GameSession.Instance?.Hud.ShowToast($"Into the parts bins: {what}.");
            PartShelf.Instance?.Refresh();
        }
        parcel.Emptied();
    }

    // The HUD card while building -------------------------------------------------------

    /// True while there is a build and the player is in the workshop corner.
    public bool WantsCard(PlayerActor player)
    {
        var bench = AssemblyBench.Instance;
        if (bench == null || !bench.HasBuild || player == null || workshopArea == null)
            return false;
        Vector3 local = workshopArea.InverseTransformPoint(player.transform.position);
        return Mathf.Abs(local.x) <= workshopSize.x * 0.5f && Mathf.Abs(local.z) <= workshopSize.z * 0.5f;
    }

    public void FillCard(Hud hud)
    {
        var bench = AssemblyBench.Instance;
        var lines = bench.Steps().Select(s => (s.done ? "<color=#8CE073>✓</color>  " : "<color=#FFFFFF66>○</color>  ") + s.text).ToArray();
        hud.SetCard("WORKBENCH", $"Building: {bench.Recipe.Name}", lines);
    }
}
