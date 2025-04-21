using System;
using System.Configuration;
using System.ServiceProcess;
using log4net;
using ReadsFilesTransform;


namespace ReadsFilesTransformSrvc
{
    public partial class ReadsFilesTransformSrvc: ServiceBase
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));
        private ReadsFileProcessor _readsFileProcessor;

        private const string LOG_FILE_LINE = "\n-------------------------------------------------------------------------";
        private const string LOG_FILE_STOP_SRVC = "\n--------------S T O P P I N G   S E R V I C E ---------------------------";
        private const string LOG_FILE_START_SRVC = "\n--------------S T A R T I N G   S E R V I C E ---------------------------";

        /// <summary>
        /// C'Tor -  Initializes a new instance of the <see cref="ReadsFilesTransformSrvc"/> class.
        /// </summary>
        public ReadsFilesTransformSrvc()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Called when service [start].
        /// </summary>
        protected override void OnStart(string[] args)
        {
            _logger.Info(LOG_FILE_LINE+ LOG_FILE_START_SRVC+ LOG_FILE_LINE);
            try
            {
                _readsFileProcessor = new ReadsFileProcessor(_logger);
                _readsFileProcessor.ReadsFileWatcher();
            }
            catch (Exception ex)
            {
                _logger.Fatal("Service encountered a fatal error.", ex);
                _logger.Info("Service shutting down...");
                this.OnStop();
            }
        }

        /// <summary>
        /// Called when [stop].
        /// </summary>
        protected override void OnStop()
        {
            _logger.Info(LOG_FILE_LINE + LOG_FILE_STOP_SRVC + LOG_FILE_LINE);
        }
    }
}
