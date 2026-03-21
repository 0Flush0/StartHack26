using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class ExportHousesJson
{
    [System.Serializable]
    public class HouseRecord
    {
        public string id;
        public string type;

        // Dummy metadata (these will be filled/used per-house)
        public string companyName = "DummyCompany";
        public string country = "DummyCountry";
        public string sector = "DummySector";

        // Positions / transforms
        public float[] position; // [x,y,z]
        public float[] rotation; // quaternion [x,y,z,w]
        public float[] rotationEulerDeg; // euler angles in degrees [x,y,z]
        public float[] scale; // [x,y,z]

        // Game-specific fields
        public bool locked = true;
        public int price = 0;
    }

    [System.Serializable]
    public class RoadRecord
    {
        public string id;
        public string type;

        // Positions / transforms
        public float[] position; // [x,y,z]
        public float[] rotation; // quaternion [x,y,z,w]
        public float[] rotationEulerDeg; // euler angles in degrees [x,y,z]
        public float[] scale; // [x,y,z]
    }

    [System.Serializable]
    public class RoadSegmentRecord : RoadRecord
    {
    }

    [System.Serializable]
    public class RoadGroupRecord
    {
        public string id;   // group id (e.g. road-straight)
        public string type; // same as base type
        public List<RoadSegmentRecord> segments = new List<RoadSegmentRecord>();
    }

    [System.Serializable]
    public class CityMeta
    {
        public string companyName = "DummyCompany";
        public string country = "DummyCountry";
        public string sector = "DummySector";
    }

    [System.Serializable]
    public class CityExport
    {
        public CityMeta meta = new CityMeta();
        public string scene;
        public string unit = "unity";

        // Placements
        public List<HouseRecord> houses = new List<HouseRecord>();
        // Kept for backward compatibility (un-grouped road segments)
        public List<RoadRecord> roads = new List<RoadRecord>();

        // New: grouped road components (recommended for Three.js)
        public List<RoadGroupRecord> roadGroups = new List<RoadGroupRecord>();
    }

    private static bool HasTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return false;
        var tags = InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tagName) return true;
        }
        return false;
    }

    private static bool IsHouse(GameObject go, GameObject housesRoot, bool hasHouseTag)
    {
        if (go == null) return false;
        if (!go.scene.IsValid()) return false;

        if (hasHouseTag && go.CompareTag("House")) return true;
        if (housesRoot != null && go.transform.IsChildOf(housesRoot.transform) && go != housesRoot) return true;

        // Fallback heuristics (based on your screenshot/object names)
        if (go.name.StartsWith("building-", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (go.name.StartsWith("house-", System.StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    private static bool IsRoad(GameObject go, GameObject roadsRoot, bool hasRoadTag)
    {
        if (go == null) return false;
        if (!go.scene.IsValid()) return false;

        if (hasRoadTag && go.CompareTag("Road")) return true;
        if (roadsRoot != null && go.transform.IsChildOf(roadsRoot.transform) && go != roadsRoot) return true;

        // Fallback heuristics (based on your screenshot/object names)
        if (go.name.StartsWith("road-", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (go.name.StartsWith("street-", System.StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    private static HouseRecord ToHouseRecord(GameObject go)
    {
        var t = go.transform;
        return new HouseRecord
        {
            id = go.name,
            type = go.name,
            companyName = "DummyCompany",
            country = "DummyCountry",
            sector = "DummySector",
            position = new[] { t.position.x, t.position.y, t.position.z },
            rotation = new[] { t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w },
            rotationEulerDeg = new[] { t.rotation.eulerAngles.x, t.rotation.eulerAngles.y, t.rotation.eulerAngles.z },
            scale = new[] { t.lossyScale.x, t.lossyScale.y, t.lossyScale.z },
            locked = true,
            price = 0
        };
    }

    private static RoadRecord ToRoadRecord(GameObject go)
    {
        var t = go.transform;
        return new RoadRecord
        {
            id = go.name,
            type = go.name,
            position = new[] { t.position.x, t.position.y, t.position.z },
            rotation = new[] { t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w },
            rotationEulerDeg = new[] { t.rotation.eulerAngles.x, t.rotation.eulerAngles.y, t.rotation.eulerAngles.z },
            scale = new[] { t.lossyScale.x, t.lossyScale.y, t.lossyScale.z }
        };
    }

    private static string GetBaseName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        // Unity duplicates often end with " (1)", " (2)", ...
        // Example: "road-straight (3)" -> "road-straight"
        return Regex.Replace(name, @"\s*\(\d+\)$", "").Trim();
    }

    private static void ExportAll()
    {
        var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        var housesRoot = GameObject.Find("Houses");
        var roadsRoot = GameObject.Find("Roads");

        var hasHouseTag = HasTag("House");
        var hasRoadTag = HasTag("Road");

        var export = new CityExport
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        var roadGroupsByBaseName = new Dictionary<string, RoadGroupRecord>();

        foreach (var go in all)
        {
            // Only export objects that actually render something.
            if (go.GetComponentInChildren<Renderer>() == null) continue;

            if (IsHouse(go, housesRoot, hasHouseTag))
            {
                export.houses.Add(ToHouseRecord(go));
            }
            else if (IsRoad(go, roadsRoot, hasRoadTag))
            {
                // Un-grouped legacy list
                export.roads.Add(ToRoadRecord(go));

                // Grouped road component
                var baseName = GetBaseName(go.name);
                if (!roadGroupsByBaseName.TryGetValue(baseName, out var group))
                {
                    group = new RoadGroupRecord { id = baseName, type = baseName };
                    roadGroupsByBaseName[baseName] = group;
                }

                var segment = new RoadSegmentRecord
                {
                    id = go.name,
                    type = baseName,
                    position = new[] { go.transform.position.x, go.transform.position.y, go.transform.position.z },
                    rotation = new[] { go.transform.rotation.x, go.transform.rotation.y, go.transform.rotation.z, go.transform.rotation.w },
                    rotationEulerDeg = new[] { go.transform.rotation.eulerAngles.x, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z },
                    scale = new[] { go.transform.lossyScale.x, go.transform.lossyScale.y, go.transform.lossyScale.z }
                };

                group.segments.Add(segment);
            }
        }

        // Emit groups in stable order.
        var orderedKeys = new List<string>(roadGroupsByBaseName.Keys);
        orderedKeys.Sort(System.StringComparer.OrdinalIgnoreCase);
        foreach (var key in orderedKeys)
        {
            export.roadGroups.Add(roadGroupsByBaseName[key]);
        }

        var path = EditorUtility.SaveFilePanel(
            "Save houses.json",
            Application.dataPath,
            "houses.json",
            "json"
        );

        if (string.IsNullOrEmpty(path)) return;

        var json = JsonUtility.ToJson(export, true);
        File.WriteAllText(path, json);
        EditorUtility.RevealInFinder(path);
        Debug.Log($"Exported {export.houses.Count} houses and {export.roads.Count} roads to {path}");
    }

    [MenuItem("Tools/Export/Export Houses JSON (Tag=House)")]
    public static void ExportByTag()
    {
        ExportAll();
    }

    [MenuItem("Tools/Export/Export Houses JSON (Root=Houses)")]
    public static void ExportByRoot()
    {
        ExportAll();
    }
}
