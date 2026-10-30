var from = new UnityEngine.Vector3(-2f, 0f, -5f);
string Check(string n, UnityEngine.Vector3 to) { var p = new UnityEngine.AI.NavMeshPath(); UnityEngine.AI.NavMesh.CalculatePath(from, to, UnityEngine.AI.NavMesh.AllAreas, p); return $"{n}: {p.status} ({p.corners.Length} corners)"; }
return string.Join(" | ", new[] { Check("kitchen", new UnityEngine.Vector3(-10f, 0f, -11f)), Check("storage", new UnityEngine.Vector3(3.5f, 0f, -11f)), Check("cloakroom", new UnityEngine.Vector3(-16f, 0f, 4f)), Check("corridor", new UnityEngine.Vector3(15.3f, 0f, 4f)) });
