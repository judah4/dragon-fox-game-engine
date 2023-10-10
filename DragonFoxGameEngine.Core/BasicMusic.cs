using Silk.NET.OpenAL;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace DragonGameEngine.Core
{
    public unsafe class BasicMusic
    {
        private static ALContext alc;
        private static AL _al;
        private static uint source;
        private static uint buffer;
        private static Device* device;
        private static Context* context;

        public static unsafe void LoadMusic()
        {
            var filePath = "Assets/Music/music1.wav";
            ReadOnlySpan<byte> file = File.ReadAllBytes(filePath);
            int index = 0;
            if (file[index++] != 'R' || file[index++] != 'I' || file[index++] != 'F' || file[index++] != 'F')
            {
                Console.WriteLine("Given file is not in RIFF format");
                return;
            }

            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
            index += 4;

            if (file[index++] != 'W' || file[index++] != 'A' || file[index++] != 'V' || file[index++] != 'E')
            {
                Console.WriteLine("Given file is not in WAVE format");
                return;
            }

            short numChannels = -1;
            int sampleRate = -1;
            int byteRate = -1;
            short blockAlign = -1;
            short bitsPerSample = -1;
            BufferFormat format = 0;

            alc = ALContext.GetApi();
            _al = AL.GetApi();
            device = alc.OpenDevice("");
            if (device == null)
            {
                Console.WriteLine("Could not create device");
                return;
            }

            context = alc.CreateContext(device, null);
            alc.MakeContextCurrent(context);

            _al.GetError();

            source = _al.GenSource();
            buffer = _al.GenBuffer();
            _al.SetSourceProperty(source, SourceBoolean.Looping, true);
            _al.SetSourceProperty(source, SourceFloat.Gain, 0.5f);


            while (index + 4 < file.Length)
            {
                var identifier = "" + (char)file[index++] + (char)file[index++] + (char)file[index++] + (char)file[index++];
                var size = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                index += 4;
                if (identifier == "fmt ")
                {
                    if (size != 16)
                    {
                        Console.WriteLine($"Unknown Audio Format with subchunk1 size {size}");
                    }
                    else
                    {
                        var audioFormat = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;
                        if (audioFormat != 1)
                        {
                            Console.WriteLine($"Unknown Audio Format with ID {audioFormat}");
                        }
                        else
                        {
                            numChannels = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                            index += 2;
                            sampleRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                            index += 4;
                            byteRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                            index += 4;
                            blockAlign = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                            index += 2;
                            bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                            index += 2;

                            if (numChannels == 1)
                            {
                                if (bitsPerSample == 8)
                                    format = BufferFormat.Mono8;
                                else if (bitsPerSample == 16)
                                    format = BufferFormat.Mono16;
                                else
                                {
                                    Console.WriteLine($"Can't Play mono {bitsPerSample} sound.");
                                }
                            }
                            else if (numChannels == 2)
                            {
                                if (bitsPerSample == 8)
                                    format = BufferFormat.Stereo8;
                                else if (bitsPerSample == 16)
                                    format = BufferFormat.Stereo16;
                                else
                                {
                                    Console.WriteLine($"Can't Play stereo {bitsPerSample} sound.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Can't play audio with {numChannels} sound");
                            }
                        }
                    }
                }
                else if (identifier == "data")
                {
                    var data = file.Slice(index, size);
                    index += size;

                    fixed (byte* pData = data)
                        _al.BufferData(buffer, format, pData, size, sampleRate);
                    Console.WriteLine($"Read {size} bytes Data");
                }
                else if (identifier == "JUNK")
                {
                    // this exists to align things
                    index += size;
                }
                else if (identifier == "iXML")
                {
                    var v = file.Slice(index, size);
                    var str = Encoding.ASCII.GetString(v);
                    Console.WriteLine($"iXML Chunk: {str}");
                    index += size;
                }
                else
                {
                    Console.WriteLine($"Unknown Section: {identifier}");
                    index += size;
                }
            }

            Console.WriteLine
            (
                $"Success. Detected RIFF-WAVE audio file, PCM encoding. {numChannels} Channels, {sampleRate} Sample Rate, {byteRate} Byte Rate, {blockAlign} Block Align, {bitsPerSample} Bits per Sample"
            );

            _al.SetSourceProperty(source, SourceInteger.Buffer, buffer);
            _al.SourcePlay(source);
            
        }

        public static void Destroy()
        {
            _al.SourceStop(source);

            _al.DeleteSource(source);
            _al.DeleteBuffer(buffer);
            alc.DestroyContext(context);
            alc.CloseDevice(device);
            _al.Dispose();
            alc.Dispose();
        }
    }
}
