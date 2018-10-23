using System;
using System.Data;
using Npgsql;

namespace BendroCorpBackgroundAgent
{
    public class PgUnicorn
    {

        string connString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        public DataTable DataTableOfSql(string sql)
        {
            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                conn.Open();

                // Retrieve all rows
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    return dataTable;
                }
            }
        }

        public object ScalerOfSql(string sql)
        {

            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        public int ExecuteNonQueryOfSql(string sql)
        {
            using (var conn = new Npgsql.NpgsqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
