using System.Collections.Generic;

namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Music Segment object class
    /// </summary>
    public class MusicSegment : HircObject
    {
        /// <summary>
        /// List of Music Track object ids
        /// </summary>
        public List<uint> MusicTrackIds = new List<uint>();
    }
}
