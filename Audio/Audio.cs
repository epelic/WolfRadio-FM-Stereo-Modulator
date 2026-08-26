using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Concurrent;
using FmStereoModulator.Dsp;
namespace FmStereoModulator.Audio;
public interface IAudioSource:IDisposable { void Start(int rate); void Read(float[] stereo,int frames,CancellationToken ct); }
public interface IAudioSink:IDisposable { void Start(int rate); void Write(short[] mono,CancellationToken ct); }
public sealed class HackRfSink(double frequencyMhz, uint txGain, bool amplifierOn):IAudioSink
{
    const int IqRate=2_000_000; readonly BlockingCollection<byte[]> queue=new(8); IntPtr device; TxCallback? callback; double fmPhase; bool initialized,started;
    public void Start(int inputRate)
    {
        try { Check(hackrf_init(),"inizializzazione");initialized=true;Check(hackrf_open(out device),"apertura dispositivo");Check(hackrf_set_freq(device,(ulong)(frequencyMhz*1_000_000)),"frequenza");Check(hackrf_set_sample_rate(device,IqRate),"sample rate");Check(hackrf_set_baseband_filter_bandwidth(device,1_750_000),"filtro baseband");Check(hackrf_set_txvga_gain(device,Math.Min(txGain,47)),"guadagno TX");
            Check(hackrf_set_amp_enable(device,amplifierOn?(byte)0:(byte)1),"amplificatore RF");callback=OnTransfer;Check(hackrf_start_tx(device,callback,IntPtr.Zero),"avvio TX");started=true;
        } catch(DllNotFoundException){throw new InvalidOperationException("hackrf.dll non trovata. Installare il driver WinUSB e copiare libhackrf nel pacchetto WolfRadio.");}
    }
    public void Write(short[] mono,CancellationToken ct)
    {
        int samples=(int)Math.Round(mono.Length*(double)IqRate/MpxEngine.SampleRate);var iq=new byte[samples*2];for(int j=0;j<samples;j++){double src=j*(double)MpxEngine.SampleRate/IqRate;int a=Math.Min((int)src,mono.Length-1),b=Math.Min(a+1,mono.Length-1);double f=mono[a]+(mono[b]-mono[a])*(src-a);fmPhase+=2*Math.PI*75_000/IqRate*(f/32768.0);if(fmPhase>Math.PI)fmPhase-=2*Math.PI;int k=j*2;iq[k]=unchecked((byte)(sbyte)(Math.Cos(fmPhase)*110));iq[k+1]=unchecked((byte)(sbyte)(Math.Sin(fmPhase)*110));}queue.Add(iq,ct);
    }
    int OnTransfer(ref Transfer t){if(!queue.TryTake(out var data,20))data=new byte[t.valid_length];int n=Math.Min(data.Length,t.valid_length);Marshal.Copy(data,0,t.buffer,n);if(n<t.valid_length){var zero=new byte[t.valid_length-n];Marshal.Copy(zero,0,t.buffer+n,zero.Length);}return 0;}
    public void Dispose(){queue.CompleteAdding();if(started)hackrf_stop_tx(device);if(device!=IntPtr.Zero)hackrf_close(device);if(initialized)hackrf_exit();started=initialized=false;device=IntPtr.Zero;}
    static void Check(int code,string step){if(code!=0)throw new InvalidOperationException($"HackRF: errore durante {step} (codice {code}). Verificare collegamento e driver WinUSB.");}
    [StructLayout(LayoutKind.Sequential)]struct Transfer{public IntPtr device,buffer;public int buffer_length,valid_length;public IntPtr rx_ctx,tx_ctx;} [UnmanagedFunctionPointer(CallingConvention.Cdecl)]delegate int TxCallback(ref Transfer transfer);
    [DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_init();[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_exit();[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_open(out IntPtr d);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_close(IntPtr d);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_freq(IntPtr d,ulong hz);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_sample_rate(IntPtr d,double hz);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_baseband_filter_bandwidth(IntPtr d,uint hz);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_txvga_gain(IntPtr d,uint gain);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_set_amp_enable(IntPtr d,byte enable);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_start_tx(IntPtr d,TxCallback cb,IntPtr ctx);[DllImport("hackrf",CallingConvention=CallingConvention.Cdecl)]static extern int hackrf_stop_tx(IntPtr d);
}
public sealed class TestToneSource:IAudioSource { int rate; long n; public void Start(int r)=>rate=r; public void Read(float[] b,int frames,CancellationToken ct){for(int i=0;i<frames;i++){b[2*i]=(float)(.55*Math.Sin(2*Math.PI*1000*n/rate));b[2*i+1]=(float)(.35*Math.Sin(2*Math.PI*1300*n/rate));n++;}} public void Dispose(){} }
public sealed class WaveFileSink(string path):IAudioSink { FileStream? f; int bytes; public void Start(int rate){f=File.Create(path);f.Write(new byte[44]);} public void Write(short[] b,CancellationToken ct){var x=MemoryMarshal.AsBytes(b.AsSpan());f!.Write(x);bytes+=x.Length;} public void Dispose(){if(f==null)return;f.Position=0;using var w=new BinaryWriter(f,System.Text.Encoding.ASCII,true);w.Write("RIFF"u8);w.Write(36+bytes);w.Write("WAVEfmt "u8);w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(192000);w.Write(384000);w.Write((short)2);w.Write((short)16);w.Write("data"u8);w.Write(bytes);f.Dispose();f=null;} }

public sealed class WaveOutSink:IAudioSink
{
    IntPtr h; readonly List<(IntPtr mem,IntPtr hdr)> pending=[];
    public void Start(int rate){var fmt=new WAVEFORMATEX{wFormatTag=1,nChannels=1,nSamplesPerSec=(uint)rate,wBitsPerSample=16,nBlockAlign=2,nAvgBytesPerSec=(uint)(rate*2),cbSize=0};Check(waveOutOpen(out h,-1,ref fmt,IntPtr.Zero,IntPtr.Zero,0));}
    public void Write(short[] b,CancellationToken ct){Reap(false);while(pending.Count>=8){ct.ThrowIfCancellationRequested();Thread.Sleep(2);Reap(false);} int size=b.Length*2;var mem=Marshal.AllocHGlobal(size);Marshal.Copy(b,0,mem,b.Length);var wh=new WAVEHDR{lpData=mem,dwBufferLength=(uint)size};var hp=Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());Marshal.StructureToPtr(wh,hp,false);Check(waveOutPrepareHeader(h,hp,(uint)Marshal.SizeOf<WAVEHDR>()));Check(waveOutWrite(h,hp,(uint)Marshal.SizeOf<WAVEHDR>()));pending.Add((mem,hp));}
    void Reap(bool all){for(int i=pending.Count-1;i>=0;i--){var x=Marshal.PtrToStructure<WAVEHDR>(pending[i].hdr);if(all||(x.dwFlags&1)!=0){waveOutUnprepareHeader(h,pending[i].hdr,(uint)Marshal.SizeOf<WAVEHDR>());Marshal.FreeHGlobal(pending[i].mem);Marshal.FreeHGlobal(pending[i].hdr);pending.RemoveAt(i);}}}
    public void Dispose(){if(h==IntPtr.Zero)return;waveOutReset(h);Reap(true);waveOutClose(h);h=IntPtr.Zero;} static void Check(uint x){if(x!=0)throw new InvalidOperationException($"Errore audio Windows: {x}");}
    [StructLayout(LayoutKind.Sequential)] struct WAVEFORMATEX { public ushort wFormatTag,nChannels;public uint nSamplesPerSec,nAvgBytesPerSec;public ushort nBlockAlign,wBitsPerSample,cbSize; }
    [StructLayout(LayoutKind.Sequential)] struct WAVEHDR { public IntPtr lpData;public uint dwBufferLength,dwBytesRecorded;public IntPtr dwUser;public uint dwFlags,dwLoops;public IntPtr lpNext,reserved; }
    [DllImport("winmm.dll")]static extern uint waveOutOpen(out IntPtr h,int id,ref WAVEFORMATEX f,IntPtr cb,IntPtr inst,uint flags);[DllImport("winmm.dll")]static extern uint waveOutPrepareHeader(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveOutWrite(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveOutUnprepareHeader(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveOutReset(IntPtr h);[DllImport("winmm.dll")]static extern uint waveOutClose(IntPtr h);
}

public sealed class WaveInSource:IAudioSource
{
    IntPtr h,mem,hdr; int frames;
    public void Start(int rate){frames=1920;var f=new Fmt{tag=1,ch=2,rate=(uint)rate,avg=(uint)rate*4,align=4,bits=16};Check(waveInOpen(out h,-1,ref f,IntPtr.Zero,IntPtr.Zero,0));mem=Marshal.AllocHGlobal(frames*4);hdr=Marshal.AllocHGlobal(Marshal.SizeOf<Hdr>());Queue();Check(waveInStart(h));}
    void Queue(){Marshal.StructureToPtr(new Hdr{data=mem,len=(uint)(frames*4)},hdr,false);Check(waveInPrepareHeader(h,hdr,(uint)Marshal.SizeOf<Hdr>()));Check(waveInAddBuffer(h,hdr,(uint)Marshal.SizeOf<Hdr>()));}
    public void Read(float[] b,int count,CancellationToken ct){while((Marshal.PtrToStructure<Hdr>(hdr).flags&1)==0){ct.ThrowIfCancellationRequested();Thread.Sleep(1);}var s=new short[count*2];Marshal.Copy(mem,s,0,s.Length);for(int i=0;i<s.Length;i++)b[i]=s[i]/32768f;waveInUnprepareHeader(h,hdr,(uint)Marshal.SizeOf<Hdr>());Queue();}
    public void Dispose(){if(h==IntPtr.Zero)return;waveInReset(h);waveInUnprepareHeader(h,hdr,(uint)Marshal.SizeOf<Hdr>());waveInClose(h);Marshal.FreeHGlobal(mem);Marshal.FreeHGlobal(hdr);h=IntPtr.Zero;} static void Check(uint x){if(x!=0)throw new InvalidOperationException($"Errore ingresso Windows: {x}");}
    [StructLayout(LayoutKind.Sequential)]struct Fmt{public ushort tag,ch;public uint rate,avg;public ushort align,bits,extra;} [StructLayout(LayoutKind.Sequential)]struct Hdr{public IntPtr data;public uint len,recorded;public IntPtr user;public uint flags,loops;public IntPtr next,reserved;}
    [DllImport("winmm.dll")]static extern uint waveInOpen(out IntPtr h,int id,ref Fmt f,IntPtr cb,IntPtr ins,uint fl);[DllImport("winmm.dll")]static extern uint waveInPrepareHeader(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveInAddBuffer(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveInStart(IntPtr h);[DllImport("winmm.dll")]static extern uint waveInReset(IntPtr h);[DllImport("winmm.dll")]static extern uint waveInUnprepareHeader(IntPtr h,IntPtr p,uint s);[DllImport("winmm.dll")]static extern uint waveInClose(IntPtr h);
}
