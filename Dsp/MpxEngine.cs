using FmStereoModulator.Audio;
using FmStereoModulator.Rds;
namespace FmStereoModulator.Dsp;
public record MpxConfig(ushort Pi, string Ps, string RadioText, bool Rds, bool Stereo, bool CarrierOnly, bool SoftCompressor, double PreemphasisUs, double Level);
public record MpxTelemetry(double LeftPeak, double RightPeak, double[] Spectrum);
public sealed class MpxEngine(MpxConfig cfg, IAudioSource source, IAudioSink sink, Action<MpxTelemetry>? telemetry=null)
{
    public const int SampleRate=192000; const int Block=1920;
    public void Run(CancellationToken ct)
    {
        using(source) using(sink) { source.Start(SampleRate); sink.Start(SampleRate); var lr=new float[Block*2]; var pcm=new short[Block]; var scope=new double[256]; var rds=new RdsEncoder(cfg.Pi,cfg.Ps,cfg.RadioText,SampleRate); double phase=0; var pre=new Preemphasis(SampleRate,cfg.PreemphasisUs*1e-6); var compressor=new StereoCompressor(SampleRate); int blocks=0;
            while(!ct.IsCancellationRequested) { source.Read(lr,Block,ct); double lp=0,rp=0; for(int i=0;i<Block;i++) { double l=lr[i*2]*cfg.Level, rr=lr[i*2+1]*cfg.Level;if(cfg.SoftCompressor)compressor.Process(ref l,ref rr); lp=Math.Max(lp,Math.Abs(l));rp=Math.Max(rp,Math.Abs(rr)); l=pre.ProcessL(l);rr=pre.ProcessR(rr); double sum=.45*(l+rr), diff=.45*(l-rr); double pilot=cfg.Stereo?.09*Math.Sin(phase):0; double stereo=cfg.Stereo?diff*Math.Sin(phase*2):0; double rv=cfg.Rds?.035*rds.Next():0; double x=cfg.CarrierOnly?0:sum+pilot+stereo+rv; if(!cfg.CarrierOnly)x=Math.Tanh(x*1.15)/Math.Tanh(1.15); pcm[i]=(short)Math.Clamp(x*32767,-32767,32767); if(i<scope.Length)scope[i]=x; phase+=2*Math.PI*19000/SampleRate; if(phase>=2*Math.PI)phase-=2*Math.PI; } sink.Write(pcm,ct); if(++blocks%5==0)telemetry?.Invoke(new(lp,rp,Spectrum(scope))); }
        }
    }
    static double[] Spectrum(double[] x){int n=x.Length,bins=n/2;var y=new double[bins];for(int k=0;k<bins;k++){double re=0,im=0;for(int i=0;i<n;i++){double w=.5-.5*Math.Cos(2*Math.PI*i/(n-1)),a=2*Math.PI*k*i/n;re+=x[i]*w*Math.Cos(a);im-=x[i]*w*Math.Sin(a);}double db=20*Math.Log10(Math.Sqrt(re*re+im*im)/(n*.5)+1e-9);y[k]=Math.Clamp((db+80)/80,0,1);}return y;}
}
sealed class StereoCompressor
{
    readonly double attack,release;double envelope,gain=1;
    public StereoCompressor(int fs){attack=1-Math.Exp(-1.0/(fs*.010));release=1-Math.Exp(-1.0/(fs*.180));}
    public void Process(ref double left,ref double right){double peak=Math.Max(Math.Abs(left),Math.Abs(right));double envRate=peak>envelope?attack:release;envelope+=envRate*(peak-envelope);const double threshold=.25,ratio=3.0,makeup=1.25;double target=makeup;if(envelope>threshold){double compressed=threshold*Math.Pow(envelope/threshold,1/ratio);target=makeup*compressed/envelope;}double gainRate=target<gain?attack:release;gain+=gainRate*(target-gain);left*=gain;right*=gain;}
}
sealed class Preemphasis
{
    readonly double derivativeGain,lowpassA;double previousL,previousR,filteredL,filteredR;
    public Preemphasis(int fs,double tau){derivativeGain=tau*fs;lowpassA=1-Math.Exp(-2*Math.PI*15000/fs);}
    public double ProcessL(double x){filteredL+=lowpassA*(x-filteredL);double y=filteredL+derivativeGain*(filteredL-previousL);previousL=filteredL;return y;}
    public double ProcessR(double x){filteredR+=lowpassA*(x-filteredR);double y=filteredR+derivativeGain*(filteredR-previousR);previousR=filteredR;return y;}
}
