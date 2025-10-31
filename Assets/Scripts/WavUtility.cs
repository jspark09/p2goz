using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Utility helpers to convert between Unity AudioClip data and standard WAV bytes.
/// </summary>
public static class WavUtility
{
    private const int HeaderSize = 44;

    private const int RiffHeaderSize = 12;

    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip));
        }

        int sampleCount = clip.samples * clip.channels;
        float[] floatData = new float[sampleCount];
        clip.GetData(floatData, 0);

        short[] intData = new short[sampleCount];
        byte[] bytesData = new byte[sampleCount * sizeof(short)];

        const float rescaleFactor = short.MaxValue;
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = Mathf.Clamp(floatData[i], -1f, 1f);
            intData[i] = (short)(sample * rescaleFactor);
        }

        Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        WriteHeader(writer, clip.channels, clip.frequency, bytesData.Length);
        writer.Write(bytesData);
        writer.Flush();

        return memoryStream.ToArray();
    }

    public static AudioClip ToAudioClip(byte[] wavFile, string clipName = "OpenAI_TTS")
    {
        if (wavFile == null || wavFile.Length < HeaderSize)
        {
            throw new ArgumentException("Invalid WAV data.", nameof(wavFile));
        }

        if (!IsRiffHeader(wavFile))
        {
            throw new InvalidDataException("Audio data is not in RIFF/WAVE format.");
        }

        (int fmtIndex, int fmtSize) = FindChunk(wavFile, "fmt ");
        if (fmtIndex < 0)
        {
            throw new InvalidDataException("WAV fmt chunk could not be located.");
        }

        short audioFormat = BitConverter.ToInt16(wavFile, fmtIndex);
        if (audioFormat != 1)
        {
            throw new NotSupportedException($"Only PCM WAV is supported. Received format code {audioFormat}.");
        }

        int channels = BitConverter.ToInt16(wavFile, fmtIndex + 2);
        int sampleRate = BitConverter.ToInt32(wavFile, fmtIndex + 4);
        short bitsPerSample = BitConverter.ToInt16(wavFile, fmtIndex + 14);

        if (bitsPerSample != 16)
        {
            throw new NotSupportedException($"Only 16-bit PCM WAV files are supported. Received {bitsPerSample}-bit.");
        }

        (int dataIndex, int dataSize) = FindChunk(wavFile, "data");
        if (dataIndex < 0)
        {
            throw new InvalidDataException("WAV data chunk could not be located.");
        }

        if (dataSize <= 0)
        {
            throw new InvalidDataException("WAV data chunk is empty.");
        }

        int sampleCount = dataSize / (bitsPerSample / 8);
        if (sampleCount <= 0)
        {
            throw new InvalidDataException("WAV file does not contain any samples.");
        }

        float[] floatData = new float[sampleCount];

        int offset = dataIndex;
        for (int i = 0; i < sampleCount; i++)
        {
            short value = BitConverter.ToInt16(wavFile, offset);
            floatData[i] = value / 32768f;
            offset += sizeof(short);
        }

        float[] processedData = floatData;
        int processedSampleRate = sampleRate;
        int samplesPerChannel = sampleCount / channels;

        int targetSampleRate = AudioSettings.outputSampleRate;
        if (targetSampleRate > 0 && targetSampleRate != sampleRate)
        {
            processedData = Resample(processedData, channels, sampleRate, targetSampleRate);
            processedSampleRate = targetSampleRate;
            samplesPerChannel = processedData.Length / channels;
        }

        if (samplesPerChannel <= 0)
        {
            throw new InvalidDataException("WAV sample count per channel is zero.");
        }

        AudioClip clip = AudioClip.Create(clipName, samplesPerChannel, channels, processedSampleRate, false);
        clip.SetData(processedData, 0);
        return clip;
    }

    private static void WriteHeader(BinaryWriter writer, int channels, int sampleRate, int dataLength)
    {
        int subChunk1Size = 16;
        short audioFormat = 1;
        short bitsPerSample = 16;
        short blockAlign = (short)(channels * (bitsPerSample / 8));
        int byteRate = sampleRate * blockAlign;
        int chunkSize = HeaderSize + dataLength - 8;

        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(chunkSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(subChunk1Size);
        writer.Write(audioFormat);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataLength);
    }

    private static bool IsRiffHeader(byte[] wavFile)
    {
        return wavFile[0] == 'R' && wavFile[1] == 'I' && wavFile[2] == 'F' && wavFile[3] == 'F' &&
               wavFile[8] == 'W' && wavFile[9] == 'A' && wavFile[10] == 'V' && wavFile[11] == 'E';
    }

    private static (int chunkDataIndex, int chunkSize) FindChunk(byte[] wavFile, string chunkId)
    {
        if (string.IsNullOrEmpty(chunkId) || chunkId.Length != 4)
        {
            throw new ArgumentException("Chunk identifier must be four characters.", nameof(chunkId));
        }

        int index = RiffHeaderSize;
        while (index + 8 <= wavFile.Length)
        {
            string id = Encoding.ASCII.GetString(wavFile, index, 4);
            uint size = BitConverter.ToUInt32(wavFile, index + 4);

            int dataStart = index + 8;
            int bytesRemaining = Math.Max(0, wavFile.Length - dataStart);
            long declaredSize = size == uint.MaxValue ? bytesRemaining : size;

            if (id.Equals(chunkId, StringComparison.OrdinalIgnoreCase))
            {
                long effectiveSize = Math.Min(declaredSize, bytesRemaining);
                int safeSize = (int)Math.Max(0, Math.Min(effectiveSize, int.MaxValue));
                if (safeSize > 0)
                {
                    return (dataStart, safeSize);
                }

                // Keep searching in case another chunk of the same type carries the data.
            }

            long advance = Math.Max(declaredSize, 0);
            long nextIndex = dataStart + advance;
            if ((advance & 1) == 1)
            {
                nextIndex += 1; // Chunks are padded to even sizes.
            }

            nextIndex = Math.Min(nextIndex, wavFile.Length);

            if (nextIndex <= index)
            {
                break;
            }

            index = (int)nextIndex;
        }

        if (chunkId.Equals("data", StringComparison.OrdinalIgnoreCase))
        {
            (int fallbackIndex, int fallbackSize) = FindDataChunkByScan(wavFile);
            if (fallbackIndex >= 0 && fallbackSize > 0)
            {
                return (fallbackIndex, fallbackSize);
            }
        }

        return (-1, -1);
    }

    private static (int chunkDataIndex, int chunkSize) FindDataChunkByScan(byte[] wavFile)
    {
        for (int i = RiffHeaderSize; i <= wavFile.Length - 8; i++)
        {
            if (wavFile[i] == 'd' && wavFile[i + 1] == 'a' && wavFile[i + 2] == 't' && wavFile[i + 3] == 'a')
            {
                uint size = BitConverter.ToUInt32(wavFile, i + 4);
                int dataStart = i + 8;
                int bytesRemaining = Math.Max(0, wavFile.Length - dataStart);
                long declaredSize = size == uint.MaxValue ? bytesRemaining : size;
                long effectiveSize = Math.Min(declaredSize, bytesRemaining);
                int safeSize = (int)Math.Max(0, Math.Min(effectiveSize, int.MaxValue));
                if (safeSize > 0)
                {
                    return (dataStart, safeSize);
                }
            }
        }

        return (-1, -1);
    }

    private static float[] Resample(float[] input, int channels, int inputSampleRate, int targetSampleRate)
    {
        if (inputSampleRate <= 0 || targetSampleRate <= 0 || input.Length == 0)
        {
            return input;
        }

        int inputFrames = input.Length / channels;
        double resampleRatio = (double)targetSampleRate / inputSampleRate;
        int outputFrames = Mathf.Max(1, Mathf.RoundToInt((float)(inputFrames * resampleRatio)));
        float[] output = new float[outputFrames * channels];

        double increment = (double)inputSampleRate / targetSampleRate;
        double sourceIndex = 0.0;

        for (int frame = 0; frame < outputFrames; frame++)
        {
            int baseIndex = (int)sourceIndex;
            double t = sourceIndex - baseIndex;

            for (int ch = 0; ch < channels; ch++)
            {
                int srcIndex1 = Mathf.Clamp(baseIndex, 0, inputFrames - 1) * channels + ch;
                int srcIndex2 = Mathf.Clamp(baseIndex + 1, 0, inputFrames - 1) * channels + ch;

                float sample1 = input[srcIndex1];
                float sample2 = input[srcIndex2];
                output[frame * channels + ch] = Mathf.Lerp(sample1, sample2, (float)t);
            }

            sourceIndex += increment;
        }

        return output;
    }
}
