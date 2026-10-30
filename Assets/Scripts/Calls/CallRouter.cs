using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Someone who can take calls: the local player, a teammate, or a simulated coworker.</summary>
public sealed class CallWorker
{
    public string Id;
    public string Name;
    /// Language ids this worker claimed (English is always included).
    public readonly HashSet<string> Languages = new() { CallLanguage.English };
    public bool Technical;
    public bool Online = true;
    public bool OnBreak;
    /// Run by the game, not a person (an NPC coworker). Nothing here is networked.
    public bool Simulated;
    public float LastAssignedAt = float.NegativeInfinity;
    public int Order;

    public bool Speaks(string language) => Languages.Contains(language);
}

public enum TransferResult { Ok, Queued, NotYours, NotLive, UnknownWorker, Offline, SameWorker, Bouncing, TooMany }

/// <summary>
/// Decides who gets each call. A call goes to someone who claimed its language
/// (and, for a technical variant, technical expertise if anyone did); among
/// those, the one who has waited longest since their last call, so work is
/// shared fairly. If all of them are busy, the call queues for them. If nobody
/// on shift claimed the language, anyone may take it and the console offers a
/// phrase aid. Transfers keep the whole call (notes, facts, attempts, patience)
/// and are refused when they would bounce a customer back and forth. A worker
/// who disconnects puts their calls back in the queue.
///
/// Pure logic: the shift ticks it, tests drive it directly.
/// </summary>
public sealed class CallRouter
{
    public const float RingTimeout = 14f;
    public const float BounceWindow = 30f;
    public const int MaxTransfers = 3;
    public const float TransferCost = 0.06f;

    // Patience lost per second in each state.
    public const float DrainWaiting = 0.006f, DrainHold = 0.01f, DrainActive = 0.0025f;

    readonly List<CallWorker> _workers = new();
    readonly List<CallCase> _calls = new();
    int _nextCall;

    public float Now { get; private set; }
    public IReadOnlyList<CallWorker> Workers => _workers;
    public IReadOnlyList<CallCase> Calls => _calls;

    /// A call now rings at this worker's desk.
    public event Action<CallCase, CallWorker> Assigned;
    public event Action<CallCase, TransferRecord> Transferred;
    public event Action<CallCase> Ended;

    public CallWorker AddWorker(string id, string name, IEnumerable<string> languages, bool technical, bool simulated)
    {
        var w = new CallWorker { Id = id, Name = name, Technical = technical, Simulated = simulated, Order = _workers.Count };
        if (languages != null)
            foreach (var l in languages)
                w.Languages.Add(l);
        _workers.Add(w);
        return w;
    }

    public CallWorker Worker(string id) => _workers.FirstOrDefault(w => w.Id == id);
    public CallCase Call(int id) => _calls.FirstOrDefault(c => c.Id == id);

    /// The live call this worker has (ringing, active or on hold), if any.
    public CallCase CallOf(string workerId) => _calls.FirstOrDefault(c => c.Live && c.Owner == workerId);

    public bool IsFree(CallWorker w) => w.Online && !w.OnBreak && CallOf(w.Id) == null;

    public IEnumerable<CallCase> Waiting => _calls.Where(c => c.Live && c.Owner == "").OrderBy(c => c.RangAt);

    /// Somebody new calls in.
    public CallCase Ring(CaseDefinition def, string language, bool callback = false)
    {
        var call = new CallCase(++_nextCall, def, language, Now) { State = CallState.Queued, Callback = callback };
        _calls.Add(call);
        Route();
        return call;
    }

    /// Who could take this call, best first (ignoring who is busy).
    public List<CallWorker> Eligible(CallCase call, out bool phraseAid)
    {
        var online = _workers.Where(w => w.Online).ToList();
        var speakers = online.Where(w => w.Speaks(call.Language)).ToList();
        phraseAid = speakers.Count == 0;
        var pool = phraseAid ? online : speakers;
        if (call.Def.Technical)
        {
            var experts = pool.Where(w => w.Technical).ToList();
            if (experts.Count > 0)
                pool = experts;
        }
        return pool;
    }

    /// Hands waiting calls to free workers, oldest call first, fairest worker first.
    public void Route()
    {
        foreach (var call in Waiting.ToList())
        {
            CallWorker pick = null;
            if (call.ReservedFor != "")
            {
                var reserved = Worker(call.ReservedFor);
                if (reserved == null || !reserved.Online)
                    call.ReservedFor = "";
                else if (IsFree(reserved))
                    pick = reserved;
                else
                    continue;
            }
            if (pick == null)
            {
                var pool = Eligible(call, out bool aid);
                call.PhraseAid = aid;
                pick = pool.Where(IsFree).OrderBy(w => w.LastAssignedAt).ThenBy(w => w.Order).FirstOrDefault();
            }
            if (pick == null)
            {
                call.State = CallState.Queued;
                continue;
            }
            Assign(call, pick);
        }
    }

