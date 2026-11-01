#if NETFRAMEWORK
using System;

namespace BrightIdeasSoftware
{
    internal static class ArgumentNullExceptionExtensions
    {
        extension(ArgumentNullException)
        {
            internal static void ThrowIfNull(object argument, string paramName) {
                if (argument == null)
                {
                    throw new ArgumentNullException(paramName);
                }
            }
        }
    }
}
#endif
