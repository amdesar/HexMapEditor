using System;
namespace HexMapEditor
{
    internal sealed class ToolNumbers
    {
        internal int Size,Height;internal float Ink;
        internal bool Same(ToolNumbers b){return Size==b.Size&&Height==b.Height&&Ink==b.Ink;}
    }
    public sealed partial class MainWindow
    {
        ToolNumbers CaptureToolNumbers(){return new ToolNumbers{Size=brushSize,Height=brushHeight,Ink=inkToolSize};}
        void BindToolNumber(ScrubNumber number)
        {
            ToolNumbers before=null;
            number.EditStarting+=(s,e)=>{if(!updating)before=CaptureToolNumbers();};
            number.EditFinished+=(s,e)=>{if(before==null)return;commandTarget=number;var after=CaptureToolNumbers();if(!updating&&!e.Cancelled&&!before.Same(after)){PushAction(new EditAction{Label="Rozmiar narzędzia",BeforeTool=before,AfterTool=after});UpdateCommandState();}before=null;};
        }
        void ApplyToolNumbers(ToolNumbers numbers)
        {
            bool previous=updating;updating=true;
            try{brushSize=numbers.Size;brushHeight=numbers.Height;inkToolSize=numbers.Ink;sizePicker.Value=numbers.Size;heightPicker.Value=numbers.Height;inkSize.Value=(decimal)numbers.Ink;}
            finally{updating=previous;}
            UpdateBrushAvailability();canvas.Invalidate();
        }
    }
}
