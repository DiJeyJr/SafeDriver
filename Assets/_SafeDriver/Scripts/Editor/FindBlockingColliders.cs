using UnityEditor;
using UnityEngine;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Lista todos los Colliders no-trigger cuyo bounds intersecta con la zona delantera
    /// del auto. Para diagnosticar "paredes invisibles" que bloquean al Rigidbody del auto.
    /// </summary>
    public static class FindBlockingColliders
    {
        [MenuItem("SafeDriver/Find Blocking Colliders (front of car)")]
        public static void Run()
        {
            var car = GameObject.Find("SafeDriver_Exterior_v1");
            if (car == null) { Debug.LogError("No se encontro SafeDriver_Exterior_v1"); return; }

            // Zona frente al auto: x in (-3, 3), y in (0, 3), z in (1, 15)
            var checkBounds = new Bounds(new Vector3(0, 1.5f, 8), new Vector3(6, 3, 14));
            int found = 0;

            foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col == null || col.isTrigger) continue;
                if (col.gameObject == car || col.transform.IsChildOf(car.transform)) continue;
                if (!col.bounds.Intersects(checkBounds)) continue;

                found++;
                Debug.Log(string.Format("[BLOCKING] {0} ({1}) bounds={2} center={3}",
                    col.gameObject.name, col.GetType().Name, col.bounds, col.bounds.center), col);
            }

            if (found == 0)
                Debug.Log("No se encontraron colliders no-trigger en la zona delantera del auto.");
            else
                Debug.Log("Total bloqueantes encontrados: " + found);
        }

        [MenuItem("SafeDriver/Map Road Segments (recorrido)")]
        public static void MapRoad()
        {
            // Zona del recorrido: x in (-15, 15), z in (-20, 180), y in (-1, 2)
            var checkBounds = new Bounds(new Vector3(0, 0.5f, 80), new Vector3(30, 3, 200));
            int found = 0;
            foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col == null) continue;
                if (!col.bounds.Intersects(checkBounds)) continue;
                if (!col.gameObject.name.ToLower().Contains("road") &&
                    !col.gameObject.name.ToLower().Contains("street") &&
                    !col.gameObject.name.ToLower().Contains("pavement") &&
                    !col.gameObject.name.ToLower().Contains("sidewalk") &&
                    !col.gameObject.name.ToLower().Contains("ground")) continue;
                found++;
                Debug.Log(string.Format("[ROAD] {0} pos={1} bounds.center={2} bounds.size={3} trigger={4}",
                    col.gameObject.name, col.transform.position, col.bounds.center, col.bounds.size, col.isTrigger), col);
            }
            Debug.Log("Total segmentos: " + found);
        }

        [MenuItem("SafeDriver/Map Sidewalks (recorrido)")]
        public static void MapSidewalks()
        {
            // Busca todo lo que esté en la zona x=-15..15, z=-20..180 y reporta el nombre
            var checkBounds = new Bounds(new Vector3(0, 0.5f, 80), new Vector3(40, 3, 200));
            var seen = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col == null) continue;
                if (!col.bounds.Intersects(checkBounds)) continue;
                var name = col.gameObject.name;
                if (seen.ContainsKey(name)) seen[name]++;
                else seen[name] = 1;
            }
            foreach (var kv in seen)
                Debug.Log(string.Format("[NEAR] {0} x{1}", kv.Key, kv.Value));
        }
    }
}
