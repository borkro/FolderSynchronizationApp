using System.IO;
using System.Diagnostics;
using Serilog;

namespace Test_Task
{
	class Program {
		
		private void Run(string sourceFolderPath, string replicaFolderPath) {
			Log.Information($"Source Folder: {sourceFolderPath}");
			Log.Information($"Replica Folder: {replicaFolderPath}");
			// start with a simple file copy
			try
			{
				File.Copy(sourceFolderPath, replicaFolderPath, true);
				Log.Information($"File {sourceFolderPath} copied successfully to {replicaFolderPath}.");
			}
			catch (IOException ex)
			{
				Log.Error(ex, $"Error copying file {sourceFolderPath} to {replicaFolderPath}.");
			}
			catch (UnauthorizedAccessException ex)
			{
				Log.Error(ex, "Permission denied.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Unexpected error.");
			}
		}
		
		static void Main(string[] args) {
			try
			{
				if (args.Length < 2)
				{
					Console.WriteLine("Usage: program.exe <logFilePath> <sourceFolderPath> <replicaFolderPath>");
					return;
				}

				Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.WriteTo.File(args[0], rollingInterval: RollingInterval.Day) // Logs to file
					.CreateLogger();

				Stopwatch sw = new Stopwatch();
				sw.Start();

				new Program().Run(args[1], args[2]);

				sw.Stop();
				TimeSpan ts = sw.Elapsed;

				string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
				Log.Information($"Runtime: {elapsedTime}.");
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Application error.");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}
	}
}