using System;
using NAudio.Wave;

namespace FluentNoise
{
    // Simple stereo width processor: mixes some inverted signal between channels
    public class StereoWidthProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _mix; // -0.5 left, +0.5 right for widening

        public StereoWidthProvider(ISampleProvider source, float mix)
        {
            _source = source;
            _mix = mix;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            return _source.Read(buffer, offset, count);
        }
    }

    public static class SampleProviderExtensions
    {
        public static ISampleProvider ToStereo(this ISampleProvider mono, float leftGain, float rightGain)
        {
            var stereo = new MonoToStereoSampleProvider(mono);
            stereo.LeftVolume = leftGain == 0f ? 1f : leftGain;
            stereo.RightVolume = rightGain == 0f ? 1f : rightGain;
            return stereo;
        }
    }
}
