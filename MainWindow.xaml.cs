using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using FmStereoModulator.Audio;
using FmStereoModulator.Dsp;
namespace FmStereoModulator;
public partial class MainWindow : Window
{
    const int TestToneId=int.MinValue, FileAudioId=int.MinValue+1, StreamAudioId=int.MinValue+2; CancellationTokenSource? cts; string[] audioFiles=[]; readonly LiveMpxControls liveControls=new(.65);
    public MainWindow() { InitializeComponent(); InputBox.Items.Add(new AudioInputDevice(TestToneId,"Stereo test tone 1 kHz / 1.3 kHz"));InputBox.Items.Add(new AudioInputDevice(FileAudioId,"Audio file (MP3, WAV, FLAC, AAC, M4A, WMA, AIFF)"));InputBox.Items.Add(new AudioInputDevice(StreamAudioId,"Network stream (HTTP/HTTPS)")); foreach(var d in WaveInSource.EnumerateDevices())InputBox.Items.Add(d);InputBox.SelectedIndex=0; Closing += (_,__) => Stop(); }
    async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!ushort.TryParse(PiBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var pi)) { MessageBox.Show("Invalid PI: enter four hexadecimal digits."); return; }
        try {
            cts = new(); Toggle(true); StatusText.Text = "Generating MPX…";
            var us = double.Parse(((ComboBoxItem)PreemphasisBox.SelectedItem).Tag.ToString()!, CultureInfo.InvariantCulture);
            var config = new MpxConfig(pi, PsBox.Text, RtBox.Text, RdsBox.IsChecked == true, TransmissionModeBox.SelectedIndex==0, CarrierOnlyBox.IsChecked==true, CompressorBox.IsChecked==true, us, LevelSlider.Value);
            var selected=(AudioInputDevice)InputBox.SelectedItem;
            IAudioSource source = selected.Id switch { TestToneId=>new TestToneSource(),FileAudioId when audioFiles.Length>0=>new FileAudioSource(audioFiles),FileAudioId=>throw new InvalidOperationException("Select one or more audio files first."),StreamAudioId when Uri.TryCreate(StreamUrlBox.Text,UriKind.Absolute,out var streamUri) && streamUri.Scheme is "http" or "https"=>new FileAudioSource(StreamUrlBox.Text),StreamAudioId=>throw new InvalidOperationException("Enter a valid HTTP or HTTPS stream URL."),_=>new WaveInSource(selected.Id) };
            if(!double.TryParse(FrequencyBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var mhz)||mhz<1||mhz>6000)throw new InvalidOperationException("Invalid HackRF frequency (1–6000 MHz).");
            if(!uint.TryParse(TxGainBox.Text,out var gain)||gain>47)throw new InvalidOperationException("Invalid TX gain (0–47 dB).");
            IAudioSink sink = OutputBox.SelectedIndex switch { 1 => new HackRfSink(mhz,gain,RfAmpBox.IsChecked==true,CarrierOnlyBox.IsChecked==true), 2 => new WaveFileSink("wolfradio_mpx_192k.wav"), _ => new WaveOutSink() };
            liveControls.InputGain=LevelSlider.Value;
            await Task.Run(() => new MpxEngine(config, source, sink, UpdateTelemetry, liveControls).Run(cts.Token));
        } catch (OperationCanceledException) { } catch (Exception ex) { string dir=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WolfRadio");System.IO.Directory.CreateDirectory(dir);string log=System.IO.Path.Combine(dir,"WolfRadio-error.log");System.IO.File.WriteAllText(log,$"{DateTime.Now:O}\r\n{ex}");MessageBox.Show($"{ex.Message}\n\nDiagnostic details were saved to:\n{log}", "Error"); }
        finally { Toggle(false); StatusText.Text = "Stopped"; cts?.Dispose(); cts = null; }
    }
    void Stop_Click(object sender, RoutedEventArgs e) => Stop();
    void Info_Click(object sender, RoutedEventArgs e) => new InfoWindow { Owner = this }.ShowDialog();
    void InputBox_SelectionChanged(object sender,System.Windows.Controls.SelectionChangedEventArgs e){if(BrowseAudioButton==null||StreamUrlBox==null)return;int? id=(InputBox.SelectedItem as AudioInputDevice)?.Id;BrowseAudioButton.IsEnabled=id==FileAudioId;AudioFileText.Visibility=id==StreamAudioId?Visibility.Collapsed:Visibility.Visible;BrowseAudioButton.Visibility=id==StreamAudioId?Visibility.Collapsed:Visibility.Visible;StreamUrlBox.Visibility=id==StreamAudioId?Visibility.Visible:Visibility.Collapsed;}
    void BrowseAudio_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Title="Select one or more audio files",Filter="Audio files|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.wma;*.aiff;*.aif|All files|*.*",Multiselect=true};if(d.ShowDialog(this)==true){audioFiles=d.FileNames;AudioFileText.Text=audioFiles.Length==1?System.IO.Path.GetFileName(audioFiles[0]):$"{audioFiles.Length} tracks selected (loop playlist)";}}
    void Stop() => cts?.Cancel();
    void LevelSlider_ValueChanged(object sender,RoutedPropertyChangedEventArgs<double> e) => liveControls.InputGain=e.NewValue;
    void Toggle(bool running) { StartButton.IsEnabled=!running; StopButton.IsEnabled=running; InputBox.IsEnabled=!running; OutputBox.IsEnabled=!running; }
    void UpdateTelemetry(MpxTelemetry t) => Dispatcher.BeginInvoke(() => { VuLeft.Value=t.LeftPeak;VuRight.Value=t.RightPeak;double w=SpectrumCanvas.ActualWidth,h=SpectrumCanvas.ActualHeight;if(w<2||h<2)return;var p=new PointCollection(t.Spectrum.Length);for(int i=0;i<t.Spectrum.Length;i++)p.Add(new(i*w/(t.Spectrum.Length-1),h*(1-t.Spectrum[i])));SpectrumLine.Points=p; });
}
