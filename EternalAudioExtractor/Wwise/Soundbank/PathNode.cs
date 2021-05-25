namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Path Node class
    /// </summary>
    public class PathNode : PathElement
    {
        /// <summary>
        /// The beginning position of the node's children in all nodes
        /// </summary>
        public ushort ChildrenStartAtIndex { get; set; }

        /// <summary>
        /// The count of children of the path parent
        /// Children can be path parents or path endpoints
        /// </summary>
        public ushort ChildCount { get; set; }

        /// <summary>
        /// Children of the path node
        /// </summary>
        public PathElement[] Children { get; set; }
    }
}
