using System.Reflection;
using System.ServiceProcess;
using log4net;
using log4net.Config;

namespace ReadsFilesTransformSrvc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        ///
        static void Main()
        {
            // Configure log4net 
            XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetCallingAssembly()));

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ReadsFilesTransformSrvc()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
