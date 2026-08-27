using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Concurrent;
using System.Net.Http;
using FmStereoModulator.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Flac;
namespace FmStereoModulator.Audio;
public interface IAudioSource:IDisposable { void Start(int rate); void Read(float[] stereo,int frames,CancellationToken ct); }
public interface ITrackMetadataSource { string? CurrentTitle { get; } }
public interface IAudioSink:IDisposable { void Start(int rate); void Write(short[] mono,CancellationToken ct); }
public sealed class HackRfSink(double frequencyMhz, uint txGain, bool amplifierOn, bool carrierOnly=false):IAudioSink
{
    const int IqRate=2_400_000,IqOffset=0;const double PhaseScale=4294967296.0/IqRate,InputStep=(double)MpxEngine.SampleRate/IqRate;static readonly sbyte[] SinLut=BuildLut(false),CosLut=BuildLut(true);readonly BlockingCollection<byte[]> queue=new(16);readonly object startLock=new();IntPtr device;TxCallback? callback;bool initialized,started;byte[]? currentIq;byte[] carrierBuffer=[],holdBuffer=[];int currentOffset;byte lastI=110,lastQ=0;uint phaseAccumulator;long inputBase;double nextInputPosition;short previousMpx;
    public void Start(int inputRate)
    {
        try { Check(hackrf_init(),"initialization");initialized=true;Check(hackrf_open(out device),"device open");Check(hackrf_set_freq(device,(ulong)(frequencyMhz*1_000_000-IqOffset)),"frequency setup");Check(hackrf_set_sample_rate_manual(device,IqRate,1),"sample-rate setup");Check(hackrf_set_baseband_filter_bandwidth(device,1_750_000),"baseband filter setup");Check(hackrf_set_txvga_gain(device,Math.Min(txGain,47)),"TX gain setup");
            Check(hackrf_set_amp_enable(device,amplifierOn?(byte)0:(byte)1),"RF amplifier setup");callback=OnTransfer;if(carrierOnly)StartTx();
        } catch(DllNotFoundException){throw new InvalidOperationException("hackrf.dll was not found. Install the WinUSB device driver and reinstall WolfRadio.");}
    }
    public void Write(short[] mono,CancellationToken ct)
    {
        if(mono.Length==0)return;long end=inputBase+mono.Length-1;int samples=nextInputPosition<end?(int)Math.Ceiling((end-nextInputPosition)/InputStep):0;var iq=new byte[samples*2];for(int j=0;j<samples;j++){long a=(long)Math.Floor(nextInputPosition),b=a+1;double q=nextInputPosition-a;int ia=(int)Math.Clamp(a-inputBase,0,mono.Length-1),ib=(int)Math.Clamp(b-inputBase,0,mono.Length-1);short sa=a<inputBase?previousMpx:mono[ia],sb=mono[ib];double f=sa+(sb-sa)*q;phaseAccumulator+=unchecked((uint)((IqOffset+75_000*(f/32768.0))*PhaseScale));int ix=(int)(phaseAccumulator>>16),k=j*2;iq[k]=unchecked((byte)CosLut[ix]);iq[k+1]=unchecked((byte)SinLut[ix]);nextInputPosition+=InputStep;}previousMpx=mono[^1];inputBase+=mono.Length;queue.Add(iq,ct);if(!started&&queue.Count>=8)StartTx();
    }
    void StartTx(){lock(startLock){if(started)return;Check(hackrf_start_tx(device,callback!,IntPtr.Zero),"TX start");started=true;}}
    static sbyte[] BuildLut(bool cosine){var x=new sbyte[65536];for(int i=0;i<x.Length;i++)x[i]=(sbyte)Math.Round((cosine?Math.Cos(2*Math.PI*i/x.Length):Math.Sin(2*Math.PI*i/x.Length))*110);return x;}
    int OnTransfer(ref Transfer t){if(carrierOnly){if(carrierBuffer.Length<t.valid_length)carrierBuffer=new byte[t.valid_length];uint step=unchecked((uint)(IqOffset*PhaseScale));for(int i=0;i+1<t.valid_length;i+=2){phaseAccumulator+=step;int ix=(int)(phaseAccumulator>>16);carrierBuffer[i]=unchecked((byte)CosLut[ix]);carrierBuffer[i+1]=unchecked((byte)SinLut[ix]);}Marshal.Copy(carrierBuffer,0,t.buffer,t.valid_length);return 0;}int written=0;while(written<t.valid_length){if(currentIq==null||currentOffset>=currentIq.Length){if(!queue.TryTake(out currentIq,50)){int missing=t.valid_length-written;if(holdBuffer.Length<missing)holdBuffer=new byte[missing];for(int i=0;i+1<missing;i+=2){holdBuffer[i]=lastI;holdBuffer[i+1]=lastQ;}Marshal.Copy(holdBuffer,0,t.buffer+written,missing);break;}currentOffset=0;}int n=Math.Min(currentIq.Length-currentOffset,t.valid_length-written);Marshal.Copy(currentIq,currentOffset,t.buffer+written,n);if(n>=2){int q=currentOffset+n-2;lastI=currentIq[q];lastQ=currentIq[q+1];}currentOffset+=n;written+=n;}return 0;}
    public void Dispose(){queue.CompleteAdding();if(started)hackrf_stop_tx(device);if(device!=IntPtr.Zero)hackrf_close(device);if(initialized)hackrf_exit();started=initialized=false;device=IntPtr.Zero;}
    static void Check(int code,string step){if(code!=0)throw new InvalidOperationException($"HackRF error during {step} (code {code}). Check the USB connection and WinUSB driver.");}
    [StructLayout(LayoutKind.Sequential)]struct Transfer{public IntPtr device,buffer;public int buffer_length,valid_length;public IntPtr rx_ctx,tx_ctx;} [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int TxCallback(ref Transfer transfer);
    [DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_init();[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_exit();[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_open(out IntPtr d);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_close(IntPtr d);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_freq(IntPtr d,ulong hz);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_sample_rate_manual(IntPtr d,uint hz,uint divider);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_baseband_filter_bandwidth(IntPtr d,uint hz);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_txvga_gain(IntPtr d,uint gain);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_amp_enable(IntPtr d,byte enable);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_start_tx(IntPtr d,TxCallback cb,IntPtr ctx);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_stop_tx(IntPtr d);
}
public sealed class TestToneSource:IAudioSource { int rate; long n; public void Start(int r)=>rate=r; public void Read(float[] b,int frames,CancellationToken ct){for(int i=0;i<frames;i++){b[2*i]=(float)(.55*Math.Sin(2*Math.PI*1000*n/rate));b[2*i+1]=(float)(.35*Math.Sin(2*Math.PI*1300*n/rate));n++;}} public void Dispose(){} }
public sealed class FileAudioSource:IAudioSource,ITrackMetadataSource
{
    readonly string[] paths;WaveStream? reader;ISampleProvider? samples;IcyMetadataMonitor? metadataMonitor;int rate,index;string? currentTitle;
    public string? CurrentTitle=>metadataMonitor?.CurrentTitle??currentTitle;
    public FileAudioSource(string path):this([path]){}
    public FileAudioSource(IEnumerable<string> paths){this.paths=paths.ToArray();if(this.paths.Length==0)throw new ArgumentException("The playlist is empty.",nameof(paths));}
    public void Start(int outputRate){rate=outputRate;Open();}
    void Open(){reader?.Dispose();metadataMonitor?.Dispose();metadataMonitor=null;string path=paths[index];if(Uri.TryCreate(path,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https"){currentTitle=null;metadataMonitor=new IcyMetadataMonitor(path);}else currentTitle=ReadFileTitle(path);reader=CreateReader(path);ISampleProvider p=reader.ToSampleProvider();if(p.WaveFormat.Channels==1)p=new MonoToStereoSampleProvider(p);else if(p.WaveFormat.Channels>2){var mux=new MultiplexingSampleProvider([p],2);mux.ConnectInputToOutput(0,0);mux.ConnectInputToOutput(1,1);p=mux;}samples=p.WaveFormat.SampleRate==rate?p:new WdlResamplingSampleProvider(p,rate);}
    static string ReadFileTitle(string path){try{using var tag=TagLib.File.Create(path);string title=tag.Tag.Title??"";string artist=tag.Tag.FirstPerformer??"";if(title.Length>0)return artist.Length>0?$"{artist} - {title}":title;}catch{}return Path.GetFileNameWithoutExtension(path);}
    static WaveStream CreateReader(string file){if(Uri.TryCreate(file,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https")return new MediaFoundationReader(file);string ext=Path.GetExtension(file).ToLowerInvariant();return ext switch{".flac"=>new FlacReader(file),".wav" or ".mp3" or ".aif" or ".aiff"=>new AudioFileReader(file),_=>new MediaFoundationReader(file)};}
    public void Read(float[] stereo,int frames,CancellationToken ct){int needed=frames*2,done=0;while(done<needed){ct.ThrowIfCancellationRequested();int n=samples!.Read(stereo,done,needed-done);if(n==0){index=(index+1)%paths.Length;Open();continue;}done+=n;}}
    public void Dispose(){metadataMonitor?.Dispose();metadataMonitor=null;reader?.Dispose();reader=null;samples=null;}
}
sealed class IcyMetadataMonitor:IDisposable
{
    readonly CancellationTokenSource stop=new();readonly HttpClient client=new();string? currentTitle;
    public string? CurrentTitle=>Volatile.Read(ref currentTitle);
    public IcyMetadataMonitor(string url){_=Task.Run(()=>Monitor(url,stop.Token));}
    async Task Monitor(string url,CancellationToken ct){try{using var request=new HttpRequestMessage(HttpMethod.Get,url);request.Headers.TryAddWithoutValidation("Icy-MetaData","1");using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();if(!response.Headers.TryGetValues("icy-metaint",out var values)||!int.TryParse(values.FirstOrDefault(),out int interval)||interval<=0)return;using var stream=await response.Content.ReadAsStreamAsync(ct);var audio=new byte[8192];while(!ct.IsCancellationRequested){int left=interval;while(left>0){int n=await stream.ReadAsync(audio.AsMemory(0,Math.Min(left,audio.Length)),ct);if(n==0)return;left-=n;}int blocks=stream.ReadByte();if(blocks<0)return;int length=blocks*16;if(length==0)continue;var metadata=new byte[length];int done=0;while(done<length){int n=await stream.ReadAsync(metadata.AsMemory(done,length-done),ct);if(n==0)return;done+=n;}string text=System.Text.Encoding.Latin1.GetString(metadata).TrimEnd('\0');const string key="StreamTitle='";int start=text.IndexOf(key,StringComparison.OrdinalIgnoreCase);if(start>=0){start+=key.Length;int end=text.IndexOf("';",start,StringComparison.Ordinal);if(end<0)end=text.IndexOf('\'',start);if(end>start)Volatile.Write(ref currentTitle,text[start..end].Trim());}}}catch(OperationCanceledException){}catch{} }
    public void Dispose(){stop.Cancel();client.Dispose();stop.Dispose();}
}
public sealed class WaveFileSink(string path):IAudioSink { FileStream? f; int bytes; public void Start(int rate){f=File.Create(path);f.Write(new byte[44]);} public void Write(short[] b,CancellationToken ct){var x=MemoryMarshal.AsBytes(b.AsSpan());f!.Write(x);bytes+=x.Length;} public void Dispose(){if(f==null)return;f.Position=0;using(var w=new BinaryWriter(f,System.Text.Encoding.ASCII,true)){w.Write("RIFF"u8);w.Write(36+bytes);w.Write("WAVEfmt "u8);w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(192000);w.Write(384000);w.Write((short)2);w.Write((short)16);w.Write("data"u8);w.Write(bytes);}f.Dispose();f=null;} }

public sealed class WaveOutSink:IAudioSink
{
    IntPtr h; readonly List<(IntPtr mem,IntPtr hdr)> pending=[];
    public void Start(int rate){var fmt=new WAVEFORMATEX{wFormatTag=1,nChannels=1,nSamplesPerSec=(uint)rate,wBitsPerSample=16,nBlockAlign=2,nAvgBytesPerSec=(uint)(rate*2),cbSize=0};Check(waveOutOpen(out h,-1,ref fmt,IntPtr.Zero,IntPtr.Zero,0));}
    public void Write(short[] b,CancellationToken ct){Reap(false);while(pending.Count>=8){ct.ThrowIfCancellationRequested();Thread.Sleep(2);Reap(false);} int size=b.Length*2;var mem=Marshal.AllocHGlobal(size);Marshal.Copy(b,0,mem,b.Length);var wh=new WAVEHDR{lpData=mem,dwBufferLength=(uint)size};var hp=Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());Marshal.StructureToPtr(wh,hp,false);Check(waveOutPrepareHeader(h,hp,(uint)Marshal.SizeOf<WAVEHDR>()));Check(waveOutWrite(h,hp,(uint)Marshal.SizeOf<WAVEHDR>()));pending.Add((mem,hp));}
    void Reap(bool all){for(int i=pending.Count-1;i>=0;i--){var x=Marshal.PtrToStructure<WAVEHDR>(pending[i].hdr);if(all||(x.dwFlags&1)!=0){waveOutUnprepareHeader(h,pending[i].hdr,(uint)Marshal.SizeOf<WAVEHDR>());Marshal.FreeHGlobal(pending[i].mem);Marshal.FreeHGlobal(pending[i].hdr);pending.RemoveAt(i);}}}
    public void Dispose(){if(h==IntPtr.Zero)return;waveOutReset(h);Reap(true);waveOutClose(h);h=IntPtr.Zero;} static void Check(uint x){if(x!=0)throw new InvalidOperationException($"Windows audio error: {x}");}
    [StructLayout(LayoutKind.Sequential)] struct WAVEFORMATEX { public ushort wFormatTag,nChannels;public uint nSamplesPerSec,nAvgBytesPerSec;public ushort nBlockAlign,wBitsPerSample,cbSize; }
    [StructLayout(LayoutKind.Sequential)] struct WAVEHDR { public IntPtr lpData;public uint dwBufferLength,dwBytesRecorded;public IntPtr dwUser;public uint dwFlags,dwLoops;public IntPtr lpNext,reserved; }
    [DllImport("winmm.dll")]static extern uint waveOutOpen(out IntPtr h,int id,ref WAVEFORMATEX f,IntPtr cb,IntPtr inst,uint flags);[DllImport("winmm.dll")]static extern uint waveOutPrepareHeader(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveOutWrite(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveOutUnprepareHeader(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveOutReset(IntPtr h);[DllImport("winmm.dll")]static extern uint waveOutClose(IntPtr h);
}

public record AudioInputDevice(int Id,string Name){public override string ToString()=>Name;}
public sealed class WaveInSource(int deviceId=-1):IAudioSource
{
    const int CaptureRate=48000;WaveInEvent? input;BufferedWaveProvider? buffer;ISampleProvider? samples;bool primed;
    public static IReadOnlyList<AudioInputDevice> EnumerateDevices(){var list=new List<AudioInputDevice>();for(int i=0;i<WaveIn.DeviceCount;i++){var c=WaveIn.GetCapabilities(i);list.Add(new(i,c.ProductName));}return list;}
    public void Start(int rate){var format=new WaveFormat(CaptureRate,16,2);buffer=new BufferedWaveProvider(format){BufferDuration=TimeSpan.FromSeconds(1),DiscardOnBufferOverflow=true,ReadFully=false};ISampleProvider provider=buffer.ToSampleProvider();samples=rate==CaptureRate?provider:new WdlResamplingSampleProvider(provider,rate);input=new WaveInEvent{DeviceNumber=deviceId,WaveFormat=format,BufferMilliseconds=20,NumberOfBuffers=6};input.DataAvailable+=(_,e)=>buffer.AddSamples(e.Buffer,0,e.BytesRecorded);input.StartRecording();}
    public void Read(float[] output,int frames,CancellationToken ct){int needed=frames*2,done=0;if(!primed){while(buffer!.BufferedDuration<TimeSpan.FromMilliseconds(100)){ct.ThrowIfCancellationRequested();Thread.Sleep(2);}primed=true;}while(done<needed){ct.ThrowIfCancellationRequested();int read=samples!.Read(output,done,needed-done);if(read==0){Thread.Sleep(2);continue;}done+=read;}}
    public void Dispose(){if(input!=null){input.StopRecording();input.Dispose();input=null;}buffer=null;samples=null;primed=false;}
}
