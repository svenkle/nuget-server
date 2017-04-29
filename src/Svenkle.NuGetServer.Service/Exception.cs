using System;

namespace Svenkle.NuGetServer.Service
{
    public class PrematureTerminationException : Exception
    {
        public PrematureTerminationException(string message) : base(message)
        {

        }
    }
}
