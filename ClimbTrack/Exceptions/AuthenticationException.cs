using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Exceptions
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    public class NetworkException : Exception
    {
        public NetworkConnectivity Status { get; }

        public NetworkException(string message, NetworkConnectivity status)
            : base(message)
        {
            Status = status;
        }
    }

    public enum NetworkConnectivity
    {
        NoConnection,
        Limited,
        Unstable
    }

    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }
        public DataAccessException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
