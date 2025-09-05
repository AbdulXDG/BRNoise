using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FluentNoise
{
    public partial class MainWindow : Window
    {
        private readonly WaveOutEvent _output;
        private readonly MixingSampleProvider _mixer;
        private NoiseVoice _voiceL;
        private NoiseVoice _voiceR;
        private readonly DispatcherTimer _animTimer;
        private readonly Equalizer _eqL;
        private readonly Equalizer _eqR;
        private ISampleProvider _pipelineL;
        private ISampleProvider _pipelineR;

        private float _volume = 0.6f;
        private float _stereoWidth = 1.0f; // 0 mono, 1 normal, >1 wide
        private float _speed = 1.0f;
        private bool _alternate = false;

        private readonly float[] _bandGains = new float[] {0,0,0,0,0,0,0,0};
        private readonly int[] _centers = new[] {60,125,250,500,1000,2000,4000,8000};

        public MainWindow()
        {
            InitializeComponent();

            // Presets list
            PresetCombo.ItemsSource = Presets.All.Select(p => p.Name);
            PresetCombo.SelectedIndex = 0;

            _output = new WaveOutEvent() { DesiredLatency = 100 };
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)) { ReadFully = true };

            // Build voices
            _voiceL = new NoiseVoice(NoiseType.White, 48000);
            _voiceR = new NoiseVoice(NoiseType.White, 48000);

            _eqL = new Equalizer(_centers, 48000);
            _eqR = new Equalizer(_centers, 48000);

            RebuildPipelines();

            _mixer.AddMixerInput(_pipelineL.ToStereo(0f, 1f));
            _mixer.AddMixerInput(_pipelineR.ToStereo(1f, 0f));

            var vol = new VolumeSampleProvider(_mixer) { Volume = _volume };
            _output.Init(vol);

            // Animation
            _animTimer = new DispatcherTimer();
            _animTimer.Interval = TimeSpan.FromMilliseconds(80);
            _animTimer.Tick += (s, e) => AnimateStep();
        }

        private void RebuildPipelines()
        {
            // speed control via resampler
            ISampleProvider left = _voiceL;
            ISampleProvider right = _voiceR;

            if (Math.Abs(_speed - 1.0f) > 0.001f)
            {
                left = new WdlResamplingSampleProvider(left, (int)(48000 * _speed));
                right = new WdlResamplingSampleProvider(right, (int)(48000 * _speed));
            }

            left = _eqL.AsSampleProvider(left, _bandGains);
            right = _eqR.AsSampleProvider(right, _bandGains);

            // stereo width processing
            var width = _stereoWidth;
            _pipelineL = new StereoWidthProvider(left, -width * 0.5f);
            _pipelineR = new StereoWidthProvider(right, width * 0.5f);
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            _output.Play();
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _output.Pause();
        }

        private void ResetBtn_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var (slider, idx) in SliderMap())
            {
                slider.Value = 0;
                _bandGains[idx] = 0;
            }
            VolumeSlider.Value = 0.6;
            SpeedSlider.Value = 1.0;
            StereoCombo.SelectedIndex = 2; // normal
            _voiceL.SetType(NoiseType.White);
            _voiceR.SetType(NoiseType.White);
            _alternate = false;
            RebuildPipelines();
        }

        private IEnumerable<(Slider slider, int idx)> SliderMap()
        {
            return new List<(Slider, int)>
            {
                (S60,0),(S125,1),(S250,2),(S500,3),(S1k,4),(S2k,5),(S4k,6),(S8k,7)
            };
        }

        private void BandSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var map = SliderMap().ToDictionary(t => t.slider, t => t.idx);
            var slider = (Slider)sender;
            if (map.TryGetValue(slider, out int idx))
            {
                _bandGains[idx] = (float)e.NewValue;
                _eqL.UpdateGains(_bandGains);
                _eqR.UpdateGains(_bandGains);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _volume = (float)e.NewValue;
            // The mixer Volume provider handles it; we need to re-init
            _output.Stop();
            _output.Init(new VolumeSampleProvider(_mixer){ Volume = _volume});
            _output.Play();
        }

        private void StereoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBoxItem)StereoCombo.SelectedItem).Content.ToString())
            {
                case "Mono": _stereoWidth = 0.0f; break;
                case "Narrow": _stereoWidth = 0.5f; break;
                case "Normal": _stereoWidth = 1.0f; break;
                case "Wide": _stereoWidth = 1.5f; break;
            }
            RebuildPipelines();
        }

        private void Slower_Click(object sender, RoutedEventArgs e) { SpeedSlider.Value = Math.Max(0.5, SpeedSlider.Value - 0.1); }
        private void Faster_Click(object sender, RoutedEventArgs e) { SpeedSlider.Value = Math.Min(2.0, SpeedSlider.Value + 0.1); }
        private void Alternate_Click(object sender, RoutedEventArgs e) { _alternate = !_alternate; }
        private void ResetSpeed_Click(object sender, RoutedEventArgs e) { SpeedSlider.Value = 1.0; }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _speed = (float)e.NewValue;
            RebuildPipelines();
        }

        private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Simple interpretation: change noise hardness/softness
            var mode = ((ComboBoxItem)ModeCombo.SelectedItem).Content.ToString();
            _voiceL.Mode = mode;
            _voiceR.Mode = mode;
        }

        private void RangeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var range = ((ComboBoxItem)RangeCombo.SelectedItem).Content.ToString();
            _voiceL.Range = range;
            _voiceR.Range = range;
        }

        private void AnimateToggle_Checked(object sender, RoutedEventArgs e) => _animTimer.Start();
        private void AnimateToggle_Unchecked(object sender, RoutedEventArgs e) => _animTimer.Stop();

        private void AnimateStep()
        {
            // Gentle random walk across sliders based on mode and range settings
            var rand = NoiseVoice.ThreadRand;
            var scale = ModeCombo.SelectedIndex switch
            {
                0 => 0.2, // soft
                1 => 0.8, // hard
                2 => 0.4, // solo duo
                3 => 0.5, // trio
                _ => 0.3
            };
            var range = RangeCombo.SelectedIndex switch
            {
                0 => 4.0,
                1 => 9.0,
                2 => 14.0,
                _ => 9.0
            };

            foreach (var (slider, idx) in SliderMap())
            {
                var v = slider.Value + (rand.NextDouble() - 0.5) * scale;
                v = Math.Max(-range, Math.Min(range, v));
                slider.Value = v;
            }

            if (_alternate)
            {
                // flip noise types left/right
                var type = _voiceL.Type;
                _voiceL.SetType(_voiceR.Type);
                _voiceR.SetType(type);
            }
        }

        private void ApplyEqToSliders_Click(object sender, RoutedEventArgs e)
        {
            // Apply iEQ selection to sliders
            var item = (ComboBoxItem)EqCombo.SelectedItem;
            var name = item.Content.ToString();
            var gains = name switch
            {
                "Balanced" => new float[]{ -2,-1,0,1,1,0,-1,-2 },
                "Full" => new float[]{ 2,2,2,2,2,2,2,2 },
                _ => new float[]{0,0,0,0,0,0,0,0}
            };
            int i = 0;
            foreach (var (slider, idx) in SliderMap())
            {
                slider.Value = gains[i++];
            }
        }

        private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var name = PresetCombo.SelectedItem?.ToString() ?? "White";
            var preset = Presets.All.First(p => p.Name == name);
            // set type
            _voiceL.SetType(preset.Type);
            _voiceR.SetType(preset.Type);
            // set gains
            int i = 0;
            foreach (var (slider, idx) in SliderMap())
            {
                slider.Value = preset.Gains[i++];
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var state = new AppState
            {
                Gains = _bandGains.ToArray(),
                Volume = _volume,
                Width = _stereoWidth,
                Speed = _speed,
                Noise = _voiceL.Type.ToString()
            };
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("FluentNoise.profile.json", json);
            MessageBox.Show("Saved to FluentNoise.profile.json");
        }

        private void LoadProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("FluentNoise.profile.json")) { MessageBox.Show("No profile file found."); return; }
            var json = File.ReadAllText("FluentNoise.profile.json");
            var state = JsonSerializer.Deserialize<AppState>(json);
            if (state == null) return;
            int i = 0;
            foreach (var (slider, idx) in SliderMap())
            {
                slider.Value = state.Gains[i++];
            }
            VolumeSlider.Value = state.Volume;
            SpeedSlider.Value = state.Speed;
            switch (state.Noise)
            {
                case "White": _voiceL.SetType(NoiseType.White); _voiceR.SetType(NoiseType.White); break;
                case "Pink": _voiceL.SetType(NoiseType.Pink); _voiceR.SetType(NoiseType.Pink); break;
                case "Brown": _voiceL.SetType(NoiseType.Brown); _voiceR.SetType(NoiseType.Brown); break;
            }
        }
    }

    public class AppState
    {
        public float[] Gains { get; set; } = new float[8];
        public float Volume { get; set; }
        public float Width { get; set; }
        public float Speed { get; set; }
        public string Noise { get; set; } = "White";
    }
}
