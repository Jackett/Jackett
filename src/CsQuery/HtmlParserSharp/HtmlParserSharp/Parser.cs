using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HtmlParserSharp.Core;
using HtmlParserSharp.Common;


namespace HtmlParserSharp
{
    /// <summary>
    /// Generic parser that accepts any ITokenHandler implementation. It's up to the client to poll
    /// the resulting document from their implementation.
    /// </summary>

    public class Parser
    {
        /// <summary>
        /// Creates a new TokenHandler and parses the html with it.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="html">
        /// The HTML.
        /// </param>
        ///
        /// <returns>
        /// The populated TokenHandler
        /// </returns>

        public static T Create<T>(string html) where T: ITokenHandler, new()
        {
            T tokenHandler = new T();
            var parser = new Parser(tokenHandler);
            parser.Parse(html);
            return tokenHandler;
        }

        public Parser(ITokenHandler treeBuilder)
        {
            TreeBuilder = treeBuilder;
        }

        private Tokenizer Tokenizer;
        private ITokenHandler TreeBuilder;

        

        public void Parse(string html)
        {
            using (var reader = new StringReader(html))
            {
                Tokenize(reader);
            }
        }


        private void Reset()
        {
            TreeBuilder = new XmlTreeBuilder();
            Tokenizer = new Tokenizer(TreeBuilder, false);
        }

        private void Tokenize(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader was null.");
            }

            Tokenizer.Start();
            bool swallowBom = true;

            try
            {
                char[] buffer = new char[2048];
                UTF16Buffer bufr = new UTF16Buffer(buffer, 0, 0);
                bool lastWasCR = false;
                int len = -1;
                if ((len = reader.Read(buffer, 0, buffer.Length)) != 0)
                {
                    int streamOffset = 0;
                    int offset = 0;
                    int length = len;
                    if (swallowBom)
                    {
                        if (buffer[0] == '\uFEFF')
                        {
                            streamOffset = -1;
                            offset = 1;
                            length--;
                        }
                    }
                    if (length > 0)
                    {
                        Tokenizer.SetTransitionBaseOffset(streamOffset);
                        bufr.Start = offset;
                        bufr.End = offset + length;
                        while (bufr.HasMore)
                        {
                            bufr.Adjust(lastWasCR);
                            lastWasCR = false;
                            if (bufr.HasMore)
                            {
                                lastWasCR = Tokenizer.TokenizeBuffer(bufr);
                            }
                        }
                    }
                    streamOffset = length;
                    while ((len = reader.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        Tokenizer.SetTransitionBaseOffset(streamOffset);
                        bufr.Start = 0;
                        bufr.End = len;
                        while (bufr.HasMore)
                        {
                            bufr.Adjust(lastWasCR);
                            lastWasCR = false;
                            if (bufr.HasMore)
                            {
                                lastWasCR = Tokenizer.TokenizeBuffer(bufr);
                            }
                        }
                        streamOffset += len;
                    }
                }
                Tokenizer.Eof();
            }
            finally
            {
                Tokenizer.End();
            }
        }
    }
}
