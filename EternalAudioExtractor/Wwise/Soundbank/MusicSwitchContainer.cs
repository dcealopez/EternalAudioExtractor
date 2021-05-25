using System;
using System.Collections.Generic;

namespace EternalAudioExtractor.Wwise.Soundbank
{
    /// <summary>
    /// Music Switch Container object class
    /// </summary>
    public class MusicSwitchContainer : HircObject
    {
        /// <summary>
        /// List of ids of children music objects
        /// </summary>
        public List<uint> MusicObjectIds = new List<uint>();

        /// <summary>
        /// Switch or State group children
        /// If true, the first item in the tuple indicates that it is a state group
        /// Otherwise it indicates that it is a switch group
        /// The second item is the element group id of the switch or state group
        /// </summary>
        public List<Tuple<bool, uint>> SwitchOrStateGroupChildren = new List<Tuple<bool, uint>>();

        /// <summary>
        /// The paths of this Music Switch Container object
        /// </summary>
        public PathNode Paths;
    }
}
