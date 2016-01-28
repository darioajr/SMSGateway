using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using GsmComm.GsmCommunication;
using GsmComm.PduConverter;
using System.Timers;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Transactions;
using System.ServiceModel;


namespace SMSGateway
{
    public partial class Service1 : ServiceBase
    {
        //componente de comunicação com modem GSM
        private GsmCommMain comm;
        
        //variávels de configuração
        private string _SQLConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;
        private string _SQLQuery = System.Configuration.ConfigurationManager.AppSettings["SQLQuery"].ToString();
        private string _ModemPort = System.Configuration.ConfigurationManager.AppSettings["ModemPort"].ToString();
        private int _Timeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Timeout"].ToString());
        private int _TimeSync = int.Parse(System.Configuration.ConfigurationManager.AppSettings["TimeSync"].ToString());
        private string _SQLUpdate = System.Configuration.ConfigurationManager.AppSettings["SQLUpdate"].ToString();

        //evento de execução em lote
        public System.Timers.Timer _aTimer;

        //hopedagem de API Webservice WCF (usado para incluir registros no banco para envio de sms)
        ServiceHost sHost;

        public Service1()
        {
            InitializeComponent();

            //registra no eventlog
            if (!System.Diagnostics.EventLog.SourceExists("SMSGateway"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "SMSGateway", "SMSGatewayLog");
            }

            string log = System.Diagnostics.EventLog.LogNameFromSourceName("SMSGateway", ".");
            
            eventLog1.Source = "SMSGateway";
            eventLog1.Log = log;
        }

        protected override void OnStart(string[] args)
        {
            //inicializa componente WCF
            sHost = new ServiceHost(typeof(SMSGatewayWCFLibrary.Messages));
            sHost.Open();

            //inicializa o sistema de processamento
            Iniciar();
        }

        public void Iniciar()
        {
            try
            {
                //le configurações
                _SQLConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SQLConnectionString"].ConnectionString;
                _SQLQuery = System.Configuration.ConfigurationManager.AppSettings["SQLQuery"].ToString();
                _ModemPort = System.Configuration.ConfigurationManager.AppSettings["ModemPort"].ToString();
                _Timeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Timeout"].ToString());
                _TimeSync = int.Parse(System.Configuration.ConfigurationManager.AppSettings["TimeSync"].ToString());
                _SQLUpdate = System.Configuration.ConfigurationManager.AppSettings["SQLUpdate"].ToString();

                if (String.IsNullOrEmpty(_ModemPort))
                    return;

                //grava log
                eventLog1.WriteEntry("In OnStart");

                //abre comunicação com o modem gsm
                comm = new GsmCommMain(_ModemPort, 9600, 150);
                comm.Open();

                //grava log
                eventLog1.WriteEntry("Opened GsmCommMain");

                //inicia evento de processamento das mensagens
                _aTimer = new System.Timers.Timer();
                _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                _aTimer.Interval = _TimeSync;
                _aTimer.Enabled = true;
            }
            catch (Exception e)
            {
                eventLog1.WriteEntry("OnStart Exception = "+ e.Message, EventLogEntryType.Error);
                throw new System.Exception(e.Message);
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //por segurança, para o evento, pois este processo pode demorar,
            //e não podemos correr o risco de executar em paralelo
            _aTimer.Stop();
            
            try
            {
                //verifica se a comunicação com o modem gsm esta ativa, 
                //caso contrário reconecta (pode ter ocorrido perda de sinal)
                if (!comm.IsOpen())
                    comm.Open();

                //abre conexão com o banco de dados
                using (SqlConnection connection = new SqlConnection(this._SQLConnectionString))
                {
                    connection.Open();

                    //cria lista de registro que vão ser atualizados como processados
                    List<int> updateList = new List<int>(); 

                    //executa comando sql para retornar a lista de sms a enviar
                    using (SqlCommand sqlCommand = new SqlCommand(this._SQLQuery, connection))
                    {
                        //processa cada registro
                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {                                       
                            while (sqlDataReader.Read())
                            {
                                SmsSubmitPdu pdu;
                                byte dcs = (byte)DataCodingScheme.GeneralCoding.Alpha7BitDefault;
                                pdu = new SmsSubmitPdu(Util.Translate(sqlDataReader["message"].ToString()), sqlDataReader["phone"].ToString(), dcs);
                                comm.SendMessage(pdu);

                                //recupera id do registro
                                int id = int.Parse(sqlDataReader["id"].ToString());
                                
                                //insere registro para atualização de processado
                                updateList.Add(id);
                            }
                        }
                    }

                    //atualiza todos os registros marcados como processados
                    for (int i = 0; i < updateList.Count; i++)
                    {
                        UpdateReference(updateList[i]);       
                    }

                    //reinicia o processamento de próximos registros
                    _aTimer.Start();
                }
            }
            catch (Exception ex)
            {
                //grava informações no log de eventos do windows
                eventLog1.WriteEntry("OnTimedEvent Exception = " + ex.Message, EventLogEntryType.Error);

                //reinicia o processamento de próximos registros
                _aTimer.Start();
            }
        }

        private bool UpdateReference(int id)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection connection = new SqlConnection(this._SQLConnectionString))
                    {
                        connection.Open();

                        string update = string.Format(_SQLUpdate, id);
                        using (SqlCommand sqlCommand = new SqlCommand(update, connection))
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                    }

                    scope.Complete();

                    return true;
                }
            }
            catch (Exception e)
            {
                eventLog1.WriteEntry("UpdateReference Exception = " + e.Message, EventLogEntryType.Error);
                return false;
            }
        }

        public void Parar()
        {
            if (String.IsNullOrEmpty(_ModemPort))
                return;

            _aTimer.Stop();
            _aTimer.Dispose();

            eventLog1.WriteEntry("In OnStop");
            if (comm.IsOpen())
            {
                comm.Close();
                eventLog1.WriteEntry("Closed GsmCommMain");
            }
        }

        protected override void OnStop()
        {
            //para componente WCF
            sHost.Close();

            //finaliza o processamento
            Parar();
        }
    }
}