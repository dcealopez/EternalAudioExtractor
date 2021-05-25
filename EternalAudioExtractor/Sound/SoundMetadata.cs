using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EternalAudioExtractor.Sound
{
    /// <summary>
    /// Sound metadata class
    /// </summary>
    public class SoundMetadata
    {
        /// <summary>
        /// Sound event list
        /// </summary>
        public List<SoundEvent> SoundEvents = new List<SoundEvent>();

        /// <summary>
        /// Switch groups
        /// </summary>
        public List<ElementGroup> SwitchGroups = new List<ElementGroup>();

        /// <summary>
        /// State groups
        /// </summary>
        public List<ElementGroup> StateGroups = new List<ElementGroup>();

        /// <summary>
        /// Reads a DOOM Eternal sound metadata file
        /// </summary>
        /// <param name="path">path to the sound metadata file</param>
        /// <returns>a SoundMetadata object with the deserialized data</returns>
        public static SoundMetadata ReadFrom(string path)
        {
            var soundMetadata = new SoundMetadata();

            // Read all the sound events with their audio ids and store them first
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream, Encoding.Default, true))
                {
                    // Skip all the way to the event section
                    // Skip version
                    fileStream.Seek(4, SeekOrigin.Begin);

                    // Skip pck section
                    int pckCount = binaryReader.ReadInt32();

                    for (int i = 0; i < pckCount; i++)
                    {
                        uint pckNameLength = binaryReader.ReadUInt32();
                        fileStream.Seek(pckNameLength, SeekOrigin.Current);
                        fileStream.Seek(4, SeekOrigin.Current);
                    }

                    // Skip snd section
                    int sndCount = binaryReader.ReadInt32();

                    for (int i = 0; i < sndCount; i++)
                    {
                        uint sndNameLength = binaryReader.ReadUInt32();
                        fileStream.Seek(sndNameLength, SeekOrigin.Current);
                        uint sndFileCount = binaryReader.ReadUInt32();

                        for (int j = 0; j < sndFileCount; j++)
                        {
                            fileStream.Seek(4, SeekOrigin.Current);
                            uint unkCount = binaryReader.ReadUInt32();
                            fileStream.Seek(4 * unkCount, SeekOrigin.Current);
                        }
                    }

                    // Skip bnk section
                    int bnkCount = binaryReader.ReadInt32();

                    for (int i = 0; i < bnkCount; i++)
                    {
                        uint bnkNameLength = binaryReader.ReadUInt32();
                        fileStream.Seek(bnkNameLength, SeekOrigin.Current);
                        fileStream.Seek(4, SeekOrigin.Current);
                    }

                    // Read effect section
                    uint effCount = binaryReader.ReadUInt32();

                    for (int i = 0; i < effCount; i++)
                    {
                        uint effId = binaryReader.ReadUInt32();
                        uint effNameLength = binaryReader.ReadUInt32();
                        byte[] effNameBytes = new byte[effNameLength];
                        binaryReader.Read(effNameBytes, 0, (int)effNameLength);
                        string effName = Encoding.UTF8.GetString(effNameBytes);
                    }

                    // Read parm section
                    uint parmCount = binaryReader.ReadUInt32();

                    for (int i = 0; i < parmCount; i++)
                    {
                        uint parmId = binaryReader.ReadUInt32();
                        uint parmNameLength = binaryReader.ReadUInt32();
                        byte[] parmNameBytes = new byte[parmNameLength];
                        binaryReader.Read(parmNameBytes, 0, (int)parmNameLength);
                        string parmName = Encoding.UTF8.GetString(parmNameBytes);
                    }

                    // Skip switch group section
                    uint switchGroupCount = binaryReader.ReadUInt32();

                    for (int i = 0; i < switchGroupCount; i++)
                    {
                        ElementGroup switchGroup = new ElementGroup();

                        uint switchGroupId = binaryReader.ReadUInt32();
                        uint switchGroupNameLength = binaryReader.ReadUInt32();
                        byte[] switchGroupNameBytes = new byte[switchGroupNameLength];
                        binaryReader.Read(switchGroupNameBytes, 0, (int)switchGroupNameLength);
                        string switchGroupName = Encoding.UTF8.GetString(switchGroupNameBytes);
                        uint switchGroupChildrenCount = binaryReader.ReadUInt32();

                        switchGroup.GroupId = switchGroupId;
                        switchGroup.Name = switchGroupName;

                        for (int j = 0; j < switchGroupChildrenCount; j++)
                        {
                            Element switchGroupChildren = new Element();

                            uint switchGroupChildrenId = binaryReader.ReadUInt32();
                            uint switchGroupChildrenNameLength = binaryReader.ReadUInt32();
                            byte[] switchGroupChildrenNameBytes = new byte[switchGroupChildrenNameLength];
                            binaryReader.Read(switchGroupChildrenNameBytes, 0, (int)switchGroupChildrenNameLength);
                            string switchGroupChildrenName = Encoding.UTF8.GetString(switchGroupChildrenNameBytes);

                            switchGroupChildren.Id = switchGroupChildrenId;
                            switchGroupChildren.Name = switchGroupChildrenName;
                            switchGroup.Children.Add(switchGroupChildren);
                        }

                        soundMetadata.SwitchGroups.Add(switchGroup);
                    }

                    // Skip switch state section
                    int switchStateCount = binaryReader.ReadInt32();

                    for (int i = 0; i < switchStateCount; i++)
                    {
                        ElementGroup stateGroup = new ElementGroup();

                        uint switchStateId = binaryReader.ReadUInt32();
                        uint switchStateNameLength = binaryReader.ReadUInt32();
                        byte[] switchStateNameBytes = new byte[switchStateNameLength];
                        binaryReader.Read(switchStateNameBytes, 0, (int)switchStateNameLength);
                        string switchStateName = Encoding.UTF8.GetString(switchStateNameBytes);
                        uint switchStateChildrenCount = binaryReader.ReadUInt32();

                        stateGroup.GroupId = switchStateId;
                        stateGroup.Name = switchStateName;

                        for (int j = 0; j < switchStateChildrenCount; j++)
                        {
                            Element stateGroupChildren = new Element();

                            uint switchStateChildrenId = binaryReader.ReadUInt32();
                            uint switchStateChildrenNameLength = binaryReader.ReadUInt32();
                            byte[] switchStateChildrenNameBytes = new byte[switchStateChildrenNameLength];
                            binaryReader.Read(switchStateChildrenNameBytes, 0, (int)switchStateChildrenNameLength);
                            string switchStateChildrenName = Encoding.UTF8.GetString(switchStateChildrenNameBytes);

                            stateGroupChildren.Id = switchStateChildrenId;
                            stateGroupChildren.Name = switchStateChildrenName;
                            stateGroup.Children.Add(stateGroupChildren);
                        }

                        soundMetadata.StateGroups.Add(stateGroup);
                    }

                    // Skip event path node section
                    uint eventPathNodeSectionLength = binaryReader.ReadUInt32();
                    fileStream.Seek(eventPathNodeSectionLength, SeekOrigin.Current);

                    // Read event section
                    int eventCount = binaryReader.ReadInt32();

                    for (int i = 0; i < eventCount; i++)
                    {
                        binaryReader.ReadUInt32();
                        uint eventNameLength = binaryReader.ReadUInt32();
                        byte[] nameBytes = new byte[eventNameLength];
                        binaryReader.Read(nameBytes, 0, (int)eventNameLength);
                        string eventName = Encoding.UTF8.GetString(nameBytes);
                        binaryReader.ReadSingle();
                        binaryReader.ReadUInt16();
                        binaryReader.ReadUInt32();
                        binaryReader.ReadUInt32();
                        uint affectedSoundCount = binaryReader.ReadUInt32();

                        List<uint> soundIds = new List<uint>();

                        for (int j = 0; j < affectedSoundCount; j++)
                        {
                            soundIds.Add(binaryReader.ReadUInt32());
                        }

                        uint soundBankIdsCount = binaryReader.ReadUInt32();
                        fileStream.Seek(soundBankIdsCount * 4, SeekOrigin.Current);

                        uint pathNodeOffsetCount = binaryReader.ReadUInt32();
                        fileStream.Seek(pathNodeOffsetCount * 4, SeekOrigin.Current);

                        soundMetadata.SoundEvents.Add(new SoundEvent()
                        {
                            Name = eventName,
                            SoundIds = soundIds
                        });
                    }
                }
            }

            return soundMetadata;
        }
    }
}
