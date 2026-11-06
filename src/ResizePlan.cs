using System.Collections.Generic;
namespace HexMapEditor
{
    internal sealed class ResizePlan
    {
        public ResizeState State;
        public readonly List<int> RemovedIndices = new List<int>(), DataIndices = new List<int>();
        public bool IsEmpty { get { return State == null; } }
    }
}
