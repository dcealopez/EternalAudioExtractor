using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EternalAudioExtractor.Sound;
using EternalAudioExtractor.Wwise;
using EternalAudioExtractor.Wwise.Soundbank;

namespace EternalAudioExtractor
{
    /// <summary>
    /// Eternal Audio Extractor
    /// </summary>
    public class EternalAudioExtractor
    {
        /// <summary>
        /// Sound metadata
        /// </summary>
        public static SoundMetadata SoundMetadata;

        /// <summary>
        /// Music Track list
        /// </summary>
        public static List<MusicTrack> musicTracks = new List<MusicTrack>();

        /// <summary>
        /// Music Segment list
        /// </summary>
        public static List<MusicSegment> musicSegments = new List<MusicSegment>();

        /// <summary>
        /// Music Playlist Container list
        /// </summary>
        public static List<MusicPlaylistContainer> musicPlaylistContainers = new List<MusicPlaylistContainer>();

        /// <summary>
        /// Music Switch Container list
        /// </summary>
        public static List<MusicSwitchContainer> musicSwitchContainers = new List<MusicSwitchContainer>();

        /// <summary>
        /// Music track name dictionary, linked to their audio file ids
        /// </summary>
        public static Dictionary<string, uint> MusicNameDict = new Dictionary<string, uint>();

        /// <summary>
        /// Auto Convert .wem to .ogg?
        /// </summary>
        public static bool AutoConvert;

        /// <summary>
        /// Extract unused (unnamed) sounds?
        /// </summary>
        public static bool ExtractUnused;

        /// <summary>
        /// Sound metadata file name
        /// </summary>
        public const string SoundMetadataFileName = "soundmetadata.bin";

        /// <summary>
        /// Pcb file for .wem to .ogg conversion
        /// </summary>
        public const string PcbFileName = "packed_codebooks_aoTuV_603.bin";

        /// <summary>
        /// Ww2Ogg utility executable file name
        /// </summary>
        public const string Ww2OggExeFileName = "ww2ogg.exe";

        /// <summary>
        /// Revorb utility executable file name
        /// </summary>
        public const string RevorbExeFileName = "revorb.exe";

        /// <summary>
        /// Relative path to where the utilities are located
        /// </summary>
        public const string UtilsRelativePath = "utils";

        /// <summary>
        /// Entry point
        /// </summary>
        public static int Main(string[] args)
        {
            Console.CursorVisible = false;

            // Parse command line arguments
            if (args.Length < 3)
            {
                Console.WriteLine("USAGE: EternalAudioExtractor.exe <path to the .snd file to extract> <path to the directory containing the 'soundmetadata.bin' file and .pck files> <output folder> [options]");
                Console.WriteLine("OPTIONS:");
                Console.WriteLine("\t-c - Converts .wem files to .ogg automatically (will increase extraction time)");
                Console.WriteLine("\t-u - Extract unused sounds (the name of these sound files will be their sound id)");
                return 0;
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine("| EternalAudioExtractor v1.0 by proteh |");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // Optional arguments
            if (args.Length > 3)
            {
                for (int i = 3; i < args.Length; i++)
                {
                    if (args[i] == "-c")
                    {
                        AutoConvert = true;
                    }
                    else if (args[i] == "-u")
                    {
                        ExtractUnused = true;
                    }
                }
            }

            // Path to utilities
            string ww2OggPath = string.Empty;
            string revorbPath = string.Empty;
            string pcbFilePath = string.Empty;

            if (AutoConvert)
            {
                ww2OggPath = Path.Combine($".{Path.DirectorySeparatorChar}", UtilsRelativePath, Ww2OggExeFileName);

                if (!File.Exists(ww2OggPath))
                {
                    Console.Error.WriteLine($"Can't find \"{ww2OggPath}\"");
                    return 1;
                }

                revorbPath = Path.Combine(UtilsRelativePath, RevorbExeFileName);

                if (!File.Exists(revorbPath))
                {
                    Console.Error.WriteLine($"Can't find \"{revorbPath}\"");
                    return 1;
                }

                pcbFilePath = Path.Combine(UtilsRelativePath, PcbFileName);

                if (!File.Exists(pcbFilePath))
                {
                    Console.Error.WriteLine($"Can't find \"{pcbFilePath}\"");
                    return 1;
                }
            }

            // Prepare input data
            var sndFilePath = args[0];
            var gameSoundFilesDirectory = args[1];
            var outputDirectory = args[2];
            var sndOutputDirectory = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sndFilePath));
            string metadataFilePath = string.Empty;

