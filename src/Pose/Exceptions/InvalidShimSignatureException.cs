namespace Pose.Exceptions
{
    [System.Serializable]
    internal class InvalidShimSignatureException : System.Exception
    {
        public InvalidShimSignatureException() : base() { }
        public InvalidShimSignatureException(string message) : base(message) { }
        public InvalidShimSignatureException(string message, System.Exception inner) : base(message, inner) { }
        protected InvalidShimSignatureException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}