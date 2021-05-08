using NUnit.Framework;

namespace Jackett.Test.TestHelpers
{
    internal static class TestExceptions
    {
        public static AssertionException UnexpectedInvocation => new AssertionException("Unexpected Invocation");
    }
}
