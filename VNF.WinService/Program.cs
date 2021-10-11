using MetsoFramework.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using VNF.Business;

namespace VNF.WinService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (!Environment.UserInteractive)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new ServiceVNF() 
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                Console.WriteLine("Iniciando serviço via console.");
                Console.WriteLine("Para parar o serviço aperte uma tecla.");

                ServiceVNF servico = new ServiceVNF();
                servico.StartService();

                Console.ReadKey();
                servico.StopService();

                Console.WriteLine("Serviço parado via console."); 
            }
        }
    }
}
