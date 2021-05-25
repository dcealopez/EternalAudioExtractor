namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Path Endpoint class
    /// </summary>
    public class PathEndpoint : PathElement
    {
        /// <summary>
        /// The ID of the music object to play
        /// Can only be objects that are direct children of the Music Switch Container
        /// </summary>
        public uint MusicObjectId { get; set; }
    }
}
