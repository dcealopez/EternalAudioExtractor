using System.Collections.Generic;

namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Music Track object class
    /// </summary>
    public class MusicTrack : HircObject
    {
        /// <summary>
        /// List of audio file ids
        /// </summary>
        public List<uint> AudioFileIds = new List<uint>();
    }
}
