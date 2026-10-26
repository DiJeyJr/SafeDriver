using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Cambia de escena (modo Image Target / modo Ground Plane). Se wirea desde Button.onClick.</summary>
public class SceneSwitcher : MonoBehaviour
{
    public void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Para probar desde adb sin tocar la pantalla (en este celu no se pueden inyectar toques):
    //   adb shell am force-stop com.safedriver.ar
    //   adb shell am start -n com.safedriver.ar/com.unity3d.player.UnityPlayerGameActivity --es scene GroundPlane --ei delay 5
    // Arranca normal (Image Target) y a los <delay> segundos pasa a la escena pedida, igual que tocando el boton.
    private static bool sIntentChecked;

    private IEnumerator Start()
    {
        if (sIntentChecked) yield break;
        sIntentChecked = true;

        string scene = null;
        int delay = 3;
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity.Call<AndroidJavaObject>("getIntent");
            scene = intent.Call<string>("getStringExtra", "scene");
            delay = intent.Call<int>("getIntExtra", "delay", 3);
        }
        catch (System.Exception)
        {
            yield break;
        }

        if (string.IsNullOrEmpty(scene) || scene == SceneManager.GetActiveScene().name) yield break;

        Debug.Log("[SceneSwitcher] el intent pide " + scene + " en " + delay + " s");
        yield return new WaitForSeconds(delay);
        Load(scene);
    }
#endif
}
