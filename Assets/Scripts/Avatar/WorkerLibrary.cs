using UnityEngine;

/// <summary>
/// The rigged worker models (one per crew role, same skeleton and clips) and the
/// animator controller they share. Lives as an asset so runtime code can spawn
/// workers without the editor's asset database.
/// </summary>
[CreateAssetMenu(menuName = "Unqualified/Worker Library")]
public class WorkerLibrary : ScriptableObject
{
    [Tooltip("Index = player slot: host, sound, catering, runner.")]
    public GameObject[] models;
    public RuntimeAnimatorController controller;
    [Tooltip("Per model (same index): its body mesh without the moustache (WorkerFaces).")]
    public Mesh[] cleanShaven;

    [Header("First-person arms (segments, pivot at the joint, length along +Z)")]
    public GameObject armUpper;
    public GameObject armForearm;
    public GameObject handPalm;
    public GameObject fingerBase;
    public GameObject fingerTip;
    public GameObject thumbBase;
    public GameObject thumbTip;

    public GameObject ModelForSlot(int slot) => models[((slot % models.Length) + models.Length) % models.Length];

    public Mesh CleanShavenForSlot(int slot)
    {
        if (cleanShaven == null || cleanShaven.Length != models.Length)
            return null;
        return cleanShaven[((slot % models.Length) + models.Length) % models.Length];
    }
}
