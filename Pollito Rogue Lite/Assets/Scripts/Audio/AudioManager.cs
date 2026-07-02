using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Source Pool")]
        [SerializeField] private int audioSourcePoolSize = 10; //Cantidad de AudioSources que se van a crear
        [SerializeField] private List<AudioSource> audioSourcePool = new List<AudioSource>(); //Lista donde se guardan los AudioSources creados
        
        [Header("Audio Categories")]
        [SerializeField] private UIAudioData uiAudioData; //Datos de sonidos para la IO (botones, menus...)
        [SerializeField] private AudioClipData environmentSounds; //Sonidos de ambiente
        [SerializeField] private AudioClipData powerupSounds; //Sonidos de power-ups
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugSounds = true; //Habilita sonidos de debug si falta un sonido
        
        private int currentPoolIndex = 0;
        
        void Awake()
        {
            InitializeAudioSourcePool(); //Crear el pool de AudioSources
            // Inicializar los sonidos de UI si estan configurados
            if (uiAudioData != null) uiAudioData.Initialize();
        }
        
        /// <summary>
        /// Crea un pool de objetos AudioSource para evitar instanciarlos cada vez que se reproduce un sonido
        /// </summary>
        private void InitializeAudioSourcePool()
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                GameObject audioSourceGO = new GameObject($"PooledAudioSource_{i}");
                audioSourceGO.transform.SetParent(transform);
                AudioSource source = audioSourceGO.AddComponent<AudioSource>();
                source.playOnAwake = false;
                audioSourcePool.Add(source);
            }
        }
        
        /// <summary>
        /// Devuelve el siguiente AudioSource disponible del pool de forma circular
        /// </summary>
        private AudioSource GetPooledAudioSource()
        {
            if (audioSourcePool.Count == 0) return null;
            AudioSource source = audioSourcePool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Count;
            return source;
        }
        
        /// <summary>
        /// Reproduce un AudioClipData con un AudioSource
        /// </summary>
        public void PlayAudio(AudioClipData audioData, Vector3 position = default)
        {
            if (audioData == null || !audioData.HasClips()) return;
            AudioSource source = GetPooledAudioSource();
            if (source == null) return;
            AudioClip clipToPlay = audioData.GetRandomClip();
            if (clipToPlay == null) return;
            // Si se especifico una posicion se coloca el audio ahi
            if (position != default) source.transform.position = position;
            source.clip = clipToPlay;
            source.volume = audioData.GetRandomizedVolume();
            source.pitch = audioData.GetRandomizedPitch();
            source.Play();
        }
        
        /// <summary>
        /// Igual que PlayAudio pero usando PlayOneShot (permite solapar sonidos sin cortar el anterior)
        /// </summary>
        public void PlayAudioOneShot(AudioClipData audioData, Vector3 position = default)
        {
            if (audioData == null || !audioData.HasClips()) return;
            AudioSource source = GetPooledAudioSource();
            if (source == null) return;
            AudioClip clipToPlay = audioData.GetRandomClip();
            if (clipToPlay == null) return;
            // Position the audio source if a position is provided
            if (position != default) source.transform.position = position;
            float volume = audioData.GetRandomizedVolume();
            float pitch = audioData.GetRandomizedPitch();
            
            float originalPitch = source.pitch;
            source.pitch = pitch;
            source.PlayOneShot(clipToPlay, volume);
            source.pitch = originalPitch;
        }
        
        /// <summary>
        /// Reproduce un clip especifico en una posicion con volumen y pitch dados
        /// </summary>
        public void PlayClipAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            AudioSource source = GetPooledAudioSource();
            if (source == null) return;
            source.transform.position = position;
            source.volume = volume;
            source.pitch = pitch;
            source.PlayOneShot(clip);
        }
        
        // METODOS DE CONVENIENCIA
        
        /// <summary>
        /// Reproduce un sonido de UI segun el tipo de interaccion (click, hover)
        /// </summary>
        public void PlayUISound(UIInteractionType interactionType, Vector3 position = default)
        {
            if (uiAudioData == null) return;
            AudioClipData audioData = uiAudioData.GetAudioData(interactionType);
            
            // Reproduce el sonido correcto
            if (audioData != null)
            {
                PlayAudioOneShot(audioData, position);
                return;
            }
            
            // Si no hay un sonido definido, intenta reproducir un sonido de debug
            if (enableDebugSounds)
            {
                AudioClipData debugAudioData = uiAudioData.GetAudioData(UIInteractionType.DebugGeneral);
                if (debugAudioData != null)
                {
                    Debug.Log($"[AudioManager] Playing DebugGeneral fallback for {interactionType}");
                    PlayAudioOneShot(debugAudioData, position);
                    return;
                }
                else Debug.LogWarning($"[AudioManager] No audio found for {interactionType} and no DebugGeneral fallback available");
            }
            else Debug.LogWarning($"[AudioManager] No audio found for {interactionType} and debug sounds are disabled");
            
        }
        
        /// <summary>
        /// Verifica si existe audio asignado para un tipo de interacción de UI
        /// </summary>
        public bool HasUIAudio(UIInteractionType interactionType)
        {
            return uiAudioData != null && uiAudioData.HasAudioForInteraction(interactionType);
        }
        
        // Métodos cortos para accesos rápidos a sonidos comunes de UI
        public void PlayButtonClick(Vector3 position = default) => PlayUISound(UIInteractionType.ButtonClick, position);
        public void PlayButtonHover(Vector3 position = default) => PlayUISound(UIInteractionType.ButtonHover, position);
        public void PlayButtonPress(Vector3 position = default) => PlayUISound(UIInteractionType.ButtonPress, position);
        public void PlayMenuOpen(Vector3 position = default) => PlayUISound(UIInteractionType.MenuOpen, position);
        public void PlayMenuClose(Vector3 position = default) => PlayUISound(UIInteractionType.MenuClose, position);
        public void PlayTabSwitch(Vector3 position = default) => PlayUISound(UIInteractionType.TabSwitch, position);
        public void PlayToggleOn(Vector3 position = default) => PlayUISound(UIInteractionType.ToggleOn, position);
        public void PlayToggleOff(Vector3 position = default) => PlayUISound(UIInteractionType.ToggleOff, position);
        public void PlayNotificationPopup(Vector3 position = default) => PlayUISound(UIInteractionType.NotificationPopup, position);
        public void PlayErrorSound(Vector3 position = default) => PlayUISound(UIInteractionType.ErrorSound, position);
        public void PlaySuccessSound(Vector3 position = default) => PlayUISound(UIInteractionType.SuccessSound, position);
        
        public void PlayEnvironmentSound(Vector3 position = default) => PlayAudioOneShot(environmentSounds, position);
        public void PlayPowerupSound(Vector3 position = default) => PlayAudioOneShot(powerupSounds, position);
        
        /// <summary>
        /// Detiene todos los sonidos en reproduccion
        /// </summary>
        public void StopAllAudio()
        {
            foreach (AudioSource source in audioSourcePool)
            {
                if (source != null) source.Stop();
            }
        }
        
        /// <summary>
        /// Cambia el volumen de todos los audios activos
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            foreach (AudioSource source in audioSourcePool)
            {
                if (source != null) source.volume = volume;
            }
        }
    }
}
