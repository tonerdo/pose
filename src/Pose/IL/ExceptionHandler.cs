using System;

namespace Pose.IL
{
    internal struct ExceptionHandler
    {
        public Type CatchType;
        public string Flag;
        public int TryStart;
        public int TryEnd;
        public int HandlerStart;
        public int HandlerEnd;
    }
}