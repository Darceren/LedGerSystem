using SqlSugar;

namespace LedGerSystem.Infrastructure;

public static class SqlSugarSetup
{
    public static IServiceCollection AddLedGerSqlSugar(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LedGerDB")
            ?? throw new InvalidOperationException("Connection string 'LedGerDB' is not configured.");

        services.AddScoped<ISqlSugarClient>(_ =>
        {
            var db = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                if (configuration.GetValue<bool>("Logging:LogSql"))
                {
                    Console.WriteLine(sql);
                }
            };

            return db;
        });

        return services;
    }
}
