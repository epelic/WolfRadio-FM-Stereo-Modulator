namespace FmStereoModulator.Rds;
public sealed class RdsEncoder
{
    readonly ushort pi; readonly string ps,rt; readonly double fs; Queue<int> bits=new(); double bitPhase,carrierPhase; int symbol=1,prev=1,group;
    public RdsEncoder(ushort p,string station,string text,int sampleRate){pi=p;ps=(station??"").PadRight(8)[..8];rt=(text??"").PadRight(64)[..64];fs=sampleRate;Fill();}
    public double Next(){bitPhase+=1187.5/fs;if(bitPhase>=1){bitPhase-=1;if(bits.Count<104)Fill();int data=bits.Dequeue();prev^=data;symbol=prev==0?1:-1;}double half=bitPhase<.5?1:-1;carrierPhase+=2*Math.PI*57000/fs;if(carrierPhase>=2*Math.PI)carrierPhase-=2*Math.PI;return symbol*half*Math.Sin(carrierPhase);}
    void Fill(){bool typeA=(group++%5)==0;int seg=typeA?(group/5)%16:group%4;ushort b1=pi,b2,b3,b4;if(typeA){b2=(ushort)(0x2000|seg);b3=(ushort)((rt[seg*4]<<8)|rt[seg*4+1]);b4=(ushort)((rt[seg*4+2]<<8)|rt[seg*4+3]);}else{b2=(ushort)seg;b3=pi;b4=(ushort)((ps[seg*2]<<8)|ps[seg*2+1]);}AddBlock(b1,0x0FC);AddBlock(b2,0x198);AddBlock(b3,0x168);AddBlock(b4,0x1B4);}
    void AddBlock(ushort data,int offset){uint v=(uint)data<<10;for(int i=25;i>=10;i--)if(((v>>i)&1)!=0)v^=0x5B9u<<(i-10);uint block=((uint)data<<10)|((v^(uint)offset)&0x3ff);for(int i=25;i>=0;i--)bits.Enqueue((int)((block>>i)&1));}
}
