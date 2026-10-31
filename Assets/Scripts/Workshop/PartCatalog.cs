using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One kind of component the shop sells. Ids are stable (saved); text is for the UI.</summary>
public sealed class PartDef
{
    public string Id;
    public string Name;
    public string Blurb;
    public int Price;
    /// The prop kind its physical model has on the bench (what a bench socket accepts).
    public string Kind;
    /// For modules: the keyed port on the controller board it belongs in ("" otherwise).
    public string Port = "";
    /// Its colour in the shop and on the bench (the port shares it).
    public Color Colour;
}

/// <summary>A gadget the crew knows how to build, and what it does at work.</summary>
public sealed class RecipeDef
{
    public string Id;
    public string Name;
    /// The job it helps with (Jobs ids).
    public string Job;
    public string Blurb;
    /// What using it looks like, and what gives it away (for the rack and the blueprint).
    public string Use;
    public string Risk;
    public string[] Parts;
    /// The board ports this recipe fills (each needs its module).
    public string[] Ports;
}

/// <summary>
/// Everything the workshop can buy and build. Six reusable part families, two
/// working recipes that share most of them. Prices are proposed starting
/// points: the first gadget is meant to cost about a quarter of a decent shift.
/// The physical models and their sockets on the bench are named after these ids.
/// </summary>
public static class PartCatalog
{
    public const string Casing = "casing";
    public const string Battery = "battery";
    public const string Board = "board";
    public const string Sensor = "sensor";
    public const string Motor = "motor";
    public const string Speaker = "speaker";

    /// Keyed ports on the controller board: a label, a shape and a colour each.
    public const string PortAudio = "audio";
    public const string PortSensor = "sensor";
    public const string PortMotor = "motor";
    public static readonly string[] Ports = { PortSensor, PortAudio, PortMotor };

    public const string WhisperBox = "whisperbox";
    public const string SteadyHand = "steadyhand";

    /// How many of each part the shop will sell in one day.
    public const int DailyStock = 4;
    /// Seconds from checkout to the knock at the door (while you are at home).
    public const float DeliverySeconds = 14f;
    /// Seconds on the charging rack from flat to full.
    public const float ChargeSeconds = 40f;

    public static readonly PartDef[] Parts =
    {
        new() { Id = Casing, Name = "Salvaged casing", Blurb = "A plastic box with a lid. Previously a pencil case.", Price = 8, Kind = "casing", Colour = new Color(0.45f, 0.47f, 0.5f) },
        new() { Id = Board, Name = "Mystery controller board", Blurb = "Three keyed ports. Instructions in a language nobody speaks.", Price = 14, Kind = "board", Colour = new Color(0.18f, 0.5f, 0.3f) },
        new() { Id = Battery, Name = "Battery pack (allegedly)", Blurb = "Holds a charge. Mostly.", Price = 10, Kind = "battery", Colour = new Color(0.85f, 0.7f, 0.15f) },
        new() { Id = Sensor, Name = "Tilt sensor", Blurb = "Knows which way is down. Round plug.", Price = 9, Kind = "module", Port = PortSensor, Colour = new Color(0.2f, 0.45f, 0.85f) },
        new() { Id = Motor, Name = "Pager vibration motor", Blurb = "Very small, very angry. Square plug.", Price = 11, Kind = "module", Port = PortMotor, Colour = new Color(0.92f, 0.5f, 0.15f) },
        new() { Id = Speaker, Name = "Speaker / display module", Blurb = "A tiny speaker and a tinier screen. Triangle plug.", Price = 9, Kind = "module", Port = PortAudio, Colour = new Color(0.35f, 0.75f, 0.35f) },
    };

    public static readonly RecipeDef[] Recipes =
    {
        new()
        {
            Id = WhisperBox, Name = "WhisperBox", Job = Jobs.CallCenter,
            Blurb = "An earpiece for the call center headset. Turns what you already know about a call into a short hint.",
            Use = "At your desk on a call: tap the gadget key for a hint (5 per charge). It suggests what to ask or which help page to read. You still choose the fix.",
            Risk = "It chirps. A supervisor close by, or listening in, will hear it.",
            Parts = new[] { Casing, Board, Battery, Speaker },
            Ports = new[] { PortAudio },
        },
        new()
        {
            Id = SteadyHand, Name = "SteadyHand", Job = Jobs.Wedding,
            Blurb = "A stabiliser that clips under a tray or a cake board and fights the wobble.",
            Use = "Tap the gadget key to switch it on. While you carry something wide it damps the wobble and the lean. It drains while it works.",
            Risk = "It's bulky and hums under heavy correction. The coordinator can spot it from a few metres, or come over when she hears it.",
            Parts = new[] { Casing, Board, Sensor, Motor, Battery },
            Ports = new[] { PortSensor, PortMotor },
        },
    };

    public static PartDef Part(string id) => Array.Find(Parts, p => p.Id == id);
    public static RecipeDef Recipe(string id) => Array.Find(Recipes, r => r.Id == id);

    /// The recipe that helps with a job (the starter kit builds this one).
    public static RecipeDef RecipeFor(string job) => Array.Find(Recipes, r => r.Job == job) ?? Recipes[0];

    public static int PriceOf(RecipeDef recipe)
    {
        int total = 0;
        foreach (var id in recipe.Parts)
            total += Part(id).Price;
        return total;
    }

    /// The recipes that use a part, by name (for the shop).
    public static List<string> UsedIn(string part)
    {
        var names = new List<string>();
        foreach (var r in Recipes)
            if (Array.IndexOf(r.Parts, part) >= 0)
                names.Add(r.Name);
        return names;
    }

    /// The module that belongs in a port.
    public static PartDef ModuleFor(string port) => Array.Find(Parts, p => p.Port == port);

    public static string PortLabel(string port) => port switch
    {
        PortSensor => "SNS (round)",
        PortMotor => "MTR (square)",
        PortAudio => "AUD (triangle)",
        _ => port,
    };

    public static string JobLabel(string job) => job == Jobs.CallCenter ? "the call center" : "the wedding";
}
