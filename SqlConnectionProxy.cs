using log4net;
using log4net.Core;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Data.SqlClient
{
    public sealed class SqlConnectionProxy : DbConnection, IDisposable
    {
        private SqlConnection mConnection;
        private ILog mLogger;
        private string mSourceFile = null;
        private int mLineNumber = 0;
        private Exception mException = null;

        public SqlConnectionProxy(string connectionString = null,
            [CallerFilePath] string sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
                mConnection = new SqlConnection(BuildConnectionString(connectionString));
            else
                mConnection = new SqlConnection();
            mLogger = LogManager.GetLogger("SqlConnectionProxy");
            mSourceFile = Path.GetFileName(sourceFilePath);
            mLineNumber = sourceLineNumber;
        }

        public static implicit operator SqlConnection(SqlConnectionProxy v)
        {
            if (v != null)
            {
                if (v.mConnection.State != ConnectionState.Open)
                    v.mConnection.Open();
                return v.mConnection;
            }
            return null;
        }

        public override string ConnectionString
        {
            get => mConnection.ConnectionString;
            set => mConnection.ConnectionString = BuildConnectionString(value);
        }

        private static string BuildConnectionString(string value)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(value);
            builder.Pooling = false;
            builder.MultipleActiveResultSets = true;
            builder.AsynchronousProcessing = true;
            builder.ApplicationName = "LeaseWeb";
            return builder.ConnectionString;
        }

        public override string Database => mConnection.Database;

        public override string DataSource => mConnection.DataSource;

        public override string ServerVersion => mConnection.ServerVersion;

        public override ConnectionState State => mConnection.State;

        public override void ChangeDatabase(string databaseName)
        {
            mConnection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            Log(new Level(60000, "SqlConnection.Close"));
            if (mConnection.State != ConnectionState.Closed)
                mConnection.Close();
        }

        public override void Open()
        {
            if (mConnection.State != ConnectionState.Open)
                mConnection.Open();
            Log(new Level(50000, "SqlConnection.Open"));
        }

        public new void Dispose()
        {
            Log(new Level(70000, "SqlConnection.Dispose"));
            mConnection.Dispose();
        }

        public new SqlTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.ReadCommitted) as SqlTransaction;
        }

        public new SqlTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return BeginTransaction(isolationLevel) as SqlTransaction;
        }

        public new SqlCommand CreateCommand()
        {
            return CreateCommand() as SqlCommand;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return mConnection.BeginTransaction(isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            return mConnection.CreateCommand();
        }

        private void Log(Level level)
        {
            mLogger.Logger.Log(typeof(SqlConnectionProxy), level, GetMessage(), mException);
        }

        private string GetMessage()
        {
            return $"<root><assembly>{GetAssemblyName()}</assembly><file>{mSourceFile}</file><line>{mLineNumber}</line></root>";
        }

        private string GetAssemblyName()
        {
            StackTrace st = new StackTrace(4, true);
            MethodBase m = st.GetFrame(0).GetMethod();
            return m.DeclaringType.Assembly.GetName().Name;
        }

        public static void ClearAllPools()
        {
            SqlConnection.ClearAllPools();
        }

        public static void ClearPool(SqlConnection connection)
        {
            SqlConnection.ClearPool(connection);
        }
    }
}