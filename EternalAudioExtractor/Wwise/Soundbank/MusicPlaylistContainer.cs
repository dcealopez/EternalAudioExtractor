using System.Collections.Generic;

namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Music Playlist Container object class
    /// </summary>
    public class MusicPlaylistContainer : HircObject
    {
        /// <summary>
        /// List of Music Segment object ids
        /// </summary>
        public List<uint> MusicSegmentIds = new List<uint>();
    }
}
