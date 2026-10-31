using UnityEngine;

/// <summary>
/// A delivered order waiting by the front door. Pick it up and click to tear it
/// open: the order's parts go into the crew's storage (once, by the order id)
/// and what is left is an empty box to kick about. Parcels are not saved as
/// objects: each delivered, unopened order puts one back by the door.
/// </summary>
public class Parcel : HeldTool
{
    [SerializeField] string orderId = "";
    [SerializeField] GameObject sealedModel;
    [SerializeField] GameObject openModel;

    public string OrderId => orderId;

    public void Configure(string order, GameObject closed, GameObject open)
    {
        orderId = order;
        sealedModel = closed;
        openModel = open;
    }

    public override string UsePrompt => "Tear it open";

    public override bool CanUseHeld => !string.IsNullOrEmpty(orderId);

    public override void UseHeld(PlayerActor user) => WorkshopRoom.Instance?.Unpack(this, user);

    /// The parts are out: just a box now.
    public void Emptied()
    {
        orderId = "";
        if (sealedModel != null)
            sealedModel.SetActive(false);
        if (openModel != null)
            openModel.SetActive(true);
        GetComponent<PickupProp>().Configure(name, "clutter", "an empty box", PropCategory.Wide);
    }
}
