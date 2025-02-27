using System;
using System.Collections.Generic;
using System.Text;

namespace CPORLib.Tools
{
    public class OverspecifiedPreconditionException : Exception
    {
        public OverspecifiedPreconditionException()
        {
        }

        public OverspecifiedPreconditionException(string message) : base(message)
        {
        }
    }
}