            if (!File.Exists(sndFilePath))
            {
                Console.Error.WriteLine($"Can't find \"{sndFilePath}\"");
                return 1;
            }

            try
            {
                // Read the sound metadata
                metadataFilePath = Path.Combine(gameSoundFilesDirectory, SoundMetadataFileName);

                if (!File.Exists(metadataFilePath))
                {
                    Console.Error.WriteLine($"Can't find \"{SoundMetadataFileName}\" in \"{gameSoundFilesDirectory}\"");
                    return 1;
                }

                Console.WriteLine($"- Snd file path: {sndFilePath}");
                Console.WriteLine($"- Game sound files directory: {gameSoundFilesDirectory}");
                Console.WriteLine($"- Metadata file path: {metadataFilePath}");
                Console.WriteLine($"- Output directory: {sndOutputDirectory}");
                Console.WriteLine($"- Convert .wem to .ogg: {AutoConvert}");
                Console.WriteLine($"- Extract unused: {ExtractUnused}");
                Console.WriteLine();

                SoundMetadata = SoundMetadata.ReadFrom(metadataFilePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error while trying to read the sound metadata");
                Console.Error.WriteLine(ex);
                return 1;
            }

            // Read the .pck files
            List<WwisePackage> wwisePackages = new List<WwisePackage>();

            try
            {
                var gameSoundFilesDirectoryInfo = new DirectoryInfo(gameSoundFilesDirectory);

                foreach (var file in gameSoundFilesDirectoryInfo.EnumerateFiles("*.pck", SearchOption.TopDirectoryOnly))
                {
                    wwisePackages.Add(WwisePackage.ReadFrom(file.FullName));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }

            // Read the soundbank files extracted from the .pck files
            foreach (var wwisePackage in wwisePackages)
            {
                foreach (var soundbankData in wwisePackage.SoundbankFilesData)
                {
                    var soundbankFile = WwiseSoundbank.Read(soundbankData);
                    musicTracks.AddRange(soundbankFile.MusicTracks);
                    musicSegments.AddRange(soundbankFile.MusicSegments);
                    musicPlaylistContainers.AddRange(soundbankFile.MusicPlaylistContainers);
                    musicSwitchContainers.AddRange(soundbankFile.MusicSwitchContainers);
                }
            }

            // Build the music names dictionary with all the extracted metadata
            BuildMusicNameDict();

            try
            {
                // Create the output directory for the .snd file
                if (!Directory.Exists(sndOutputDirectory))
                {
                    Directory.CreateDirectory(sndOutputDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error while creating output directory");
                Console.Error.WriteLine(ex);
                return 1;
            }

            // Read the .snd file to extract the sound files from it
            Stopwatch stopwatch = new Stopwatch();
            float percentageProgress = 0;
            uint extractedFiles = 0;
            uint convertedFiles = 0;
            uint unusedFileCount = 0;

            using (var fileStream = new FileStream(sndFilePath, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream, Encoding.Default, true))
                {
                    stopwatch.Start();

                    // Read the info and the header sizes
                    fileStream.Seek(4, SeekOrigin.Begin);

                    uint infoSize = binaryReader.ReadUInt32();
                    uint headerSize = binaryReader.ReadUInt32();

                    fileStream.Seek(headerSize, SeekOrigin.Current);

                    // Loop through all the sound info
                    uint soundInfoCount = (infoSize - headerSize) / 32;

                    for (int i = 0; i < soundInfoCount; i++)
                    {
                        Console.Write($"\r* Extracting... [{(int)Math.Ceiling(percentageProgress)}%]\t\t");
                        percentageProgress = ((float)extractedFiles / (float)soundInfoCount) * 100.0f;

                        fileStream.Seek(8, SeekOrigin.Current);
                        uint soundId = binaryReader.ReadUInt32();
                        uint encodedSize = binaryReader.ReadUInt32();
                        uint soundDataOffset = binaryReader.ReadUInt32();
                        uint decodedSize = binaryReader.ReadUInt32();
                        ushort soundFormat = binaryReader.ReadUInt16();
                        long currentPos = fileStream.Position;

                        // Get the sound data
                        fileStream.Seek(soundDataOffset, SeekOrigin.Begin);
                        byte[] soundData = new byte[encodedSize];
                        binaryReader.Read(soundData, 0, (int)encodedSize);

                        // Empty files are considered unused
                        if (soundData.Length == 0)
                        {
                            unusedFileCount++;

                            if (!ExtractUnused)
                            {
                                fileStream.Seek(currentPos + 6, SeekOrigin.Begin);
                                continue;
                            }
                        }

                        // Attempt to find the event name for this sound, if not, use the sound id as the name
                        bool idFound = false;
                        string fileName = soundId.ToString();

                        foreach (var soundEvent in SoundMetadata.SoundEvents)
                        {
                            string soundEventName = Regex.Replace(soundEvent.Name.ToLowerInvariant(), "(^play_(vo_)?)|(^stop_)|([_]ghost([1-9]*)?(.*))", "");
                            int soundEventIndex = soundEvent.SoundIds.IndexOf(soundId);

                            if (soundEventIndex != -1)
                            {
                                fileName = $"{soundEventName}";

                                if (soundEvent.SoundIds.Count > 1)
                                {
                                    fileName += $"_{soundEventIndex}";
                                }

                                idFound = true;
                                break;
                            }
                        }

                        // Maybe it's a music track, try to find the name in the music names dict
                        if (!idFound)
                        {
                            var musicName = MusicNameDict.FirstOrDefault(music => music.Value == soundId);

                            if (musicName.Key != null)
                            {
                                fileName = musicName.Key;
                                idFound = true;
                            }
                        }

                        // Don't extract unnamed sounds if not specified
                        if (!idFound)
                        {
                            unusedFileCount++;

                            if (!ExtractUnused)
                            {
                                fileStream.Seek(currentPos + 6, SeekOrigin.Begin);
                                continue;
                            }
                        }

                        // Add the sound id at the end of the filename
                        if (fileName != soundId.ToString())
                        {
                            fileName += $"_id#{soundId}";
                        }

                        // Determine the file extension by its format
                        var formatExtension = soundFormat == 2 ? ".opus" : ".wem";

                        // Write the file
                        var outputPath = Path.Combine(sndOutputDirectory, fileName + formatExtension);
                        File.WriteAllBytes(outputPath, soundData);
                        extractedFiles++;

                        // Convert to .wem to .ogg if specified
                        if (AutoConvert && soundFormat != 2)
                        {
                            var convertedOggPath = Path.Combine(sndOutputDirectory, fileName + ".ogg");
                            var ww2OggProcess = new Process();
                            ww2OggProcess.StartInfo.UseShellExecute = false;
                            ww2OggProcess.StartInfo.FileName = ww2OggPath;
                            ww2OggProcess.StartInfo.Arguments = $"\"{outputPath}\" -o \"{convertedOggPath}\" --pcb \"{pcbFilePath}\"";
                            ww2OggProcess.StartInfo.RedirectStandardError = true;
                            ww2OggProcess.StartInfo.RedirectStandardOutput = true;
                            ww2OggProcess.StartInfo.CreateNoWindow = false;
                            ww2OggProcess.Start();
                            ww2OggProcess.WaitForExit();

                            // Revorb the .ogg file
                            var revorbProcess = new Process();
                            revorbProcess.StartInfo.UseShellExecute = false;
                            revorbProcess.StartInfo.FileName = revorbPath;
                            revorbProcess.StartInfo.Arguments = $"\"{convertedOggPath}\"";
                            revorbProcess.StartInfo.RedirectStandardError = true;
                            revorbProcess.StartInfo.RedirectStandardOutput = true;
                            revorbProcess.StartInfo.CreateNoWindow = false;
                            revorbProcess.Start();
                            revorbProcess.WaitForExit();

                            // Delete the .wem file
                            File.Delete(outputPath);

                            convertedFiles++;
                        }

                        // Continue with the next file
                        fileStream.Seek(currentPos + 6, SeekOrigin.Begin);
                    }

                    stopwatch.Stop();
                }
            }

            Console.WriteLine($"\r* Extracting... [100%]\t\t");
            Console.WriteLine();
            Console.WriteLine($"Finished in {stopwatch.Elapsed} seconds.");
            Console.Write($"{extractedFiles} sound files were extracted");

            if (ExtractUnused)
            {
                if (unusedFileCount > 0)
                {
                    Console.Write($" ({unusedFileCount} unused)");
                }

                Console.WriteLine();
            }
            else
            {
                if (unusedFileCount > 0)
                {
                    Console.Write($" ({unusedFileCount} unused files were skipped)");
                }

                Console.WriteLine();
            }

            if (AutoConvert && convertedFiles > 0)
            {
                Console.WriteLine($"{convertedFiles} extracted sound files were converted to .ogg");
            }

            return 0;
        }

        /// <summary>
        /// Builds the music names directory, linking them to audio file ids
        /// </summary>
        public static void BuildMusicNameDict()
        {
            List<MusicSwitchContainer> parentMusicSwitchContainers = new List<MusicSwitchContainer>();

            // Build the parent music switch container list
            foreach (var switchContainer in musicSwitchContainers)
            {
                bool isParent = true;

                foreach (var otherSwitchContainer in musicSwitchContainers)
                {
                    if (switchContainer == otherSwitchContainer)
                    {
                        continue;
                    }

                    if (otherSwitchContainer.MusicObjectIds.Contains(switchContainer.ObjectId))
                    {
                        isParent = false;
                    }
                }

                if (isParent)
                {
                    parentMusicSwitchContainers.Add(switchContainer);
                }
            }

            // Loop through all the parent music switch containers and look through their paths
            foreach (var switchContainer in parentMusicSwitchContainers)
            {
                var switchOrStateGroup = switchContainer.SwitchOrStateGroupChildren.FirstOrDefault();

                if (switchOrStateGroup != null)
                {
                    ElementGroup switchOrStateElementGroup = null;

                    if (switchOrStateGroup.Item1) // Skip parent state groups
                    {
                        continue;
                    }
                    else
                    {
                        switchOrStateElementGroup = SoundMetadata.SwitchGroups.FirstOrDefault(sGroup => sGroup.GroupId == switchOrStateGroup.Item2);
                    }
                }

                TraverseMusicSwitchContainerPaths(string.Empty, switchContainer);
            }
        }

        /// <summary>
        /// Traverses the paths inside a Music Switch Container object
        /// to determine the music file names and to link them with the audio file ids
        /// </summary>
        public static void TraverseMusicSwitchContainerPaths(string musicNameSoFar, MusicSwitchContainer musicSwitchContainer)
        {
            // Keep track of pathed objects so we can find the ones that are not pathed
            List<uint> pathedMusicObjectIds = new List<uint>();

            foreach (var pathElement in musicSwitchContainer.Paths.Children)
            {
                if (pathElement is PathEndpoint)
                {
                    string musicName = musicNameSoFar;
                    uint switchOrStateId = (pathElement as PathEndpoint).FromStateOrSwitchId;
                    uint musicObjectId = (pathElement as PathEndpoint).MusicObjectId;

                    ElementGroup switchOrStateElementGroup = null;
                    switchOrStateElementGroup = SoundMetadata.SwitchGroups.FirstOrDefault(e => e.Children.FirstOrDefault(c => c.Id == switchOrStateId) != null);

                    if (switchOrStateElementGroup == null)
                    {
                        switchOrStateElementGroup = SoundMetadata.StateGroups.FirstOrDefault(e => e.Children.FirstOrDefault(c => c.Id == switchOrStateId) != null);

                        if (switchOrStateElementGroup == null)
                        {
                            continue;
                        }
                    }

                    var switchOrStateElement = switchOrStateElementGroup.Children.FirstOrDefault(c => c.Id == switchOrStateId);

                    if (musicName != string.Empty)
                    {
                        musicName += "_";
                    }

                    musicName += switchOrStateElement.Name;

                    // Now, we need to traverse the music objects referenced in the paths
                    // until we reach a music track object
                    HircObject musicObject = musicSwitchContainers.FirstOrDefault(obj => obj.ObjectId == musicObjectId);
                    pathedMusicObjectIds.Add(musicObjectId);

                    if (musicObject != null)
                    {
                        // This is a Music Switch Container
                        // Do recursion and traverse its paths
                        TraverseMusicSwitchContainerPaths(musicName, (MusicSwitchContainer)musicObject);
                    }
                    else
                    {
                        musicObject = musicPlaylistContainers.FirstOrDefault(obj => obj.ObjectId == musicObjectId);

                        if (musicObject != null)
                        {
                            // This is a music playlist container, containing music segments
                            var musicPlaylistContainer = musicObject as MusicPlaylistContainer;

                            for (int i = 0; i < musicPlaylistContainer.MusicSegmentIds.Count; i++)
                            {
                                var musicSegment = musicSegments.FirstOrDefault(seg => seg.ObjectId == musicPlaylistContainer.MusicSegmentIds[i]);

                                if (musicSegment == null)
                                {
                                    continue;
                                }

                                for (int j = 0; j < musicSegment.MusicTrackIds.Count; j++)
                                {
                                    var musicTrack = musicTracks.FirstOrDefault(track => track.ObjectId == musicSegment.MusicTrackIds[j]);

                                    if (musicTrack == null)
                                    {
                                        continue;
                                    }

                                    for (int k = 0; k < musicTrack.AudioFileIds.Count; k++)
                                    {
                                        string finalMusicName = musicName;

                                        if (musicPlaylistContainer.MusicSegmentIds.Count > 1)
                                        {
                                            finalMusicName += $"_{i}";
                                        }

                                        if (musicSegment.MusicTrackIds.Count > 1)
                                        {
                                            finalMusicName += $"_{j}";
                                        }

                                        if (musicTrack.AudioFileIds.Count > 1)
                                        {
                                            finalMusicName += $"_{k}";
                                        }

                                        if (MusicNameDict.ContainsKey(finalMusicName))
                                        {
                                            int underscoreCount = finalMusicName.Count(c => c == '_');
                                            int lastUnderscoreIndex = finalMusicName.LastIndexOf('_');
                                            string nameWithLastIndex = MusicNameDict.Keys.LastOrDefault(name => name.Count(c => c == '_') == underscoreCount && name.StartsWith(finalMusicName.Remove(lastUnderscoreIndex + 1)));
                                            int lastIndex = 0;

                                            if (int.TryParse(nameWithLastIndex.Split('_')[underscoreCount], out lastIndex))
                                            {
                                                finalMusicName = nameWithLastIndex.Remove(lastUnderscoreIndex + 1, lastIndex.ToString().Count(char.IsDigit)) + (lastIndex + 1);
                                            }
                                            else
                                            {
                                                var currentValue = MusicNameDict[finalMusicName];
                                                MusicNameDict.Remove(finalMusicName);
                                                MusicNameDict.Add($"{finalMusicName}_0", currentValue);
                                                finalMusicName = $"{finalMusicName}_1";
                                            }
                                        }

                                        MusicNameDict.Add(finalMusicName, musicTrack.AudioFileIds[k]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            musicObject = musicSegments.FirstOrDefault(obj => obj.ObjectId == musicObjectId);

                            if (musicObject != null)
                            {
                                // This is a music segment, containing music tracks
                                var musicSegment = musicObject as MusicSegment;

                                if (musicSegment == null)
                                {
                                    continue;
                                }

                                for (int j = 0; j < musicSegment.MusicTrackIds.Count; j++)
                                {
                                    var musicTrack = musicTracks.FirstOrDefault(track => track.ObjectId == musicSegment.MusicTrackIds[j]);

                                    if (musicTrack == null)
                                    {
                                        continue;
                                    }

                                    for (int k = 0; k < musicTrack.AudioFileIds.Count; k++)
                                    {
                                        string finalMusicName = musicName;

                                        if (musicSegment.MusicTrackIds.Count > 1)
                                        {
                                            finalMusicName += $"_{j}";
                                        }

                                        if (musicTrack.AudioFileIds.Count > 1)
                                        {
                                            finalMusicName += $"_{k}";
                                        }

                                        if (MusicNameDict.ContainsKey(finalMusicName))
                                        {
                                            int underscoreCount = finalMusicName.Count(c => c == '_');
                                            int lastUnderscoreIndex = finalMusicName.LastIndexOf('_');
                                            string nameWithLastIndex = MusicNameDict.Keys.LastOrDefault(name => name.Count(c => c == '_') == underscoreCount && name.StartsWith(finalMusicName.Remove(lastUnderscoreIndex + 1)));
                                            int lastIndex = 0;

                                            if (int.TryParse(nameWithLastIndex.Split('_')[underscoreCount], out lastIndex))
                                            {
                                                finalMusicName = nameWithLastIndex.Remove(lastUnderscoreIndex + 1, lastIndex.ToString().Count(char.IsDigit)) + (lastIndex + 1);
                                            }
                                            else
                                            {
                                                var currentValue = MusicNameDict[finalMusicName];
                                                MusicNameDict.Remove(finalMusicName);
                                                MusicNameDict.Add($"{finalMusicName}_0", currentValue);
                                                finalMusicName = $"{finalMusicName}_1";
                                            }
                                        }

                                        MusicNameDict.Add(finalMusicName, musicTrack.AudioFileIds[k]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Handle non-pathed music objects
            foreach (var childrenMusicObjectId in musicSwitchContainer.MusicObjectIds)
            {
                if (!pathedMusicObjectIds.Contains(childrenMusicObjectId))
                {
                    string musicName = musicNameSoFar;

                    // Now, we need to traverse the music objects referenced in the paths
                    // until we reach a music track object
                    HircObject musicObject = musicSwitchContainers.FirstOrDefault(obj => obj.ObjectId == childrenMusicObjectId);

                    if (musicObject != null)
                    {
                        // This is a Music Switch Container
                        // Do recursion and traverse its paths
                        TraverseMusicSwitchContainerPaths(musicName, (MusicSwitchContainer)musicObject);
                    }
                    else
                    {
                        musicObject = musicPlaylistContainers.FirstOrDefault(obj => obj.ObjectId == childrenMusicObjectId);

                        if (musicObject != null)
                        {
                            // This is a music playlist container, containing music segments
                            var musicPlaylistContainer = musicObject as MusicPlaylistContainer;

                            for (int i = 0; i < musicPlaylistContainer.MusicSegmentIds.Count; i++)
                            {
                                var musicSegment = musicSegments.FirstOrDefault(seg => seg.ObjectId == musicPlaylistContainer.MusicSegmentIds[i]);

                                if (musicSegment == null)
                                {
                                    continue;
                                }

                                for (int j = 0; j < musicSegment.MusicTrackIds.Count; j++)
                                {
                                    var musicTrack = musicTracks.FirstOrDefault(track => track.ObjectId == musicSegment.MusicTrackIds[j]);

                                    if (musicTrack == null)
                                    {
                                        continue;
                                    }

                                    for (int k = 0; k < musicTrack.AudioFileIds.Count; k++)
                                    {
                                        string finalMusicName = musicName;

                                        if (musicPlaylistContainer.MusicSegmentIds.Count > 1)
                                        {
                                            finalMusicName += $"_{i}";
                                        }

                                        if (musicSegment.MusicTrackIds.Count > 1)
                                        {
                                            finalMusicName += $"_{j}";
                                        }

                                        if (musicTrack.AudioFileIds.Count > 1)
                                        {
                                            finalMusicName += $"_{k}";
                                        }

                                        if (MusicNameDict.ContainsKey(finalMusicName))
                                        {
                                            int underscoreCount = finalMusicName.Count(c => c == '_');
                                            int lastUnderscoreIndex = finalMusicName.LastIndexOf('_');
                                            string nameWithLastIndex = MusicNameDict.Keys.LastOrDefault(name => name.Count(c => c == '_') == underscoreCount && name.StartsWith(finalMusicName.Remove(lastUnderscoreIndex + 1)));
                                            int lastIndex = 0;

                                            if (int.TryParse(nameWithLastIndex.Split('_')[underscoreCount], out lastIndex))
                                            {
                                                finalMusicName = nameWithLastIndex.Remove(lastUnderscoreIndex + 1, lastIndex.ToString().Count(char.IsDigit)) + (lastIndex + 1);
                                            }
                                            else
                                            {
                                                var currentValue = MusicNameDict[finalMusicName];
                                                MusicNameDict.Remove(finalMusicName);
                                                MusicNameDict.Add($"{finalMusicName}_0", currentValue);
                                                finalMusicName = $"{finalMusicName}_1";
                                            }
                                        }

                                        MusicNameDict.Add(finalMusicName, musicTrack.AudioFileIds[k]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            musicObject = musicSegments.FirstOrDefault(obj => obj.ObjectId == childrenMusicObjectId);

                            if (musicObject != null)
                            {
                                // This is a music segment, containing music tracks
                                var musicSegment = musicObject as MusicSegment;

                                if (musicSegment == null)
                                {
                                    continue;
                                }

                                for (int j = 0; j < musicSegment.MusicTrackIds.Count; j++)
                                {
                                    var musicTrack = musicTracks.FirstOrDefault(track => track.ObjectId == musicSegment.MusicTrackIds[j]);

                                    if (musicTrack == null)
                                    {
                                        continue;
                                    }

                                    for (int k = 0; k < musicTrack.AudioFileIds.Count; k++)
                                    {
                                        string finalMusicName = musicName;

                                        if (musicSegment.MusicTrackIds.Count > 1)
                                        {
                                            finalMusicName += $"_{j}";
                                        }

                                        if (musicTrack.AudioFileIds.Count > 1)
                                        {
                                            finalMusicName += $"_{k}";
                                        }

                                        if (MusicNameDict.ContainsKey(finalMusicName))
                                        {
                                            int underscoreCount = finalMusicName.Count(c => c == '_');
                                            int lastUnderscoreIndex = finalMusicName.LastIndexOf('_');
                                            string nameWithLastIndex = MusicNameDict.Keys.LastOrDefault(name => name.Count(c => c == '_') == underscoreCount && name.StartsWith(finalMusicName.Remove(lastUnderscoreIndex + 1)));
                                            int lastIndex = 0;

                                            if (int.TryParse(nameWithLastIndex.Split('_')[underscoreCount], out lastIndex))
                                            {
                                                finalMusicName = nameWithLastIndex.Remove(lastUnderscoreIndex + 1, lastIndex.ToString().Count(char.IsDigit)) + (lastIndex + 1);
                                            }
                                            else
                                            {
                                                var currentValue = MusicNameDict[finalMusicName];
                                                MusicNameDict.Remove(finalMusicName);
                                                MusicNameDict.Add($"{finalMusicName}_0", currentValue);
                                                finalMusicName = $"{finalMusicName}_1";
                                            }
                                        }

                                        MusicNameDict.Add(finalMusicName, musicTrack.AudioFileIds[k]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