    void Assign(CallCase call, CallWorker worker)
    {
        call.Owner = worker.Id;
        call.ReservedFor = "";
        call.State = CallState.Ringing;
        call.RangAt = Now;
        worker.LastAssignedAt = Now;
        Assigned?.Invoke(call, worker);
    }

    public bool Answer(int callId, string workerId)
    {
        var call = Call(callId);
        if (call == null || call.Owner != workerId || call.State != CallState.Ringing)
            return false;
        call.State = CallState.Active;
        call.AnsweredAt = Now;
        return true;
    }

    public bool SetHold(int callId, string workerId, bool on)
    {
        var call = Call(callId);
        if (call == null || call.Owner != workerId)
            return false;
        if (on && call.State == CallState.Active)
        {
            call.State = CallState.OnHold;
            call.HoldSince = Now;
            call.HoldComplained = false;
            return true;
        }
        if (!on && call.State == CallState.OnHold)
        {
            call.State = CallState.Active;
            return true;
        }
        return false;
    }

    /// Moves a call to another worker as one step: either everything changes or
    /// nothing does. The call object (and so its notes, facts, transcript,
    /// attempts, promises and patience) is the same one the recipient gets.
    public TransferResult Transfer(int callId, string fromId, string toId, string reason = "transfer")
    {
        var call = Call(callId);
        var to = Worker(toId);
        if (call == null || !call.Live) return TransferResult.NotLive;
        if (call.Owner != fromId || call.State == CallState.Ringing) return TransferResult.NotYours;
        if (to == null) return TransferResult.UnknownWorker;
        if (toId == fromId) return TransferResult.SameWorker;
        if (!to.Online) return TransferResult.Offline;
        if (call.Transfers.Count(t => t.Reason != "disconnected") >= MaxTransfers) return TransferResult.TooMany;
        var last = call.Transfers.LastOrDefault();
        if (last != null && last.From == toId && Now - last.At < BounceWindow) return TransferResult.Bouncing;

        // Validated: commit.
        var record = new TransferRecord { From = fromId, To = toId, Reason = reason, At = Now };
        call.Transfers.Add(record);
        call.Patience = Math.Max(0f, call.Patience - TransferCost);
        call.Muted = false;
        bool free = IsFree(to);
        call.Owner = "";
        call.ReservedFor = toId;
        call.State = CallState.Queued;
        Transferred?.Invoke(call, record);
        if (free)
        {
            Assign(call, to);
            return TransferResult.Ok;
        }
        return TransferResult.Queued;
    }

    /// A worker drops out (left the game, lost connection): their calls go back in the queue.
    public void Disconnect(string workerId)
    {
        var w = Worker(workerId);
        if (w == null || !w.Online)
            return;
        w.Online = false;
        foreach (var call in _calls.Where(c => c.Live && (c.Owner == workerId || c.ReservedFor == workerId)).ToList())
        {
            if (call.Owner == workerId)
                call.Transfers.Add(new TransferRecord { From = workerId, To = "", Reason = "disconnected", At = Now });
            call.Owner = "";
            call.ReservedFor = "";
            call.State = CallState.Queued;
        }
        Route();
    }

    public void Reconnect(string workerId)
    {
        var w = Worker(workerId);
        if (w == null || w.Online)
            return;
        w.Online = true;
        Route();
    }

    public void SetBreak(string workerId, bool onBreak)
    {
        var w = Worker(workerId);
        if (w == null)
            return;
        w.OnBreak = onBreak;
        if (!onBreak)
            Route();
    }

    public void End(CallCase call, CallOutcome outcome)
    {
        if (call == null || !call.Live)
            return;
        call.State = CallState.Ended;
        call.Outcome = outcome;
        call.EndedAt = Now;
        Ended?.Invoke(call);
        Route();
    }

    /// Advances time: patience drains, unanswered phones move on, customers give up.
    public void Tick(float dt)
    {
        Now += dt;
        foreach (var call in _calls.Where(c => c.Live).ToList())
        {
            float drain = call.State switch
            {
                CallState.Active => DrainActive,
                CallState.OnHold => DrainHold,
                _ => DrainWaiting,
            };
            call.Patience = Math.Max(0f, call.Patience - drain * dt);
            if (call.Patience <= 0f)
            {
                if (call.State == CallState.Active || call.State == CallState.OnHold)
                    call.CustomerSays("hangup", call.Owner);
                End(call, CallOutcome.HungUp);
                continue;
            }
            // Nobody picked up: offer it to the next person in the rotation.
            if (call.State == CallState.Ringing && Now - call.RangAt > RingTimeout)
            {
                var worker = Worker(call.Owner);
                var pool = Eligible(call, out _);
                if (pool.Any(w => w != worker && IsFree(w)))
                {
                    call.Owner = "";
                    call.State = CallState.Queued;
                    Route();
                }
            }
        }
        Route();
    }
}
