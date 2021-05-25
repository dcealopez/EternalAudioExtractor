using System.Collections.Generic;

namespace EternalAudioExtractor.Sound
{
    /// <summary>
    /// Element group class
    /// </summary>
    public class ElementGroup
    {
        /// <summary>
        /// Group Id
        /// </summary>
        public uint GroupId;

        /// <summary>
        /// Group name
        /// </summary>
        public string Name;

        /// <summary>
        /// Children elements
        /// </summary>
        public List<Element> Children = new List<Element>();
    }
}
