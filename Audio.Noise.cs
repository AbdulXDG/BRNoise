using System;
using System.Threading;
using NAudio.Wave;

namespace FluentNoise
{
    public enum NoiseType { White, Pink, Brown }

    public class NoiseVoice : ISampleProvider
    {
        private readonly WaveFormat _format;
        private NoiseType _type;
        private double _brown;
        public string Mode { get; set; } = "Soft";
        public string Range { get; set; } = "Normal";
        public NoiseType Type => _type;
        public static readonly Random ThreadRand = new Random();

        public NoiseVoice(NoiseType type, int sampleRate)
        {
            _type = type;
            _format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        }

        public void SetType(NoiseType t) { _type = t; }

        public WaveFormat WaveFormat => _format;

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                double sample = 0.0;
                switch (_type)
                {
                    case NoiseType.White:
                        sample = NextWhite();
                        break;
                    case NoiseType.Pink:
                        sample = NextPink();
                        break;
                    case NoiseType.Brown:
                        sample = NextBrown();
                        break;
                }
                buffer[offset + i] = (float)sample;
            }
            return count;
        }

        private double NextWhite()
        {
            // Uniform white [-1,1]
            return (ThreadRand.NextDouble() * 2.0 - 1.0) * 0.5;
        }

        // Pink noise via Paul Kellet approximation
        private double b0, b1, b2, b3, b4, b5, b6;
        private double NextPink()
        {
            double white = ThreadRand.NextDouble() * 2.0 - 1.0;
            b0 = 0.99886 * b0 + white * 0.0555179;
            b1 = 0.99332 * b1 + white * 0.0750759;
            b2 = 0.96900 * b2 + white * 0.1538520;
            b3 = 0.86650 * b3 + white * 0.3104856;
            b4 = 0.55000 * b4 + white * 0.5329522;
            b5 = -0.7616 * b5 - white * 0.0168980;
            double pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
            b6 = white * 0.115926;
            return pink * 0.11;
        }

        private double NextBrown()
        {
            // Integrate white
            _brown += (ThreadRand.NextDouble() * 2.0 - 1.0) * 0.02;
            _brown = Math.Clamp(_brown, -1.0, 1.0);
            return _brown * 0.7;
        }
    }
}
