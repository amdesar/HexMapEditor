using System;
namespace HexMapEditor
{
    internal static class BrushAngles
    {
        internal static decimal Wrap(decimal value){value=Decimal.Round(value,0,MidpointRounding.AwayFromZero);decimal next=value%360;return next==0&&value!=0?(value>0?360:-360):next;}
        internal static decimal Parse(string text,decimal previous)
        {
            if(!NumberTextBox.Draft(text)||String.IsNullOrEmpty(text)||text=="-"||text.EndsWith(".")||text.EndsWith(","))return previous;
            bool negative=text[0]=='-';string[] parts=(negative?text.Substring(1):text).Replace(',','.').Split('.');int modulo=0;bool nonzero=false;
            foreach(char c in parts[0]){modulo=(modulo*10+c-'0')%360;nonzero|=c!='0';}
            if(parts.Length>1&&parts[1].Length>0&&parts[1][0]>='5'){modulo=(modulo+1)%360;nonzero=true;}
            int result=modulo==0&&nonzero?360:modulo;return negative?-result:result;
        }
    }
}
