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
    const int TestToneId=int.MinValue, FileAudioId=int.MinValue+1; CancellationTokenSource? cts; string? audioFile;
    public MainWindow() { InitializeComponent(); InputBox.Items.Add(new AudioInputDevice(TestToneId,"Tono test stereo 1 kHz / 1,3 kHz"));InputBox.Items.Add(new AudioInputDevice(FileAudioId,"File audio (MP3, WAV, AAC, M4A, WMA, AIFF)")); foreach(var d in WaveInSource.EnumerateDevices())InputBox.Items.Add(d);InputBox.SelectedIndex=0; Closing += (_,__) => Stop(); }
    async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!ushort.TryParse(PiBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var pi)) { MessageBox.Show("PI non valido: usare quattro cifre esadecimali."); return; }
        try {
            cts = new(); Toggle(true); StatusText.Text = "Generazione MPX in corso…";
            var us = double.Parse(((ComboBoxItem)PreemphasisBox.SelectedItem).Tag.ToString()!, CultureInfo.InvariantCulture);
            var config = new MpxConfig(pi, PsBox.Text, RtBox.Text, RdsBox.IsChecked == true, CarrierOnlyBox.IsChecked==true, us, LevelSlider.Value);
            var selected=(AudioInputDevice)InputBox.SelectedItem;
            IAudioSource source = selected.Id switch { TestToneId=>new TestToneSource(),FileAudioId when audioFile!=null=>new FileAudioSource(audioFile),FileAudioId=>throw new InvalidOperationException("Selezionare prima un file audio."),_=>new WaveInSource(selected.Id) };
            if(!double.TryParse(FrequencyBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var mhz)||mhz<1||mhz>6000)throw new InvalidOperationException("Frequenza HackRF non valida (1–6000 MHz).");
            if(!uint.TryParse(TxGainBox.Text,out var gain)||gain>47)throw new InvalidOperationException("Guadagno TX non valido (0–47 dB).");
            IAudioSink sink = OutputBox.SelectedIndex switch { 1 => new HackRfSink(mhz,gain,RfAmpBox.IsChecked==true,CarrierOnlyBox.IsChecked==true), 2 => new WaveFileSink("wolfradio_mpx_192k.wav"), _ => new WaveOutSink() };
            await Task.Run(() => new MpxEngine(config, source, sink, UpdateTelemetry).Run(cts.Token));
        } catch (OperationCanceledException) { } catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
        finally { Toggle(false); StatusText.Text = "Fermato"; cts?.Dispose(); cts = null; }
    }
    void Stop_Click(object sender, RoutedEventArgs e) => Stop();
    void Info_Click(object sender, RoutedEventArgs e) => new InfoWindow { Owner = this }.ShowDialog();
    void InputBox_SelectionChanged(object sender,System.Windows.Controls.SelectionChangedEventArgs e){if(BrowseAudioButton==null)return;BrowseAudioButton.IsEnabled=(InputBox.SelectedItem as AudioInputDevice)?.Id==FileAudioId;}
    void BrowseAudio_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Title="Scegli un file audio",Filter="File audio|*.mp3;*.wav;*.aac;*.m4a;*.wma;*.aiff;*.aif|Tutti i file|*.*"};if(d.ShowDialog(this)==true){audioFile=d.FileName;AudioFileText.Text=System.IO.Path.GetFileName(audioFile);}}
    void Stop() => cts?.Cancel();
    void Toggle(bool running) { StartButton.IsEnabled=!running; StopButton.IsEnabled=running; InputBox.IsEnabled=!running; OutputBox.IsEnabled=!running; }
    void UpdateTelemetry(MpxTelemetry t) => Dispatcher.BeginInvoke(() => { VuLeft.Value=t.LeftPeak;VuRight.Value=t.RightPeak;double w=SpectrumCanvas.ActualWidth,h=SpectrumCanvas.ActualHeight;if(w<2||h<2)return;var p=new PointCollection(t.Spectrum.Length);for(int i=0;i<t.Spectrum.Length;i++)p.Add(new(i*w/(t.Spectrum.Length-1),h*(1-t.Spectrum[i])));SpectrumLine.Points=p; });
}
