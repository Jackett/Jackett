namespace CurlSharp.Enums
{
    /// <summary>
    ///     Contains values used to specify the time condition when using
    ///     the <see cref="CurlOption.TimeCondition" /> option in a call
    ///     to <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlTimeCond
    {
        /// <summary>
        ///     Use no time condition.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The time condition is true if the resource has been modified
        ///     since the date/time passed in
        ///     <see cref="CurlOption.TimeValue" />.
        /// </summary>
        IfModSince = 1,

        /// <summary>
        ///     True if the resource has not been modified since the date/time
        ///     passed in <see cref="CurlOption.TimeValue" />.
        /// </summary>
        IfUnmodSince = 2,

        /// <summary>
        ///     True if the resource's last modification date/time equals that
        ///     passed in <see cref="CurlOption.TimeValue" />.
        /// </summary>
        LastMod = 3,

        /// <summary>
        ///     Last entry in enumeration; do not use in application code.
        /// </summary>
        Last = 4
    };
}