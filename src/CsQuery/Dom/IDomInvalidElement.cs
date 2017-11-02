using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// An element that will be rendered as text because it was determined to be a mismatched tag
    /// </summary>
    [Obsolete]
    public interface IDomInvalidElement : IDomText
    {

    }
}
