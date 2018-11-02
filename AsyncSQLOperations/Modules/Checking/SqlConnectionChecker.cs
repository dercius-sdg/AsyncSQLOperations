using System.Data.SqlClient;

namespace AsyncSQLOperations
{
    static class SqlConnectionChecker
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

        public static bool checkProcedure(SqlConnection connection,DirectionsEnum direction)
        {
            var ret = true;
            var procName = "";

            switch(direction)
            {
                case DirectionsEnum.Import:
                    {
                        procName = Properties.Settings.Default.ImportReplicaProc;
                        break;
                    }
                case DirectionsEnum.Export:
                    {
                        procName = Properties.Settings.Default.ExportReplicaProc;
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
