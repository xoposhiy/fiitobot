using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ydb.Sdk;
using Ydb.Sdk.Auth;
using Ydb.Sdk.Table;
using Ydb.Sdk.Value;
using Ydb.Sdk.Yc;

namespace tgnames
{
    public class NamesRepo
    {
        private readonly IYdbSettings settings;

        public NamesRepo(IYdbSettings settings)
        {
            this.settings = settings;
        }

        public async Task<UserEntry> Save(long tgId, string username)
        {
            using var tableClient = await CreateTableClient();
            var now = DateTime.Now;
            var result = await tableClient.SessionExec(async session =>
            {
                var res = await session.ExecuteDataQuery(@"
                    DECLARE $id AS Int64;
                    DECLARE $username AS Utf8;
                    DECLARE $lastUpdate AS Timestamp;

                    UPSERT INTO usernames (id, username, lastUpdate)
                    VALUES ($id, $username, $lastUpdate)",
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters:
                    new Dictionary<string, YdbValue>
                    {
                        { "$id", YdbValue.MakeInt64(tgId) },
                        { "$username", YdbValue.MakeUtf8(username) },
                        { "$lastUpdate", YdbValue.MakeTimestamp(now) }
                    });
                res.EnsureSuccess();
                return await session.ExecuteDataQuery(@"
                    DECLARE $id AS Int64;

                    SELECT id, username, lastUpdate,
                    FROM usernames
                    WHERE id = $id;",
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters:
                    new Dictionary<string, YdbValue>
                    {
                        { "$id", YdbValue.MakeInt64(tgId) }
                    });
            });
            result.Status.EnsureSuccess();
            var row = ((ExecuteDataQueryResponse)result).Result.ResultSets[0].Rows.FirstOrDefault();
            return row == null ? null : CreateUserEntry(row);

        }

        public async Task<UserEntry> SearchByTgId(long tgId)
        {
            using var tableClient = await CreateTableClient();
            var result = await tableClient.SessionExec(async session =>
                await session.ExecuteDataQuery(@"
                    DECLARE $id AS Int64;

                    SELECT id, username, lastUpdate,
                    FROM usernames
                    WHERE id = $id;",
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters:
                    new Dictionary<string, YdbValue>
                    {
                        { "$id", YdbValue.MakeInt64(tgId) }
                    }));
            result.Status.EnsureSuccess();
            var row = ((ExecuteDataQueryResponse)result).Result.ResultSets[0].Rows.FirstOrDefault();
            return row == null ? null : CreateUserEntry(row);
        }

        public async Task<UserEntry> SearchByUsername(string username)
        {
            using var tableClient = await CreateTableClient();
            var result = await tableClient.SessionExec(async session =>
                await session.ExecuteDataQuery(@"
                    DECLARE $username AS Utf8;

                    SELECT id, username, lastUpdate,
                    FROM usernames
                    VIEW idx_username AS i
                    WHERE i.username = $username;",
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters:
                    new Dictionary<string, YdbValue>
                    {
                        { "$username", YdbValue.MakeUtf8(username) }
                    }));
            result.Status.EnsureSuccess();
            var row = ((ExecuteDataQueryResponse)result).Result.ResultSets[0].Rows.FirstOrDefault();
            return row == null ? null : CreateUserEntry(row);
        }

        private UserEntry CreateUserEntry(ResultSet.Row row)
        {
            return new UserEntry(
                (long?)row[0] ?? throw new Exception(),
                (string)row[1] ?? throw new Exception(),
                (DateTime?)row[2] ?? throw new Exception()
                );
        }
        private async Task<TableClient> CreateTableClient()
        {
            var credProvider = settings.AccessToken != null
                ? new TokenProvider(settings.AccessToken)
                : (ICredentialsProvider)new ServiceAccountProvider(FileHelper.FindFilenameUpwards(settings.YandexCloudKeyFile));
            var config = new DriverConfig(
                endpoint: settings.YdbEndpoint,
                database: settings.YdbDatabase,
                credentials: credProvider,
                customServerCertificate: YcCerts.GetDefaultServerCertificate()
            );

            var driver = new Driver(
                config: config,
                loggerFactory: new NullLoggerFactory()
            );

            await driver.Initialize();

            return new TableClient(driver, new TableClientConfig());
        }
    }

    
}
