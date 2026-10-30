using System.Linq;
using UnityEngine;

/// <summary>
/// The shared reference shelf: the same help pages as the console, in binders,
/// for anyone who wants to read them away from a screen. Walk away to put them back.
/// </summary>
public class ReferenceShelf : Interactable
{
    PlayerActor _reader;

    public override string Prompt(IInteractionActor actor) => _reader != null ? "Put the binders back" : "Read the reference binders";
    public override InteractionResult CanUse(IInteractionActor actor) => InteractionResult.Ok;

    public override void Use(IInteractionActor actor)
    {
        var hud = GameSession.Instance.Hud;
        if (_reader != null)
        {
            _reader = null;
            hud.ShowDocument(null);
            return;
        }
        _reader = actor as PlayerActor;
        hud.ShowDocument("<size=13><color=#888888>BRIGHTLINE · AGENT REFERENCE</color></size>\n" +
            string.Join("\n\n", CaseLibrary.HelpPages.Select(p => $"<b>{p.title}</b>\n<size=13>{p.body}</size>")));
    }

    void Update()
    {
        if (_reader != null && DistanceFrom(_reader.transform.position) > 2.2f)
        {
            _reader = null;
            GameSession.Instance.Hud.ShowDocument(null);
        }
    }
}
