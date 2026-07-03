using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundtrackManager : MonoBehaviour
{
    public static SoundtrackManager Instance;
    [SerializeField]
    private float volumeStep = 0.1f;

    private float currentVolume = 0.5f;

    void Start()
    {
        uint bankID; 
        AkSoundEngine.LoadBank("Main", out bankID);
        AkSoundEngine.PostEvent("Soundtrack", gameObject);
        AkSoundEngine.SetRTPCValue("MusicVolume", currentVolume * 100f);   
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentVolume = Mathf.Clamp01(currentVolume - volumeStep);
            AkSoundEngine.SetRTPCValue("MusicVolume", currentVolume * 100f);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            currentVolume = Mathf.Clamp01(currentVolume + volumeStep);
            AkSoundEngine.SetRTPCValue("MusicVolume", currentVolume * 100f);
        }
    }
}
