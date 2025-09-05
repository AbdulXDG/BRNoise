using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace FluentNoise
{
    public class Equalizer
    {
        private readonly int[] _centers;
        private readonly int _sampleRate;
        private readonly BiquadFilter[] _filters;
        private float[] _gains;

        public Equalizer(int[] centers, int sampleRate)
        {
            _centers = centers;
            _sampleRate = sampleRate;
            _filters = new BiquadFilter[centers.Length];
            _gains = new float[centers.Length];
            for (int i = 0; i < centers.Length; i++)
            {
                _filters[i] = BiquadFilter.PeakingEQ(sampleRate, centers[i], 1.0f, 0);
            }
        }

        public void UpdateGains(float[] gains)
        {
            _gains = (float[])gains.Clone();
            for (int i = 0; i < _filters.Length; i++)
            {
                _filters[i].SetPeakingEq(_sampleRate, _centers[i], 1.0f, _gains[i]);
            }
        }

        public ISampleProvider AsSampleProvider(ISampleProvider source, float[] initialGains)
        {
            UpdateGains(initialGains);
            return new EqualizerProvider(source, _filters);
        }
    }

    public class EqualizerProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly BiquadFilter[] _filters;
        public EqualizerProvider(ISampleProvider source, BiquadFilter[] filters)
        {
            _source = source;
            _filters = filters;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            for (int n = 0; n < read; n++)
            {
                float sample = buffer[offset + n];
                foreach (var f in _filters)
                    sample = f.Transform(sample);
                buffer[offset + n] = sample;
            }
            return read;
        }
    }
}
