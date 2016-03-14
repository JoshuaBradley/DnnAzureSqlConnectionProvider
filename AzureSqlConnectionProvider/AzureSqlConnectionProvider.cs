
namespace Dnn.AzureSqlConnectionProvider
{
    using System.Data;
    using System.Text.RegularExpressions;

    using DotNetNuke.Data;
    using DotNetNuke.Data.PetaPoco;
    using DotNetNuke.Entities.Portals;
    using DotNetNuke.Entities.Users;
    using DotNetNuke.Services.Log.EventLog;

    using Microsoft.ApplicationBlocks.Data;
    using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

    public class AzureSqlConnectionProvider : DatabaseConnectionProvider
    {
        private static readonly Regex ScriptWithRegex = new Regex("WITH\\s*\\([\\s\\S]*?((PAD_INDEX|ALLOW_ROW_LOCKS|ALLOW_PAGE_LOCKS)\\s*=\\s*(ON|OFF))+[\\s\\S]*?\\)",
                                                            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ScriptOnPrimaryRegex = new Regex("(TEXTIMAGE_)*ON\\s*\\[\\s*PRIMARY\\s*\\]", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public override void ExecuteCommand()
        {
            var retry = RetryHelper.GetRetryPolicy();

            var query = GetAzureCompactScript(Query);

            if (query != Query)
            {
                var props = new LogProperties { new LogDetailInfo("SQL Script Modified", query) };

                EventLogController.Instance.AddLog(props,
                            PortalController.Instance.GetCurrentPortalSettings(),
                            UserController.Instance.GetCurrentUserInfo().UserID,
                            EventLogController.EventLogType.HOST_ALERT.ToString(),
                            true);
            }

            using (var reliableConnection = new ReliableSqlConnection(ConnectionString, retry, retry))
            {
                var command = reliableConnection.CreateCommand();
                command.CommandText = query;
                command.CommandTimeout = 0;

                reliableConnection.Open();
                command.ExecuteNonQuery();
                reliableConnection.Close();
            }
        }

        public override T ExecuteScalar<T>(string connectionString, CommandType commandType, string procedureName, params object[] commandParameters)
        {
            var retry = RetryHelper.GetRetryPolicy();
            return retry.ExecuteAction(() => PetaPocoHelper.ExecuteScalar<T>(connectionString, commandType, procedureName, commandParameters));
        }

        public override IDataReader ExecuteReader(string connectionString, CommandType commandType, string procedureName, params object[] commandParameters)
        {
            var retry = RetryHelper.GetRetryPolicy();
            return retry.ExecuteAction(() => PetaPocoHelper.ExecuteReader(connectionString, commandType, procedureName, commandParameters));
        }

        public override void ExecuteNonQuery(string connectionString, CommandType commandType, string procedure, object[] commandParameters)
        {
            var retry = RetryHelper.GetRetryPolicy();
            retry.ExecuteAction(() => PetaPocoHelper.ExecuteNonQuery(connectionString, commandType, procedure, commandParameters));
        }

        public override int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText)
        {
            var retry = RetryHelper.GetRetryPolicy();
            return retry.ExecuteAction(() => SqlHelper.ExecuteNonQuery(connectionString, commandType, commandText));
        }

        private string GetAzureCompactScript(string script)
        {
            if (ScriptWithRegex.IsMatch(script))
            {
                script = ScriptWithRegex.Replace(script, string.Empty);
            }

            if (ScriptOnPrimaryRegex.IsMatch(script))
            {
                script = ScriptOnPrimaryRegex.Replace(script, string.Empty);
            }

            return script;
        }
    }
}
