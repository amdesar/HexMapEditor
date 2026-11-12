using System;
using System.Diagnostics;
namespace HexMapEditor
{
    internal sealed class InkTiming
    {
        [ThreadStatic] internal static InkTiming Current;
        internal double Build,Contains,Exclude,Serialize,Validate,History,Snapshot,ApplyUI;
        internal static T Measure<T>(Func<T> operation,Action<InkTiming,double> record)
        {
            if(Current==null)return operation();var watch=Stopwatch.StartNew();
            try{return operation();}finally{record(Current,watch.Elapsed.TotalMilliseconds);}
        }
        public override string ToString(){return String.Format("build={0:F2} contains={1:F2} Exclude={2:F2} serialize={3:F2} validate={4:F2} history={5:F2} snapshot={6:F2} applyUI={7:F2} ms",Build,Contains,Exclude,Serialize,Validate,History,Snapshot,ApplyUI);}
    }
}
