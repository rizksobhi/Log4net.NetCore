using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Reflection;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using Log4net.NetCore.Lib.Parameters;

namespace Log4net.NetCore.Lib.Appenders
{
    public class AdoNetAppender : BufferingAppenderSkeleton
    {
        private readonly static Type declaringType = typeof(AdoNetAppender);

        #region Fields
        private IDbCommand _DbCommand;
        protected bool _UsePreparedCommand;
        protected ArrayList _Parameters;
        #endregion

        #region Properties
        public string ConnectionString { get; set; }

        public string AppSettingsKey { get; set; }

        public string ConnectionType { get; set; }

        public string CommandText { get; set; }

        public CommandType CommandType { get; set; } = CommandType.Text;

        public bool UseTransactions { get; set; } = true;

        public SecurityContext SecurityContext { get; set; }

        public bool ReconnectOnError { get; set; } = false;

        protected IDbConnection Connection { get; set; }
        #endregion

        public AdoNetAppender()
        {
            ConnectionType = "System.Data.OleDb.OleDbConnection, System.Data, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            _Parameters = new ArrayList();
        }

        #region Methods
        override public void ActivateOptions()
        {
            base.ActivateOptions();

            _UsePreparedCommand = (CommandText != null && CommandText.Length > 0);

            if (SecurityContext == null)
            {
                SecurityContext = SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);
            }

            InitializeDatabaseConnection();
            InitializeDatabaseCommand();
        }

        public void AddParameter(AdoNetAppenderParameter parameter)
        {
            _Parameters.Add(parameter);
        }

        override protected void OnClose()
        {
            base.OnClose();
            DisposeCommand(false);
            DiposeConnection();
        }

        override protected void SendBuffer(LoggingEvent[] events)
        {
            if (ReconnectOnError && (Connection == null || Connection.State != ConnectionState.Open))
            {
                LogLog.Debug(declaringType, "Attempting to reconnect to database. Current Connection State: " + ((Connection == null) ? SystemInfo.NullText : Connection.State.ToString()));

                InitializeDatabaseConnection();
                InitializeDatabaseCommand();
            }

            if (Connection != null && Connection.State == ConnectionState.Open)
            {
                if (UseTransactions)
                {
                    IDbTransaction dbTran = null;
                    try
                    {
                        dbTran = Connection.BeginTransaction();
                        SendBuffer(dbTran, events);
                        dbTran.Commit();
                    }
                    catch (Exception ex)
                    {
                        if (dbTran != null)
                        {
                            try
                            {
                                dbTran.Rollback();
                            }
                            catch (Exception) { }
                        }

                        ErrorHandler.Error("Exception while writing to database", ex);
                    }
                }
                else
                {
                    SendBuffer(null, events);
                }
            }
        }

