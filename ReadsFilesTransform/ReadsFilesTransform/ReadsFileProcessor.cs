using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text;
using log4net;
using System.Linq;
using ReadsFilesTransform.Models;
using System.Threading;
using Microsoft.EntityFrameworkCore;

namespace ReadsFilesTransform
{
    public class ReadsFileProcessor
    {
        /// <summary>
        /// Constants 
        /// </summary>
        private const string INPUT_FILES_PATH = "MekorotInputPath";
        private const string OUTPUT_FILES_PATH = "MekorotOutputPath";
        private const string ARCHIVE_FILES_PATH = "MekorotArchivePath";
        private const string ERROR_FILES_PATH = "MekorotErrPath";
        private const string DEFAULT_ARCHIVE_FILES_PATH = @"%ProgramData%\AradTechnologies\ReadsFilesTransformService\Archive_Files";
        private const string DEFAULT_ERROR_FILES_PATH = @"%ProgramData%\AradTechnologies\ReadsFilesTransformService\ERR_Files";
        private const int METER_ID_CSV_POSITION = 2;
        private const string LOG_FILE_LINE = "-------------------------------------------------------------------------";
        private const string LOG_FILE_STOP_SRVC = "\n--------------S T O P P I N G   S E R V I C E ->  E R R O R -------------\n";

        private FileSystemWatcher _fileWatcher;
        private readonly ILog _logger;
        private Dictionary<string, string> _mappingTable;
        private ReadsFileTransformDbContext _context;
        private string _mekorotInputPath;
        private string _mekorotArchivePath;
        private string _mekorotOutputPath;
        private string _mekorotErrPath;
        private bool _isMultiFilesMode = false;

        private int _rowCounter;
        private int _successCounter;
        private int _errCounter;

        private string _senderName;
        private string _fullArchivePath;
        private string _fullOutputPath;
        private string _fullErrortPath;

        /// <summary>
        /// C'Tor - ReadsFileProcessor
        /// Initializes a new instance of the <see cref="ReadsFileProcessor" /> class.
        /// </summary>
        public ReadsFileProcessor(ILog logger)
        {
            _logger = logger;
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"]?.ConnectionString;
                var options = new DbContextOptionsBuilder<ReadsFileTransformDbContext>().UseSqlServer(connectionString).Options;
                _context = new ReadsFileTransformDbContext(options);
                InitializeFilesPath();

            }
            catch (Exception ex)
            {
                _logger.Error("Critical error in ReadsFileProcessor.", ex);
                throw;  
            }
        }

