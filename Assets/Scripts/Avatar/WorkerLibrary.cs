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

    public GameObject ModelForSlot(int slot) => models[((slot % models.Length) + models.Length) % models.Length];
}
