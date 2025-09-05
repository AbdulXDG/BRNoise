using System.Collections.Generic;

namespace FluentNoise
{
    public record Preset(string Name, NoiseType Type, float[] Gains);

    public static class Presets
    {
        public static readonly List<Preset> All = new()
        {
            new Preset("White", NoiseType.White,  new float[]{0,0,0,0,0,0,0,0}),
            new Preset("Pink",  NoiseType.Pink,   new float[]{0,0,0,0,0,0,0,0}),
            new Preset("Brown", NoiseType.Brown,  new float[]{0,0,0,0,0,0,0,0}),
            new Preset("Grey",  NoiseType.White,  new float[]{ 1, 1, 1, 1, 1, 1, 1, 1}),
            new Preset("Infra", NoiseType.Brown,  new float[]{ 4, 3, 1, -2, -4, -6, -8, -10}),
            new Preset("Ultra", NoiseType.White,  new float[]{ -8, -6, -4, -2, 0, 2, 4, 6}),
            new Preset("125Hz", NoiseType.White,  new float[]{ -12, 8, -8, -12, -12, -12, -12, -12}),
            new Preset("250Hz", NoiseType.White,  new float[]{ -12, -8, 8, -12, -12, -12, -12, -12}),
            new Preset("500Hz", NoiseType.White,  new float[]{ -12, -12, -8, 8, -12, -12, -12, -12}),
            new Preset("1kHz",  NoiseType.White,  new float[]{ -12, -12, -12, -8, 8, -12, -12, -12}),
            new Preset("2kHz",  NoiseType.White,  new float[]{ -12, -12, -12, -12, -8, 8, -12, -12}),
            new Preset("4kHz",  NoiseType.White,  new float[]{ -12, -12, -12, -12, -12, -8, 8, -12}),
            new Preset("8kHz",  NoiseType.White,  new float[]{ -12, -12, -12, -12, -12, -12, -8, 8}),
        };
    }
}
