using System;
using System.Collections.Generic;
using System.Text;

namespace CPORLib.Tools
{
    public class DeadendException : Exception
    {
        public DeadendException(string message) : base(message)
        {
        }
    }
}
