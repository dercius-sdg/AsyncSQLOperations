using System.Data.SqlClient;

namespace AsyncReplicaOperations
{
    public static class SqlConnectionChecker
    {
        public static bool checkConnection(SqlConnection connection)
        {
            var ret = true;

            try
            {
                connection.Open();
                connection.Close();
            }
            catch
            {
                ret = false;
            }
            return ret;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static bool checkProcedure(SqlConnection connection,DirectionsEnum direction)
        {
            var ret = true;
            var procName = "";

            switch(direction)
            {
                case DirectionsEnum.Import:
                    {
                        OperationsAPI.IsValid("ImportReplicaProcedure");
                        procName = OperationsAPI.ImportReplicaProcedure;
                        break;
                    }
                case DirectionsEnum.Export:
                    {
                        OperationsAPI.IsValid("ExportReplicaProcedure");
                        procName = OperationsAPI.ExportReplicaProcedure;
                        break;
                    }
            }

            var command = new SqlCommand();
            command.Connection = connection;
            command.CommandText = string.Format("select COUNT(*) from [{1}].[sys].[procedures] where name ='{0}'", procName, connection.Database);

            try
            {
                command.Connection.Open();
                var countQuery = (int)command.ExecuteScalar();
                if(countQuery == 0)
                {
                    ret = false;
                }
            }
            catch
            {
                ret = false;
            }

            return ret;
        }
    }
}
