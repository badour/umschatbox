namespace System.Web
{
    // WebForms HttpContext is unavailable on Linux test runs; production tracing is skipped when Current is null.
    public sealed class HttpContext
    {
        public static HttpContext Current
        {
            get { return null; }
        }

        public TraceContext Trace
        {
            get { return null; }
        }
    }

    public sealed class TraceContext
    {
        public void Warn(string category, string message, Exception errorInfo)
        {
        }
    }
}
