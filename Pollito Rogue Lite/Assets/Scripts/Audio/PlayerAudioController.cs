using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    public class PlayerAudioController : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private List<AudioSource> audioSources = new List<AudioSource>();
        [SerializeField] private int maxAudioSources = 5;
        
        [Header("Audio Data")]
        [SerializeField] private AudioClipData footstepSounds;
        
        [Header("Footstep Timing Control")]
        [SerializeField] private float minTimeBetweenFootsteps = 0.3f;
        [SerializeField] private bool enableFootstepTiming = true;
        
        private int currentSourceIndex = 0;
        private float lastFootstepTime = 0f;
        
        void Start()
        {
            InitializeAudioSources();
        }
        
        /// <summary>
        /// Initializes audio sources if they don't exist
        /// </summary>
        private void InitializeAudioSources()
        {
            // Create audio sources if none are assigned
            if (audioSources.Count == 0)
            {
                for (int i = 0; i < maxAudioSources; i++)
                {
                    GameObject audioSourceGO = new GameObject($"AudioSource_{i}");
                    audioSourceGO.transform.SetParent(transform);
                    AudioSource source = audioSourceGO.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    audioSources.Add(source);
                }
            }
        }
        
        /// <summary>
        /// Gets the next available audio source
        /// </summary>
        private AudioSource GetNextAudioSource()
        {
            if (audioSources.Count == 0) return null;
            AudioSource source = audioSources[currentSourceIndex];
            currentSourceIndex = (currentSourceIndex + 1) % audioSources.Count;
            return source;
            //Devuelve la siguiente fuente disponible y actualiza currentSourceIndex.
            //Esto permite reproducir varios sonidos al mismo tiempo sin cortar otros que ya se están reproduciendo.
        }
        
        /// <summary>
        /// Plays audio using AudioClipData with randomization
        /// </summary>
        public void PlayAudio(AudioClipData audioData)
        {
            if (audioData == null || !audioData.HasClips()) return;
            AudioSource source = GetNextAudioSource();
            if (source == null) return;
            AudioClip clipToPlay = audioData.GetRandomClip();
            if (clipToPlay == null) return;
            source.clip = clipToPlay;
            source.volume = audioData.GetRandomizedVolume();
            source.pitch = audioData.GetRandomizedPitch();
            source.Play();
            //Reproduce un clip aleatorio de AudioClipData usando la siguiente fuente disponible.
            //Randomiza volume y pitch para que no suenen idénticos cada vez.
        }
        
        /// <summary>
        /// Plays audio using AudioClipData as oneshot
        /// </summary>
        public void PlayAudioOneShot(AudioClipData audioData)
        {
            if (audioData == null || !audioData.HasClips()) return;
            AudioSource source = GetNextAudioSource();
            if (source == null) return;
            AudioClip clipToPlay = audioData.GetRandomClip();
            if (clipToPlay == null) return;
            float volume = audioData.GetRandomizedVolume();
            float pitch = audioData.GetRandomizedPitch();
            
            // Store original pitch, apply new pitch, play oneshot, then restore
            float originalPitch = source.pitch;
            source.pitch = pitch;
            source.PlayOneShot(clipToPlay, volume);
            source.pitch = originalPitch;
            //Similar a PlayAudio, pero usa PlayOneShot, que es ideal para sonidos cortos que pueden superponerse.
            //Restaura el pitch original después de reproducir para no afectar futuros sonidos.
        }
        
        /// <summary>
        /// Plays a specific audio clip with optional volume and pitch
        /// </summary>
        public void PlayAudioClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            AudioSource source = GetNextAudioSource();
            if (source == null) return;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }
        
        /// <summary>
        /// Plays a specific audio clip as oneshot
        /// </summary>
        public void PlayAudioClipOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource source = GetNextAudioSource();
            if (source == null) return;
            source.PlayOneShot(clip, volume);
        }
        
        // Convenience methods for common audio types
        public void PlayFootstep()
        {
            if (!CanPlayFootstep()) return;
            AkSoundEngine.PostEvent("Walk", gameObject);
            lastFootstepTime = Time.time;
        }
        
        
        
        /// <summary>
        /// Checks if enough time has passed since the last footstep sound
        /// </summary>
        private bool CanPlayFootstep()
        {
            if (!enableFootstepTiming) return true;
            return Time.time - lastFootstepTime >= minTimeBetweenFootsteps;
        }
        
        /// Metodos para:
        // Ajustar intervalo de pasos.
        // Activar/desactivar control de pasos.
        // Reiniciar el temporizador de pasos (útil al cargar nueva escena).
        // Obtener los datos de pasos.
        // Detener todos los sonidos.
        // Ajustar volumen maestro de todas las fuentes de audio.
        /// </summary>
        public void SetFootstepTimingInterval(float interval)
        {
            minTimeBetweenFootsteps = Mathf.Max(0f, interval);
        }
        
        /// <summary>
        /// Enables or disables footstep timing control
        /// </summary>
        public void SetFootstepTimingEnabled(bool enabled)
        {
            enableFootstepTiming = enabled;
        }
        
        /// <summary>
        /// Resets the footstep timer (useful for scene transitions)
        /// </summary>
        public void ResetFootstepTimer()
        {
            lastFootstepTime = 0f;
        }
        
        /// <summary>
        /// Gets the footstep audio data for use with AudioManager
        /// </summary>
        public AudioClipData GetFootstepData()
        {
            return footstepSounds;
        }
        
        /// <summary>
        /// Stops all audio sources
        /// </summary>
        public void StopAllAudio()
        {
            foreach (AudioSource source in audioSources)
            {
                if (source != null) source.Stop();
            }
        }
        
        /// <summary>
        /// Sets the master volume for all audio sources
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            foreach (AudioSource source in audioSources)
            {
                if (source != null) source.volume = volume;
            }
        }
    }
}
