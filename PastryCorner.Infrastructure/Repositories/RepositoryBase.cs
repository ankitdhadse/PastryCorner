

namespace PastryCorner.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using PastryCorner.Domain.Interfaces;
    using Dapper;

    public abstract class RepositoryBase
    {
        private readonly IDbConnectionFactory _factory;

        protected RepositoryBase(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<T> WithConnectionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> query)
        {
            using (var connection = _factory.CreateConnection())
            {
                if (connection.State != ConnectionState.Open) connection.Open();

                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    var result = await query(connection, transaction).ConfigureAwait(false);
                    transaction.Commit();
                    return result;
                }
            }
        }

        public async Task<Dictionary<string, IEnumerable<object>>> WithConnectionQueryMultipleAsync(
            IEnumerable<Type> responseTypes, Func<IDbConnection, IDbTransaction, Task<SqlMapper.GridReader>> query)
        {
            var resultSet = new Dictionary<string, IEnumerable<object>>();

            using (var connection = _factory.CreateConnection())
            {
                if (connection.State != ConnectionState.Open) connection.Open();

                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    using (var result = await query(connection, transaction).ConfigureAwait(false))
                    {
                        foreach (var objectType in responseTypes)
                            resultSet.Add(objectType.Name, await result.ReadAsync(objectType).ConfigureAwait(false));

                        transaction.Commit();
                    }
                }
            }

            return resultSet;
        }

        public async Task<int> WithConnectionExecuteAsync(string insertQuery, object parameters = null, int? commandTimeOut = null, CommandType? commandType = null)
        {
            using (var connection = _factory.CreateConnection())
            {
                if (connection.State != ConnectionState.Open) connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var rowsAffected = await connection.ExecuteAsync(insertQuery, parameters, transaction, commandTimeOut, commandType).ConfigureAwait(false);
                    transaction.Commit();
                    return rowsAffected;
                }
            }
        }
    }
}
