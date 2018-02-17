using System;
using System.Data;
using log4net.Core;
using log4net.Layout;

namespace Log4net.NetCore.Lib.Parameters
{
    public class AdoNetAppenderParameter
    {
        #region Fields
        private DbType _DbType;
        private bool _InferType = true;
        #endregion

        #region Public Properties
        public string ParameterName { get; set; }

        public DbType DbType
        {
            get { return _DbType; }
            set
            {
                _DbType = value;
                _InferType = false;
            }
        }

        public byte Precision { get; set; }

        public byte Scale { get; set; }

        public int Size { get; set; }

        public IRawLayout Layout { get; set; }
        #endregion

        public AdoNetAppenderParameter() { }

        #region Public Methods
        virtual public void Prepare(IDbCommand command)
        {
            IDbDataParameter param = command.CreateParameter();

            param.ParameterName = ParameterName;

            if (!_InferType)
                param.DbType = _DbType;

            if (Precision != 0)
                param.Precision = Precision;

            if (Scale != 0)
                param.Scale = Scale;

            if (Size != 0)
                param.Size = Size;

            command.Parameters.Add(param);
        }

        virtual public void FormatValue(IDbCommand command, LoggingEvent loggingEvent)
        {
            IDbDataParameter param = (IDbDataParameter)command.Parameters[ParameterName];

            object formattedValue = Layout.Format(loggingEvent);

            if (formattedValue == null)
            {
                formattedValue = DBNull.Value;
            }

            param.Value = formattedValue;
        }
        #endregion
    }
}