        /// <summary>
        ///  Start file watcher on the Input path 
        /// </summary>
        public void ReadsFileWatcher()
        {
            try
            {
                _mappingTable = new Dictionary<string, string>();
                _fileWatcher = new FileSystemWatcher(_mekorotInputPath);
                _fileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                _fileWatcher.Created += OnFileCreated;
                _fileWatcher.EnableRaisingEvents = true; // Start listening for file changes
                _logger.Info($"Watching for files in directory: {_mekorotInputPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Critical error in ReadsFileWatcher() occurred: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Called on event of file created in the Input Directory.
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.Info(LOG_FILE_LINE);
            _logger.Info($"New file detected: {e.Name}");
            try
            {
                // Ensure input file exists, accessible, Not empty ,not duplicate (processed in past) and process it
                if (File.Exists(e.FullPath) && !IsEmptyInputFile(e) && !IsDuplicateInputFile(e))
                {
                    //in case of single file - open DB connection and read mapping table.
                    if (!_isMultiFilesMode) GetMappingFromDb();

                    var reader = new StreamReader(e.FullPath, Encoding.UTF8);

                    //Handle Header - extract senderName 
                    var headerLine = reader.ReadLine();
                    _senderName = GetSenderName(headerLine);

                    //Handle rowData and counters - Process each line
                    _rowCounter = 0;
                    _errCounter = 0;
                    _successCounter = 0;

                    //get target files names 
                    _fullOutputPath = GetTargetFileName(e.Name, _mekorotOutputPath);
                    _fullErrortPath = GetTargetFileName(e.Name, _mekorotErrPath);
                    
                    //process row Data 
                    using (var writerSuccess = new StreamWriter(_fullOutputPath, append: false)) // Overwrites the target 
                    using (var writerError = new StreamWriter(_fullErrortPath, append: false))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            _rowCounter++;
                            var result = ReplaceMeterId(line);
                            if(result.Item1)
                            {
                                writerSuccess.WriteLine(result.Item2);
                                _successCounter++;
                            }
                            else
                            {
                                writerError.WriteLine(result.Item2);
                                _errCounter++;
                            }
                        }   
                        writerSuccess.Close();
                        writerError.Close();
                        DeleteEmptyOutputFiles();
                    }
                    reader.Close();

                    // Move file to Archive 
                    _fullArchivePath = Path.Combine(_mekorotArchivePath, e.Name);
                    if (!File.Exists(_fullArchivePath))
                    {
                        File.Move(e.FullPath, _fullArchivePath);  
                    }

                    WriteResultsToLog(e.Name);
                    WriteResultsToDB(e.Name);
                    SetMultiFilesFlag();
                }
            }
            catch (Exception ex)
            {
                //error open connection 
                _logger.Error($"Critical error in ReadsFileProcessor:OnFileCreated: {ex.Message}");
                _logger.Info(LOG_FILE_LINE + LOG_FILE_STOP_SRVC + LOG_FILE_LINE);

                _context.Database.GetDbConnection().Close(); 
                throw;  
            }
        }
        /// <summary>
        /// Sets the multi files flag.
        /// if there are pending files in the input directory then set the flag to true
        /// </summary>
        private void SetMultiFilesFlag()
        {
            if (Directory.Exists(_mekorotInputPath))
            {
                // Get all files in the folder
                string[] files = Directory.GetFiles(_mekorotInputPath);

                if (files.Length > 0) 
                {
                    _logger.Info($"The Input folder contains {files.Length} files. DB connection will stay open");
                    _logger.Info($"Database connection State is: {_context.Database.GetDbConnection().State}");
                    _isMultiFilesMode = true;
                }
                else
                {
                    _isMultiFilesMode = false;
                    _logger.Info("The folder is empty.");
                    _context.Database.GetDbConnection().Close(); // close connection
                    _logger.Info($"Database connection State is: {_context.Database.GetDbConnection().State}");
                }
            }
        }
        /// <summary>
        /// Gets properties table data from DB into hash table.
        /// </summary>
        private void GetHydrologicNoMapping()
        {
            try
            {
                _mappingTable = _context.Properties
                                        .Select(p => new { p.HydrologicNo, p.PropertyID })
                                        .Where(p => p.HydrologicNo.HasValue && p.HydrologicNo != 0) 
                                        .ToDictionary(p => p.HydrologicNo.ToString(), p => p.PropertyID);
            }
            catch (Exception ex)
            {
                _logger.Error($"GetHydrologicNoMapping: {ex.Message}");
                _logger.Error($"GetHydrologicNoMapping: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Gets the name of the target file  
        /// </summary>
        private string GetTargetFileName(string fileName,string targetFilePath)
        {
            // Handle output files names
            string formattedDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            FileInfo fileInfo = new FileInfo(fileName);
            string newFileName = $"{fileInfo.Name.Replace(fileInfo.Extension, "")}_{formattedDateTime}{fileInfo.Extension}";
            string fullPath = Path.Combine(targetFilePath, newFileName);

            return fullPath;
        }

        /// <summary>
        /// Gets the sender
        /// source Company from the file header 
        /// </summary>
        private string GetSenderName(string headerLine)
        {
            string senderName = string.Empty;
            string[] row = headerLine.Split(':');
            if (row.Length > 1)
            {
                senderName = row[1].Split(',')[0];
            }
            return senderName;
        }

        /// <summary>
        /// Replaces the value of the meterId with the value of PropertyID mapped in the database 
        /// </summary>
        private (bool, string) ReplaceMeterId(string line)
        {
            //Get the MeterID  value
            string[] currRow = line.Split(',');
            string currMeterId = currRow[METER_ID_CSV_POSITION]; 
             
            //split MeterID value to FaciliyNumber and MeterNumber 
            string[] currMeterIdSpliter = currMeterId.Split('-');
            string currFaciliyNumber = currMeterIdSpliter[0];

            //Replace currFaciliyNumber(HydrologicNo) with currentPropertyID  
            //if not found - write it to error file.
            string currentPropertyID = string.Empty;
            string newLine = line;

            if (_mappingTable.TryGetValue(currFaciliyNumber, out string value))
            {
                currentPropertyID = value;
                newLine = line.Replace(currFaciliyNumber, currentPropertyID);
                return (true, newLine);
            }
            else
            {
                return (false, line);
            }
        }

        /// <summary>
        /// Gets the mapping from database.
        /// get {PropertyID, HydrologicNo} into hash table. 
        /// </summary>
        private void GetMappingFromDb()
        {
            try
            {
                _context.Database.GetDbConnection().Open();
                _logger.Info($"Database connection State is: {_context.Database.GetDbConnection().State}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Critical error in ReadsFileProcessor occurred: {ex.Message}");
                throw;
            }
            GetHydrologicNoMapping();
        }
        /// <summary>
        /// Writes the file processing results to log file
        /// </summary>
        /// <param name="fileName"> Input file Name.</param>
        private void WriteResultsToLog(string fileName)
        {
            _logger.Info(LOG_FILE_LINE);
            _logger.Info($"file: {fileName} processing finished");
            _logger.Info($"# of Total Processed Lines: {_rowCounter}");
            _logger.Info($"# of Error Lines: {_errCounter}");
            _logger.Info($"# of Success Lines: {_successCounter}");
            _logger.Info($"File moved to: {_fullArchivePath}");
            _logger.Info(LOG_FILE_LINE);
        }
        /// <summary>
        /// Writes the file processing results to database.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void WriteResultsToDB(string fileName)
        {
            using (var transaction = _context.Database.BeginTransaction()) 
            {
                try
                {
                    var auditEntry = new ReadsFileTransformAudit
                    {
                        SourceCompany = _senderName,
                        ProcessingTime = DateTime.Now,
                        InputFileName = fileName,
                        OutputFileName = _fullOutputPath,
                        ErrortFileName = _fullErrortPath,
                        SuccessRecordsCount = _successCounter,
                        ErrRecordsCount = _errCounter
                    };

                    _context.ReadsFileTransformAudit.Add(auditEntry);  
                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Critical error - insert into ReadsFileTransformAudit table .{ex.Message}");
                    transaction.Rollback();
                    throw;  
                }
            }
        }

        /// <summary>Initializes the files path.</summary>
        private void InitializeFilesPath()
        {
            _mekorotInputPath = GetFilePath(INPUT_FILES_PATH, string.Empty);
            _mekorotOutputPath = GetFilePath(OUTPUT_FILES_PATH, string.Empty);
            _mekorotArchivePath = GetFilePath(ARCHIVE_FILES_PATH, DEFAULT_ARCHIVE_FILES_PATH);
            _mekorotErrPath = GetFilePath(ERROR_FILES_PATH, DEFAULT_ERROR_FILES_PATH);
        }

        /// <summary>
        /// GetFilePath 
        /// Gets the file paths from the app.config 
        ///  and validate the path exists , if not then use the default paths from the input 
        ///  if defaut path not exists then create it . 
        /// </summary>
        private string GetFilePath(string inPath, string defaultPath)
        {
            string rawPath = ConfigurationManager.AppSettings[inPath];
            string newPath = Environment.ExpandEnvironmentVariables(rawPath);

            if (!Directory.Exists(newPath) && !string.IsNullOrEmpty(defaultPath))
            {
                newPath = Environment.ExpandEnvironmentVariables(defaultPath);
                _logger.Info($"path is set to DEFAULT_PATH: {newPath}");
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                    _logger.Info($"craeting default directoy: {newPath}");
                }
            }
            return newPath;
        }
        /// <summary>
        /// Deletes the empty output files.
        /// </summary>
        private void DeleteEmptyOutputFiles()
        {
            DeleteEmptyOutputFile(_fullErrortPath);
            DeleteEmptyOutputFile(_fullOutputPath);
        }

        /// <summary>
        /// Deletes the empty output file.
        /// </summary>
        private void DeleteEmptyOutputFile(string fileFullPath)
        {
            if (File.Exists(fileFullPath) && new FileInfo(fileFullPath).Length == 0)
            {
                File.Delete(fileFullPath);
                Console.WriteLine("File was empty and has been deleted.");
            }
        }
        /// <summary>
        /// Determines whether [is empty input file]
        /// </summary>
        private bool IsEmptyInputFile(FileSystemEventArgs e)
        {
            if (new FileInfo(e.FullPath).Length == 0)
            {
                _logger.Info($"File is Empty, Skipping processing.");
                // Move file to Archive or to error if duplicate
                _fullArchivePath = Path.Combine(_mekorotArchivePath, e.Name);
                if (!File.Exists(_fullArchivePath))
                {
                    _logger.Info($"File moved to: {_fullArchivePath}");
                    File.Move(e.FullPath, _fullArchivePath);
                }
                else
                {
                    _fullErrortPath = GetTargetFileName(e.Name, _mekorotErrPath);
                    _logger.Info($"File moved to: {_fullErrortPath}");
                    Thread.Sleep(500);
                    File.Move(e.FullPath, _fullErrortPath);
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Determines whether [is duplicate input file] .
        /// </summary>
        private bool IsDuplicateInputFile(FileSystemEventArgs e)
        {
            _fullArchivePath = Path.Combine(_mekorotArchivePath, e.Name);
            if (File.Exists(_fullArchivePath))
            {
                _logger.Info($"File is duplicate, moved error files and Skip processing.");
                _fullErrortPath = GetTargetFileName(e.Name, _mekorotErrPath);

                if (!File.Exists(_fullErrortPath))
                {
                    _logger.Info($"File moved to: {_fullErrortPath}");
                    Thread.Sleep(500);
                    File.Move(e.FullPath, _fullErrortPath);
                }
                return true;
            }
            return false;
        }
    }
}
