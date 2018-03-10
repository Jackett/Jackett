namespace CurlSharp.Enums
{
    /// <summary>
    ///     These are options available to build a multi-part form section
    ///     in a call to <see cref="CurlHttpMultiPartForm.AddSection" />
    /// </summary>
    public enum CurlFormOption
    {
        /// <summary>
        ///     Another possibility to send options to
        ///     <see cref="CurlHttpMultiPartForm.AddSection" /> is this option, that
        ///     passes a <see cref="CurlForms" /> array reference as its value.
        ///     Each <see cref="CurlForms" /> array element has a
        ///     <see cref="CurlFormOption" /> and a <c>string</c>. All available
        ///     options can be used in an array, except the <c>Array</c>
        ///     option itself! The last argument in such an array must always be
        ///     <c>End</c>.
        /// </summary>
        Array = 8,

        /// <summary>
        ///     Followed by a <c>string</c>, tells libcurl that a buffer is to be
        ///     used to upload data instead of using a file.
        /// </summary>
        Buffer = 11,

        /// <summary>
        ///     Followed by an <c>int</c> with the size of the
        ///     <c>BufferPtr</c> byte array, tells libcurl the length of
        ///     the data to upload.
        /// </summary>
        BufferLength = 13,

        /// <summary>
        ///     Followed by a <c>byte[]</c> array, tells libcurl the address of
        ///     the buffer containing data to upload (as indicated with
        ///     <c>Buffer</c>). You must also use
        ///     <c>BufferLength</c> to set the length of the buffer area.
        /// </summary>
        BufferPtr = 12,

        /// <summary>
        ///     Specifies extra headers for the form POST section. This takes an
        ///     <see cref="CurlSlist" /> prepared in the usual way using
        ///     <see cref="CurlSlist.Append" /> and appends the list of headers to
        ///     those libcurl automatically generates.
        /// </summary>
        ContentHeader = 15,

        /// <summary>
        ///     Followed by an <c>int</c> setting the length of the contents.
        /// </summary>
        ContentsLength = 6,

        /// <summary>
        ///     Followed by a <c>string</c> with a content-type will make cURL
        ///     use this given content-type for this file upload part, possibly
        ///     instead of an internally chosen one.
        /// </summary>
        ContentType = 14,

        /// <summary>
        ///     Followed by a <c>string</c> is used for the contents of this part, the
        ///     actual data to send away. If you'd like it to contain zero bytes,
        ///     you need to set the length of the name with
        ///     <c>ContentsLength</c>.
        /// </summary>
        CopyContents = 4,

        /// <summary>
        ///     Followed by a <c>string</c> used to set the name of this part.
        ///     If you'd like it to contain zero bytes, you need to set the
        ///     length of the name with <c>NameLength</c>.
        /// </summary>
        CopyName = 1,

        /// <summary>
        ///     This should be the last argument to a call to
        ///     <see cref="CurlHttpMultiPartForm.AddSection" />.
        /// </summary>
        End = 17,

        /// <summary>
        ///     Followed by a file name, makes this part a file upload part. It
        ///     sets the file name field to the actual file name used here,
        ///     it gets the contents of the file and passes as data and sets the
        ///     content-type if the given file match one of the new internally
        ///     known file extension. For <c>File</c> the user may send
        ///     one or more files in one part by providing multiple <c>File</c>
        ///     arguments each followed by the filename (and each <c>File</c>
        ///     is allowed to have a <c>ContentType</c>).
        /// </summary>
        File = 10,

        /// <summary>
        ///     Followed by a file name, and does the file read: the contents
        ///     will be used in as data in this part.
        /// </summary>
        FileContent = 7,

        /// <summary>
        ///     Followed by a <c>string</c> file name, will make libcurl use the
        ///     given name in the file upload part, instead of the actual file
        ///     name given to <c>File</c>.
        /// </summary>
        Filename = 16,

        /// <summary>
        ///     Followed by an <c>int</c> setting the length of the name.
        /// </summary>
        NameLength = 3,

        /// <summary>
        ///     Not used.
        /// </summary>
        Nothing = 0,

        /// <summary>
        ///     No longer used.
        /// </summary>
        Obsolete = 9,

        /// <summary>
        ///     No longer used.
        /// </summary>
        Obsolete2 = 18,

        /// <summary>
        ///     Followed by a <c>byte[]</c> used for the contents of this part.
        ///     If you'd like it to contain zero bytes, you need to set the
        ///     length of the name with <c>ContentsLength</c>.
        /// </summary>
        PtrContents = 5,

        /// <summary>
        ///     Followed by a <c>byte[]</c> used for the name of this part.
        ///     If you'd like it to contain zero bytes, you need to set the
        ///     length of the name with <c>NameLength</c>.
        /// </summary>
        PtrName = 2
    };
}