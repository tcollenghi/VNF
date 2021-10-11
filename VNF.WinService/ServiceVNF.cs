using MetsoFramework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using VNF.Business;


namespace VNF.WinService
{
    public partial class ServiceVNF : ServiceBase
    {
        private System.Timers.Timer timer = null;
        private volatile bool isTimerRunning;
        private volatile bool isStopping;
        const long defaultTimerInterval = 30000;
        modVerificar objVerificar = null;

        public ServiceVNF()
        {
            InitializeComponent();
        }
        
        protected override void OnStart(string[] args)
        {
            try
            {
                CultureInfo culture = new CultureInfo(ConfigurationManager.AppSettings["DefaultCulture"]);
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                isStopping = isTimerRunning = false;
                objVerificar = null;

                string timerIntervalString;
                long timerInterval = 0;

                timerIntervalString = Uteis.GetSettingsValue<string>("TimerInterval");
                if (!long.TryParse(timerIntervalString, out timerInterval))
                    timerInterval = defaultTimerInterval;

                timer = new System.Timers.Timer();
                timer.Interval = timerInterval;
                timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
        
        protected override void OnStop()
        {
            int maxCountDown = 60;
            int countDown = 0;

            isStopping = true;
            if (objVerificar != null) { 
                objVerificar.isStopping = true;
                objVerificar.RegistrarLog(VNF.Business.modVerificar.TipoProcessamento.Servico, null, "STOPPING SERVICE", "The service is stopping");
            }

            if (isTimerRunning)
            {
                countDown = 0;
                while (isTimerRunning && countDown < maxCountDown)
                {
                    System.Threading.Thread.Sleep(1000);
                    countDown++;
                }
            }

            timer.Stop();
        }

        internal void StartService()
        {
            string[] emptyStringArray = new string[0];
            this.OnStart(emptyStringArray);
        }

        internal void StopService()
        {
            this.OnStop();
        }

        protected void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isTimerRunning && !isStopping)
            {
                try 
                {
                    isTimerRunning = true;

                    CultureInfo culture = new CultureInfo(ConfigurationManager.AppSettings["DefaultCulture"]);
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    VariaveisGlobais.ptipoProcssamento = 1;
                    objVerificar = new modVerificar();
                    objVerificar.OnTimedEvent();
                }
                catch (Exception ex)
                {
                    objVerificar.RegistrarLog(modVerificar.TipoProcessamento.Servico, ex);
                }
                finally
                {
                    isTimerRunning = false;
                    objVerificar = null;
                }
            }
        }

    }
}
