using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Dapper;
using NLog;

namespace Jackett.Common.Utils
{
    public abstract class CustomSqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        private readonly Logger _logger;

        protected CustomSqliteTypeHandler(Logger logger)
        {
            _logger = logger;
        }

        protected string FormatValueForDb(T value)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.CanConvertTo(typeof(string)))
            {
                return converter.ConvertToString(value);
            }
            else
            {
                throw new NotSupportedException($"Conversion of {typeof(T)} to string is not supported.");
            }
        }

        protected virtual T ParseValueFromDb(object value)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.CanConvertFrom(value.GetType()))
            {
                return (T)converter.ConvertFrom(value);
            }
            else
            {
                throw new NotSupportedException(
                    $"Conversion from {value.GetType().Name} to {typeof(T).Name} is not supported.");
            }
        }

        public override void SetValue(IDbDataParameter parameter, T value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for type: {0}", typeof(T).Name);

            parameter.Value = FormatValueForDb(value);
        }

        public override T Parse(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for type: {0}, Value: {1}", typeof(T).Name, value);

            return ParseValueFromDb(value);
        }
    }

    public class UriHandler : CustomSqliteTypeHandler<Uri>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, Uri value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for Uri: {0}", value);

            parameter.Value = value?.ToString();
        }

        protected override Uri ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for Uri: {0}", value);

            return new Uri((string)value);
        }

        public UriHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class ICollectionIntHandler : CustomSqliteTypeHandler<ICollection<int>>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, ICollection<int> value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for ICollection<int>: {0}", string.Join(", ", value));

            parameter.Value = string.Join(",", value);
        }

        protected override ICollection<int> ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for ICollection<int>: {0}", value);

            return ((string)value).Split(',').Select(int.Parse).ToList();
        }

        public ICollectionIntHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class ICollectionStringHandler : CustomSqliteTypeHandler<ICollection<string>>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, ICollection<string> value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for ICollection<string>: {0}", string.Join(", ", value));

            parameter.Value = string.Join(",", value);
        }

        protected override ICollection<string> ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for ICollection<string>: {0}", value);

            return ((string)value).Split(',').ToList();
        }

        public ICollectionStringHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }
    public class StringHandler : CustomSqliteTypeHandler<string>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, string value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for string: {0}", value);

            parameter.Value = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        protected override string ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for string: {0}", value);

            return value as string ?? string.Empty;
        }

        public StringHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class DoubleHandler : CustomSqliteTypeHandler<double>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, double value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for double: {0}", value);

            parameter.Value = value;
        }

        protected override double ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for double: {0}", value);

            return Convert.ToDouble(value);
        }

        public DoubleHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class FloatHandler : CustomSqliteTypeHandler<float>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, float value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for float: {0}", value);

            parameter.Value = value;
        }

        protected override float ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for float: {0}", value);

            return Convert.ToSingle(value);
        }

        public FloatHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class LongHandler : CustomSqliteTypeHandler<long>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, long value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for long: {0}", value);

            parameter.Value = value;
        }

        protected override long ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for long: {0}", value);

            return Convert.ToInt64(value);
        }

        public LongHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class DateTimeHandler : CustomSqliteTypeHandler<DateTime>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for DateTime: {0}", value);

            parameter.Value = value;//.ToString("O");
        }

        protected override DateTime ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for DateTime: {0}", value);

            return DateTime.Parse((string)value);
        }

        public DateTimeHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }

    public class NullableDateTimeHandler : CustomSqliteTypeHandler<DateTime?>
    {
        private readonly Logger _logger;
        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Setting value for DateTime: {0}", value);

            if (value == null)
            {
                parameter.Value = DBNull.Value;
            }
            else
            {
                parameter.Value = value;
            }
        }

        protected override DateTime? ParseValueFromDb(object value)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Parsing value for DateTime: {0}", value);

            return DateTime.Parse((string)value);
        }

        public NullableDateTimeHandler(Logger logger) : base(logger)
        {
            _logger = logger;
        }
    }
}
