using System.Collections.Generic;

namespace EternalAudioExtractor.Sound
{
    /// <summary>
    /// Sound event class
    /// </summary>
    public class SoundEvent
    {
        /// <summary>
        /// Event name
        /// </summary>
        public string Name;

        /// <summary>
        /// Associated sound ids
        /// </summary>
        public List<uint> SoundIds;
    }
}
