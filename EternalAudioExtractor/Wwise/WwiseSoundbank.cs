using EternalAudioExtractor.Wwise.Soundbank;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EternalAudioExtractor.Wwise
{
    /// <summary>
    /// Wwise Soundbank class
    /// </summary>
    public class WwiseSoundbank
    {
        /// <summary>
        /// Music Track list
        /// </summary>
        public List<MusicTrack> MusicTracks = new List<MusicTrack>();

        /// <summary>
        /// Music Segment list
        /// </summary>
        public List<MusicSegment> MusicSegments = new List<MusicSegment>();

        /// <summary>
        /// Music Playlist Container list
        /// </summary>
        public List<MusicPlaylistContainer> MusicPlaylistContainers = new List<MusicPlaylistContainer>();

        /// <summary>
        /// Music Switch Container list
        /// </summary>
        public List<MusicSwitchContainer> MusicSwitchContainers = new List<MusicSwitchContainer>();

        /// <summary>
        /// Reads the Wwise Soundbank contained in the data
        /// </summary>
        /// <param name="data">soundbank file data byte array</param>
        /// <returns>a WwiseSoundbank object</returns>
        public static WwiseSoundbank Read(byte[] data)
        {
            WwiseSoundbank wwiseSoundbank = new WwiseSoundbank();

            using (var memoryStream = new MemoryStream(data))
            {
                using (var binaryReader = new BinaryReader(memoryStream, Encoding.Default, true))
                {
                    while (memoryStream.Position < memoryStream.Length)
                    {
                        // Find HIRC section
                        byte[] sectionId = new byte[4];
                        binaryReader.Read(sectionId, 0, 4);

                        if (sectionId[0] == 0x48 && sectionId[1] == 0x49 && sectionId[2] == 0x52 && sectionId[3] == 0x43)
                        {
                            // HIRC section
                            uint hircSectionLength = binaryReader.ReadUInt32();
                            uint objectCount = binaryReader.ReadUInt32();

                            for (int i = 0; i < objectCount; i++)
                            {
                                byte objectType = binaryReader.ReadByte();
                                uint objectLength = binaryReader.ReadUInt32();

                                // Skip object id
                                uint objectId = binaryReader.ReadUInt32();
                                int bytesToSkip = 4;

                                // Parse music tracks
                                if (objectType == 0x0B)
                                {
                                    MusicTrack musicTrack = new MusicTrack();
                                    musicTrack.ObjectId = objectId;

                                    // Skip MIDI flags
                                    memoryStream.Seek(1, SeekOrigin.Current);
                                    bytesToSkip += 1;

                                    // Audio file sources
                                    uint childCount = binaryReader.ReadUInt32();
                                    bytesToSkip += 4;

                                    for (int j = 0; j < childCount; j++)
                                    {
                                        // Skip useless data
                                        ushort sourceType = binaryReader.ReadUInt16();
                                        bytesToSkip += 2;

                                        if (sourceType == 0x01 || sourceType == 0x02)
                                        {
                                            memoryStream.Seek(2, SeekOrigin.Current);
                                            bytesToSkip += 2;
                                        }

                                        uint inclusionType = binaryReader.ReadByte();
                                        bytesToSkip += 1;

                                        // Read ID
                                        uint sourceId = binaryReader.ReadUInt32();
                                        bytesToSkip += 4;

                                        musicTrack.AudioFileIds.Add(sourceId);

                                        if (inclusionType == 0x00)
                                        {
                                            memoryStream.Seek(4, SeekOrigin.Current);
                                            bytesToSkip += 4;
                                        }
                                        else if (inclusionType == 0x01 || inclusionType == 0x02)
                                        {
                                            memoryStream.Seek(4, SeekOrigin.Current);
                                            bytesToSkip += 4;
                                        }

                                        memoryStream.Seek(1, SeekOrigin.Current);
                                        bytesToSkip += 1;
                                    }

                                    // Go to the end of the object
                                    memoryStream.Seek(objectLength - bytesToSkip, SeekOrigin.Current);
                                    wwiseSoundbank.MusicTracks.Add(musicTrack);
                                }
                                else if (objectType == 0x0A) // Parse music segments
                                {
                                    MusicSegment musicSegment = new MusicSegment();
                                    musicSegment.ObjectId = objectId;

                                    // Skip MIDI flags
                                    memoryStream.Seek(1, SeekOrigin.Current);
                                    bytesToSkip += 1;

                                    // Skip sound section
                                    bytesToSkip += SkipSoundStructure(memoryStream, binaryReader);

                                    // Parse children (Music Tracks)
                                    uint childCount = binaryReader.ReadUInt32();
                                    bytesToSkip += 4;

                                    for (int j = 0; j < childCount; j++)
                                    {
                                        uint musTrackId = binaryReader.ReadUInt32();
                                        bytesToSkip += 4;

                                        musicSegment.MusicTrackIds.Add(musTrackId);
                                    }

                                    // Go to the end of the object
                                    memoryStream.Seek(objectLength - bytesToSkip, SeekOrigin.Current);
                                    wwiseSoundbank.MusicSegments.Add(musicSegment);
                                }
                                else if (objectType == 0x0D) // Music Playlist Container
                                {
                                    MusicPlaylistContainer musicPlaylistContainer = new MusicPlaylistContainer();
                                    musicPlaylistContainer.ObjectId = objectId;

                                    // Skip MIDI flags
                                    memoryStream.Seek(1, SeekOrigin.Current);
                                    bytesToSkip += 1;

                                    // Skip sound section
                                    bytesToSkip += SkipSoundStructure(memoryStream, binaryReader);

                                    // Parse child count (Music Segments)
                                    uint musicSegmentChildCount = binaryReader.ReadUInt32();
                                    bytesToSkip += 4;

                                    for (int j = 0; j < musicSegmentChildCount; j++)
                                    {
                                        uint musicSegmentId = binaryReader.ReadUInt32();
                                        bytesToSkip += 4;

                                        musicPlaylistContainer.MusicSegmentIds.Add(musicSegmentId);
                                    }

                                    // Go to the end of the object
                                    memoryStream.Seek(objectLength - bytesToSkip, SeekOrigin.Current);
                                    wwiseSoundbank.MusicPlaylistContainers.Add(musicPlaylistContainer);
                                }
                                else if (objectType == 0x0C) // Music Switch Container
                                {
                                    MusicSwitchContainer musicSwitchContainer = new MusicSwitchContainer();
                                    musicSwitchContainer.ObjectId = objectId;

                                    // Skip MIDI flags
                                    memoryStream.Seek(1, SeekOrigin.Current);

                                    // Skip sound section
                                    SkipSoundStructure(memoryStream, binaryReader);

                                    // Parse child count (Switch/Playlist Containers, or Segments)
                                    uint childCount = binaryReader.ReadUInt32();

                                    for (int j = 0; j < childCount; j++)
                                    {
                                        uint childId = binaryReader.ReadUInt32();
                                        musicSwitchContainer.MusicObjectIds.Add(childId);
                                    }

                                    // Skip until we reach the switch/state group ids
                                    memoryStream.Seek(23, SeekOrigin.Current);

                                    uint stingerCount = binaryReader.ReadUInt32();
                                    memoryStream.Seek(stingerCount * 24, SeekOrigin.Current);

                                    uint transitionCount = binaryReader.ReadUInt32();

                                    for (int j = 0; j < transitionCount; j++)
                                    {
                                        uint sourceCount = binaryReader.ReadUInt32();
                                        memoryStream.Seek(sourceCount * 4, SeekOrigin.Current);

                                        uint destCount = binaryReader.ReadUInt32();
                                        memoryStream.Seek(destCount * 4, SeekOrigin.Current);

                                        memoryStream.Seek(47, SeekOrigin.Current);
                                        byte useTransitionSegment = binaryReader.ReadByte();

                                        if (useTransitionSegment == 0x01)
                                        {
                                            memoryStream.Seek(30, SeekOrigin.Current);
                                        }
                                    }

                                    memoryStream.Seek(1, SeekOrigin.Current);

                                    // Read child switch/state groups
                                    uint switchStateGroupChildrenCount = binaryReader.ReadUInt32();
                                    uint[] childrenIds = new uint[switchStateGroupChildrenCount];

                                    for (int j = 0; j < switchStateGroupChildrenCount; j++)
                                    {
                                        uint switchStateGroupId = binaryReader.ReadUInt32();
                                        childrenIds[j] = switchStateGroupId;
                                    }

                                    for (int j = 0; j < switchStateGroupChildrenCount; j++)
                                    {
                                        byte isStateGroup = binaryReader.ReadByte();
                                        musicSwitchContainer.SwitchOrStateGroupChildren.Add(new Tuple<bool, uint>(isStateGroup == 0x01 ? true : false, childrenIds[j]));
                                    }

                                    // Read paths
                                    uint pathSectionLength = binaryReader.ReadUInt32();
                                    memoryStream.Seek(1, SeekOrigin.Current);
                                    musicSwitchContainer.Paths = ReadPaths(binaryReader, pathSectionLength, musicSwitchContainer.MusicObjectIds.ToArray());

                                    wwiseSoundbank.MusicSwitchContainers.Add(musicSwitchContainer);
                                }
                                else
                                {
                                    memoryStream.Seek(objectLength - 4, SeekOrigin.Current);
                                }
                            }
                        }
                        else
                        {
                            uint sectionLength = binaryReader.ReadUInt32();
                            memoryStream.Seek(sectionLength, SeekOrigin.Current);
                        }
                    }
                }
            }

            return wwiseSoundbank;
        }

        /// <summary>
        /// Skips past a Wwise Soundbank sound structure
        /// </summary>
        /// <param name="memoryStream">soundbank file data memory stream</param>
        /// <param name="binaryReader">memory stream binary reader</param>
        /// <returns>number of bytes skipped</returns>
        private static int SkipSoundStructure(MemoryStream memoryStream, BinaryReader binaryReader)
        {
            memoryStream.Seek(1, SeekOrigin.Current);
            int byteCount = 1;

            byte effCount = binaryReader.ReadByte();
            byteCount += 1;

            if (effCount > 0)
            {
                memoryStream.Seek(1, SeekOrigin.Current);
                byteCount += 1;

                memoryStream.Seek(effCount * 7, SeekOrigin.Current);
                byteCount += effCount * 7;
            }

            memoryStream.Seek(10, SeekOrigin.Current);
            byteCount += 10;

            // Skip parms
            uint parmCount = binaryReader.ReadByte();
            byteCount += 1;

            memoryStream.Seek(parmCount, SeekOrigin.Current);
            byteCount += (int)parmCount;

            memoryStream.Seek(parmCount * 4, SeekOrigin.Current);
            byteCount += (int)parmCount * 4;

            uint parmRndCount = binaryReader.ReadByte();
            byteCount += 1;

            memoryStream.Seek(parmRndCount, SeekOrigin.Current);
            byteCount += (int)parmRndCount;

            memoryStream.Seek(parmRndCount * 4, SeekOrigin.Current);
            byteCount += (int)parmRndCount * 4;

            memoryStream.Seek(parmRndCount * 4, SeekOrigin.Current);
            byteCount += (int)parmRndCount * 4;

            // Skip positioning flags
            byte posFlags = binaryReader.ReadByte();
            byteCount += 1;

            if (posFlags >= 0x02 || posFlags >= 0x03 || posFlags >= 0x20 || posFlags == 0x40)
            {
                memoryStream.Seek(1, SeekOrigin.Current);
                byteCount += 1;

                if (posFlags >= 0x20 || posFlags == 0x40)
                {
                    memoryStream.Seek(5, SeekOrigin.Current);
                    byteCount += 5;

                    uint keyCount = binaryReader.ReadUInt32();
                    byteCount += 4;

                    memoryStream.Seek(keyCount * 16, SeekOrigin.Current);
                    byteCount += (int)keyCount * 16;

                    uint pathCount = binaryReader.ReadUInt32();
                    byteCount += 4;

                    memoryStream.Seek(pathCount * 8, SeekOrigin.Current);
                    byteCount += (int)pathCount * 8;

                    memoryStream.Seek(pathCount * 12, SeekOrigin.Current);
                    byteCount += (int)pathCount * 12;
                }
            }

            // Skip aux flags
            byte auxFlags = binaryReader.ReadByte();
            byteCount += 1;

            if (auxFlags >= 0x08 || auxFlags == 0x10)
            {
                memoryStream.Seek(16, SeekOrigin.Current);
                byteCount += 16;
            }

            memoryStream.Seek(10, SeekOrigin.Current);
            byteCount += 10;

            byte propCount = binaryReader.ReadByte();
            byteCount += 1;
            memoryStream.Seek(propCount * 3, SeekOrigin.Current);
            byteCount += propCount * 3;

            byte stateGroupCount = binaryReader.ReadByte();
            byteCount += 1;

            for (int j = 0; j < stateGroupCount; j++)
            {
                memoryStream.Seek(5, SeekOrigin.Current);
                byteCount += 5;

                byte stateCount = binaryReader.ReadByte();
                byteCount += 1;
                memoryStream.Seek(stateCount * 8, SeekOrigin.Current);
                byteCount += stateCount * 8;
            }

            ushort rtpcCount = binaryReader.ReadUInt16();
            byteCount += 2;

            for (int j = 0; j < rtpcCount; j++)
            {
                memoryStream.Seek(12, SeekOrigin.Current);
                byteCount += 12;

                ushort pointCount = binaryReader.ReadUInt16();
                byteCount += 2;

                memoryStream.Seek(pointCount * 12, SeekOrigin.Current);
                byteCount += pointCount * 12;
            }

            return byteCount;
        }

        /// <summary>
        /// Reads the Paths section in a Music Switch Container
        /// </summary>
        /// <param name="reader">binary reader for the soundbank file data memory stream</param>
        /// <param name="pathsSectionLength">length of the paths section</param>
        /// <param name="childIds">the children music object ids of the Music Switch Container</param>
        /// <returns></returns>
        private static PathNode ReadPaths(BinaryReader reader, uint pathsSectionLength, uint[] childIds)
        {
            // Read section bytes
            var sectionCount = (int)pathsSectionLength / 12;
            var sections = new List<byte[]>(sectionCount);

            while (sectionCount > 0)
            {
                sections.Add(reader.ReadBytes(12));
                sectionCount--;
            }

            // Read root node (index 0)
            return ReadPathElement(sections, childIds, 0) as PathNode;
        }

        /// <summary>
        /// Reads a path element in a path section of a Music Switch Container
        /// </summary>
        /// <param name="sections">sections in the paths section</param>
        /// <param name="childIds">the children music object ids of the Music Switch Container</param>
        /// <param name="childrenStartAt">the beginning position of the node's children in all nodes</param>
        /// <returns>a PathElement object</returns>
        private static PathElement ReadPathElement(List<byte[]> sections, uint[] childIds, uint childrenStartAt)
        {
            var section = sections[(int)childrenStartAt];
            var childId = BitConverter.ToUInt32(section, 4);

            if (childIds.Contains(childId))
            {
                // childId is a Music Object, reached the end
                var endpoint = new PathEndpoint();
                endpoint.FromStateOrSwitchId = BitConverter.ToUInt32(section, 0);
                endpoint.MusicObjectId = childId;

                return endpoint;
            }
            else
            {
                // childId is not an Music Object, reached a node
                var node = new PathNode();
                node.FromStateOrSwitchId = BitConverter.ToUInt32(section, 0);
                node.ChildrenStartAtIndex = BitConverter.ToUInt16(section, 4);
                node.ChildCount = BitConverter.ToUInt16(section, 6);
                node.Children = new PathElement[node.ChildCount];

                for (uint i = 0; i < node.ChildCount; i++)
                {
                    node.Children[i] = ReadPathElement(sections, childIds, node.ChildrenStartAtIndex + i);
                }

                return node;
            }
        }
    }
}
