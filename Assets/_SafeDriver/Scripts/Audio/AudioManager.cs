using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Audio
{
    /// <summary>
    /// Controlador maestro de audio: master volume, grupos (SFX/Music/UI), mute.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume    = 1f;
        [Range(0f, 1f)] public float musicVolume  = 0.7f;

        // TODO: AudioMixer + grupos, SetVolume, PlayOneShot
    }
}
