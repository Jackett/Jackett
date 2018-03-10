namespace CurlSharp.Enums
{
    /// <summary>
    ///     A member of this enumeration is passed to the function
    ///     <see cref="CurlShare.SetOpt" /> to configure a <see cref="CurlShare" />
    ///     transfer.
    /// </summary>
    public enum CurlShareOption
    {
        /// <summary>
        ///     Start-of-enumeration; do not use in application code.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The parameter, which should be a member of the
        ///     <see cref="CurlLockData" /> enumeration, specifies a type of
        ///     data that should be shared.
        /// </summary>
        Share = 1,

        /// <summary>
        ///     The parameter, which should be a member of the
        ///     <see cref="CurlLockData" /> enumeration, specifies a type of
        ///     data that should be unshared.
        /// </summary>
        Unshare = 2,

        /// <summary>
        ///     The parameter should be a reference to a
        ///     <see cref="CurlShare.CurlShareLockCallback" /> delegate.
        /// </summary>
        LockFunction = 3,

        /// <summary>
        ///     The parameter should be a reference to a
        ///     <see cref="CurlShare.CurlShareUnlockCallback" /> delegate.
        /// </summary>
        UnlockFunction = 4,

        /// <summary>
        ///     The parameter allows you to specify an object reference that
        ///     will passed to the <see cref="CurlShare.CurlShareLockCallback" /> delegate and
        ///     the <see cref="CurlShare.CurlShareUnlockCallback" /> delegate.
        /// </summary>
        UserData = 5,

        /// <summary>
        ///     End-of-enumeration; do not use in application code.
        /// </summary>
        Last = 6
    };
}