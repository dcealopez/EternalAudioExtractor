using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EternalAudioExtractor.Wwise
{
    /// <summary>
    /// Wwise Package class
    /// </summary>
    public class WwisePackage
    {
        /// <summary>
        /// Soundbank files extracted from the packages
        /// </summary>
        public List<byte[]> SoundbankFilesData = new List<byte[]>();

        /// <summary>
        /// Reads the Soundbanks contained inside a .pck file
        /// </summary>
        /// <param name="path">path to the .pck file</param>
        /// <returns>a WwisePackage object</returns>
        public static WwisePackage ReadFrom(string path)
        {
            var wwisePackage = new WwisePackage();

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream, Encoding.Default, true))
                {
                    // Skip to header
                    fileStream.Seek(8, SeekOrigin.Begin);

                    // Skip to soundbank section
                    fileStream.Seek(4, SeekOrigin.Current);
                    uint pathSectionLength = binaryReader.ReadUInt32();
                    uint bnkSectionLength = binaryReader.ReadUInt32();
                    uint sndEntrySectionLength = binaryReader.ReadUInt32();
                    uint secondSndEntrySectionLength = binaryReader.ReadUInt32();
                    fileStream.Seek(pathSectionLength, SeekOrigin.Current);

                    // Read soundbank entry section
                    uint bnkCount = binaryReader.ReadUInt32();

                    for (int i = 0; i < bnkCount; i++)
                    {
                        // Skip id
                        fileStream.Seek(4, SeekOrigin.Current);

                        uint bnkBlockSize = binaryReader.ReadUInt32();
                        uint bnkDataLength = binaryReader.ReadUInt32();
                        uint bnkBlockNum = binaryReader.ReadUInt32();
                        long currentPos = fileStream.Position;

                        // Read the Bnk data, parse it later
                        byte[] bnkData = new byte[bnkDataLength];
                        fileStream.Seek(bnkBlockSize * bnkBlockNum, SeekOrigin.Begin);
                        binaryReader.Read(bnkData, 0, (int)bnkDataLength);
                        wwisePackage.SoundbankFilesData.Add(bnkData);

                        // Go back to the bnk entry section and continue with the next one
                        fileStream.Seek(currentPos, SeekOrigin.Begin);

                        // Skip path index
                        fileStream.Seek(4, SeekOrigin.Current);
                    }
                }
            }

            return wwisePackage;
        }
    }
}
