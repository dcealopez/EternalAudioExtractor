namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Path Element class for Music Switch Container objects
    /// </summary>
    public class PathElement
    {
        /// <summary>
        /// The ID of the parent State or Switch
        /// Zero if any
        /// </summary>
        public uint FromStateOrSwitchId { get; set; }
    }
}