        virtual protected void SendBuffer(IDbTransaction dbTran, LoggingEvent[] events)
        {
            if (_UsePreparedCommand)
            {
                if (_DbCommand != null)
                {
                    if (dbTran != null)
                    {
                        _DbCommand.Transaction = dbTran;
                    }

                    foreach (LoggingEvent e in events)
                    {
                        foreach (AdoNetAppenderParameter param in _Parameters)
                        {
                            param.FormatValue(_DbCommand, e);
                        }

                        _DbCommand.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                using (IDbCommand dbCmd = Connection.CreateCommand())
                {
                    if (dbTran != null)
                    {
                        dbCmd.Transaction = dbTran;
                    }

                    foreach (LoggingEvent e in events)
                    {
                        string logStatement = GetLogStatement(e);
                        LogLog.Debug(declaringType, "LogStatement [" + logStatement + "]");
                        dbCmd.CommandText = logStatement;
                        dbCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        virtual protected string GetLogStatement(LoggingEvent logEvent)
        {
            if (Layout == null)
            {
                ErrorHandler.Error("AdoNetAppender: No Layout specified.");
                return String.Empty;
            }
            else
            {
                StringWriter writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
                Layout.Format(writer, logEvent);
                return writer.ToString();
            }
        }

        virtual protected IDbConnection CreateConnection(Type connectionType, string connectionString)
        {
            IDbConnection connection = (IDbConnection)Activator.CreateInstance(connectionType);
            connection.ConnectionString = connectionString;
            return connection;
        }

        virtual protected string ResolveConnectionString(out string connectionStringContext)
        {
            if (ConnectionString != null && ConnectionString.Length > 0)
            {
                connectionStringContext = "ConnectionString";
                return ConnectionString;
            }

            if (AppSettingsKey != null && AppSettingsKey.Length > 0)
            {
                connectionStringContext = "AppSettingsKey";
                string appSettingsConnectionString = SystemInfo.GetAppSetting(AppSettingsKey);
                if (appSettingsConnectionString == null || appSettingsConnectionString.Length == 0)
                {
                    throw new LogException("Unable to find [" + AppSettingsKey + "] AppSettings key.");
                }
                return appSettingsConnectionString;
            }

            connectionStringContext = "Unable to resolve connection string from ConnectionString, ConnectionStrings, or AppSettings.";
            return string.Empty;
        }

        virtual protected Type ResolveConnectionType()
        {
            try
            {
                return SystemInfo.GetTypeFromString(Assembly.GetEntryAssembly(), ConnectionType, true, false);
            }
            catch (Exception ex)
            {
                ErrorHandler.Error("Failed to load connection type [" + ConnectionType + "]", ex);
                throw;
            }
        }
        #endregion

        #region Helper Methods
        private void InitializeDatabaseCommand()
        {
            if (Connection != null && _UsePreparedCommand)
            {
                try
                {
                    DisposeCommand(false);
                    _DbCommand = Connection.CreateCommand();
                    _DbCommand.CommandText = CommandText;
                    _DbCommand.CommandType = CommandType;
                }
                catch (Exception e)
                {
                    ErrorHandler.Error("Could not create database command [" + CommandText + "]", e);
                    DisposeCommand(true);
                }

                if (_DbCommand != null)
                {
                    try
                    {
                        foreach (AdoNetAppenderParameter param in _Parameters)
                        {
                            try
                            {
                                param.Prepare(_DbCommand);
                            }
                            catch (Exception e)
                            {
                                ErrorHandler.Error("Could not add database command parameter [" + param.ParameterName + "]", e);
                                throw;
                            }
                        }
                    }
                    catch
                    {
                        DisposeCommand(true);
                    }
                }

                if (_DbCommand != null)
                {
                    try
                    {
                        _DbCommand.Prepare();
                    }
                    catch (Exception e)
                    {
                        ErrorHandler.Error("Could not prepare database command [" + CommandText + "]", e);

                        DisposeCommand(true);
                    }
                }
            }
        }

        private void InitializeDatabaseConnection()
        {
            string connectionStringContext = "Unable to determine connection string context.";
            string resolvedConnectionString = string.Empty;

            try
            {
                DisposeCommand(true);
                DiposeConnection();

                resolvedConnectionString = ResolveConnectionString(out connectionStringContext);
                Connection = CreateConnection(ResolveConnectionType(), resolvedConnectionString);

                using (SecurityContext.Impersonate(this))
                {
                    Connection.Open();
                }
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Could not open database connection [" + resolvedConnectionString + "]. Connection string context [" + connectionStringContext + "].", e);
                Connection = null;
            }
        }

        private void DisposeCommand(bool ignoreException)
        {
            if (_DbCommand != null)
            {
                try
                {
                    _DbCommand.Dispose();
                }
                catch (Exception ex)
                {
                    if (!ignoreException)
                    {
                        LogLog.Warn(declaringType, "Exception while disposing cached command object", ex);
                    }
                }
                finally
                {
                    _DbCommand = null;
                }
            }
        }

        private void DiposeConnection()
        {
            if (Connection != null)
            {
                try
                {
                    Connection.Close();
                }
                catch (Exception ex)
                {
                    LogLog.Warn(declaringType, "Exception while disposing cached connection object", ex);
                }
                finally
                {
                    Connection = null;
                }
            }
        }
        #endregion
    }
}
