using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Models
{
    public enum ErrorSeverity
    {
        Critical,   // App cannot continue, requires immediate attention
        Error,      // Feature is broken but app can continue
        Warning,    // Something is wrong but not critical
        Info        // Informational error, minor issue
    }
}
