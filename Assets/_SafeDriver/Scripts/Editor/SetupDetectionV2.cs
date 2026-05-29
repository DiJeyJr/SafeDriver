using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SafeDriver.Scoring;
using SafeDriver.Traffic;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Re-arma la deteccion de Level_01_City con el sistema nuevo (zonas separadas):
    ///   - Semaforo: desactiva el TrafficLightDetector viejo y monta TL_StopZone
    ///     (premio frenar en rojo, antes de la linea) + TL_CrossLine (infraccion/premio
    ///     al cruzar la linea).
    ///   - Peaton: agrega un BoxCollider trigger + PedestrianHitbox al PedestrianVisual
    ///     para detectar atropello (HitPedestrian).
    ///   - Crea una DirectionZone de prueba en un carril.
    ///
    /// Idempotente: si los objetos ya existen, los reusa. Disparar con el menu y guardar.
    /// </summary>
    public static class SetupDetectionV2
    {
        [MenuItem("SafeDriver/Setup Deteccion V2 (Level_01_City)")]
        public static void Run()
        {
#pragma warning disable CS0618 // uso intencional del detector obsoleto para migrarlo
            var oldDetector = Object.FindFirstObjectByType<TrafficLightDetector>();
#pragma warning restore CS0618
            if (oldDetector == null)
            {
                Debug.LogError("[SetupDeteccionV2] No se encontro TrafficLightDetector viejo en la escena.");
                return;
            }

            // Recuperar la referencia al TrafficLightController y la posicion de la linea.
            var oldSo = new SerializedObject(oldDetector);
            var tlc = oldSo.FindProperty("trafficLight").objectReferenceValue as TrafficLightController;
            Transform oldT = oldDetector.transform;
            Transform parent = oldT.parent;
            Vector3 linePos = oldT.position;   // z de la linea de detencion
            if (tlc == null)
                Debug.LogWarning("[SetupDeteccionV2] El detector viejo no tenia TrafficLightController asignado; se intentara buscar uno en escena.");
            if (tlc == null) tlc = Object.FindFirstObjectByType<TrafficLightController>();

            // 1. Desactivar el detector viejo (no borrar, por si otra escena lo usa).
            oldDetector.gameObject.SetActive(false);

            // 2. StopZone: 7m antes de la linea, cubriendo el tramo de aproximacion.
            var stop = GetOrCreate("TL_StopZone", parent);
            stop.transform.position = linePos + new Vector3(0f, 0f, -7f);
            var stopBox = EnsureTriggerBox(stop, new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 14f));
            var stopZone = EnsureComponent<RedLightStopZone>(stop);
            SetRef(stopZone, "trafficLight", tlc);

            // 3. CrossLine: sobre la linea de detencion. localForward = +Z (sentido de avance).
            var cross = GetOrCreate("TL_CrossLine", parent);
            cross.transform.position = linePos + new Vector3(0f, 0f, 1f);
            cross.transform.rotation = Quaternion.identity;
            EnsureTriggerBox(cross, new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 6f));
            var crossLine = EnsureComponent<TrafficLightCrossLine>(cross);
            SetRef(crossLine, "trafficLight", tlc);

            // 4. Peaton: collider trigger + hitbox en el PedestrianVisual.
            var pedVisual = GameObject.Find("PedestrianVisual");
            if (pedVisual != null)
            {
                var pedBox = pedVisual.GetComponent<BoxCollider>();
                if (pedBox == null) pedBox = pedVisual.AddComponent<BoxCollider>();
                pedBox.isTrigger = true;
                pedBox.center = Vector3.zero;
                pedBox.size = new Vector3(1.2f, 2f, 1.2f); // cubre el cuerpo (la escala del visual lo agranda)
                EnsureComponent<PedestrianHitbox>(pedVisual);
            }
            else
            {
                Debug.LogWarning("[SetupDeteccionV2] No se encontro PedestrianVisual; no se agrego PedestrianHitbox.");
            }

            // 5. DirectionZone de prueba: en el carril contrario, sentido legal +Z.
            var dir = GetOrCreate("DirectionZone_Test", parent);
            dir.transform.position = new Vector3(4f, 0f, 40f);
            dir.transform.rotation = Quaternion.identity;
            EnsureTriggerBox(dir, new Vector3(0f, 2f, 0f), new Vector3(6f, 4f, 20f));
            EnsureComponent<DirectionZone>(dir);

            EditorSceneManager.MarkSceneDirty(stop.scene);
            Debug.Log("[SetupDeteccionV2] Listo. TrafficLightDetector viejo desactivado; montados TL_StopZone, " +
                      "TL_CrossLine, PedestrianHitbox y DirectionZone_Test. Guardar la escena (Ctrl+S).");
        }

        private static GameObject GetOrCreate(string name, Transform parent)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        private static BoxCollider EnsureTriggerBox(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = center;
            box.size = size;
            return box;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedProperties(); }
        }
    }
}
