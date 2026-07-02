using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    [System.Serializable]
    public class AudioClipData
    {
        [Header("Audio Clips")]
        public List<AudioClip> AudioClips = new List<AudioClip>();
        
        [Header("Randomization Settings")]
        public bool EnableRandomization;
        
        [Header("Volume Randomization")]
        [Range(0f, 1f)]
        public float BaseVolume = 1f;
        [Range(0f, 1f)]
        public float VolumeVariation = 0.1f;
        
        [Header("Pitch Randomization")]
        [Range(0.1f, 3f)]
        public float BasePitch = 1f;
        [Range(0f, 1f)]
        public float PitchVariation = 0.1f;
        
        // Track the last played clip to avoid repeating
        private AudioClip lastPlayedClip;
        
        /// <summary>
        /// Gets a random audio clip from the list, avoiding the last played clip if possible
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (AudioClips == null || AudioClips.Count == 0)
                return null;
            
            // If only one clip, return it
            if (AudioClips.Count == 1)
            {
                lastPlayedClip = AudioClips[0];
                return AudioClips[0];
            }
            
            // If multiple clips, avoid repeating the last one
            AudioClip selectedClip;
            int attempts = 0;
            const int maxAttempts = 10; // Prevent infinite loop
            
            do
            {
                selectedClip = AudioClips[Random.Range(0, AudioClips.Count)];
                attempts++;
            }
            while (selectedClip == lastPlayedClip && attempts < maxAttempts);
            
            lastPlayedClip = selectedClip;
            return selectedClip;
        }
        
        /// <summary>
        /// Gets randomized volume based on settings
        /// </summary>
        public float GetRandomizedVolume()
        {
            if (!EnableRandomization)
                return BaseVolume;
                
            float variation = Random.Range(-VolumeVariation, VolumeVariation);
            return Mathf.Clamp01(BaseVolume + variation);
        }
        
        /// <summary>
        /// Gets randomized pitch based on settings
        /// </summary>
        public float GetRandomizedPitch()
        {
            if (!EnableRandomization)
                return BasePitch;
                
            float variation = Random.Range(-PitchVariation, PitchVariation);
            return Mathf.Clamp(BasePitch + variation, 0.1f, 3f);
        }
        
        /// <summary>
        /// Checks if this audio data has any clips to play
        /// </summary>
        public bool HasClips()
        {
            return AudioClips != null && AudioClips.Count > 0;
        }
    }
}
