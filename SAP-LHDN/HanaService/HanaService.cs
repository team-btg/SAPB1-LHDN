using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class HanaService
{
    HanaConnection oConn;
    private readonly string ConnStr;

    // The DI container will provide the connection string here.
    public HanaService(string _ConnStr)
    {
        // This is where the connection string is stored upon initialization
        this.ConnStr = _ConnStr;
    }
    /// <summary>
    /// Execute SELECT statement that returns DataTable
    /// </summary>
    /// <param name="oQuery"></param>
    /// <returns></returns>
    public DataTable HanaSelectQuery(string oQuery)
    {
        DataTable oDtReport = new DataTable();
        using (oConn = new HanaConnection(ConnStr))
        {
            HanaDataAdapter oDa = new HanaDataAdapter(
            oQuery, oConn);

            oDa.Fill(oDtReport);
        }
        return oDtReport;
    }

    /// <summary>
    /// Execute NonQuery (INSERT, UPDATE, DELETE)
    /// </summary>
    /// <param name="oQuery"></param>
    /// <returns></returns>
    public async Task<int> HanaExecuteNonQuery(string oQuery)
    {
        using (oConn = new HanaConnection(ConnStr))
        {
            oConn.Open();

            HanaCommand cmd = new HanaCommand(oQuery, oConn);
            return await cmd.ExecuteNonQueryAsync();
        }
    }

}
