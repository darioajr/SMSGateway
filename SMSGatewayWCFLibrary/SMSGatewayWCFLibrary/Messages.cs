using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SMSGatewayWCFLibrary
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Messages" in both code and config file together.
    public class Messages : IMessages
    {
        private string _SQLConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;
        private string _SQLInsert = System.Configuration.ConfigurationManager.AppSettings["SQLInsert"].ToString();

        public bool SendSMS(string phone, string message)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(this._SQLConnectionString))
                {
                    connection.Open();

                    string command = string.Format(this._SQLInsert,phone, message);

                    //executa comando sql para inserir na lista de sms a enviar
                    using (SqlCommand sqlCommand = new SqlCommand(command, connection))
                    {
                        //insere registro
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch{}
                
            return false;
        }
    }
}
