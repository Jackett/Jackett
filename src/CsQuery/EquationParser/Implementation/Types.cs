using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.EquationParser.Implementation
{

    interface IIntegerValue : IComparable, IConvertible,
        IEquatable<byte>, IEquatable<short>, IEquatable<ushort>,
        IEquatable<int>, IEquatable<uint>,
        IEquatable<long>, IEquatable<ulong>,
        IEquatable<double>, IEquatable<float>,
        IComparable<byte>, IComparable<short>, IComparable<ushort>,
        IComparable<int>, IComparable<uint>,
        IComparable<long>, IComparable<ulong>
    {

    }
    interface INumericValue : IIntegerValue,
        IComparable<double>, IComparable<float>, IComparable<decimal>,
        IEquatable<double>, IEquatable<float>, IEquatable<decimal>
    {

    }
    interface ITextValue : IComparable, IConvertible,
        IComparable<string>, IComparable<char>,
        IEquatable<string>, IEquatable<char>
    {

    }
    interface IBooleanValue : IComparable, IConvertible,
        IComparable<bool>, IEquatable<bool>
    {

    }
}
