using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    [System.Serializable]
    public class UIAudioEntry
    {
        public UIInteractionType interactionType;
        public AudioClipData audioData;
    }

    [System.Serializable]
    public class UIAudioData
    {
        [Header("UI Audio Mappings")]
        [SerializeField] private List<UIAudioEntry> uiAudioEntries = new List<UIAudioEntry>();
        
        // Dictionary for fast lookup at runtime
        private Dictionary<UIInteractionType, AudioClipData> audioLookup;
        
        /// <summary>
        /// Initializes the audio lookup dictionary
        /// </summary>
        public void Initialize()
        {
            audioLookup = new Dictionary<UIInteractionType, AudioClipData>();
            
            foreach (var entry in uiAudioEntries)
            {
                if (entry.audioData != null && !audioLookup.ContainsKey(entry.interactionType))
                {
                    audioLookup[entry.interactionType] = entry.audioData;
                }
            }
        }
        
        /// <summary>
        /// Gets the audio data for a specific UI interaction type
        /// </summary>
        public AudioClipData GetAudioData(UIInteractionType interactionType)
        {
            if (audioLookup == null)
                Initialize();
                
            audioLookup.TryGetValue(interactionType, out AudioClipData audioData);
            return audioData;
        }
        
        /// <summary>
        /// Checks if audio data exists for the specified interaction type
        /// </summary>
        public bool HasAudioForInteraction(UIInteractionType interactionType)
        {
            if (audioLookup == null)
                Initialize();
                
            return audioLookup.ContainsKey(interactionType) && audioLookup[interactionType] != null;
        }
        
        /// <summary>
        /// Adds or updates audio data for a specific interaction type at runtime
        /// </summary>
        public void SetAudioData(UIInteractionType interactionType, AudioClipData audioData)
        {
            if (audioLookup == null)
                Initialize();
                
            audioLookup[interactionType] = audioData;
        }
    }
}
