using System.IO;
using Serilog;

class Program
{
	private static string source;
	private static string replica;
	private static Timer timer;
	private static int syncInterval;

	static void Main(string[] args)
	{
		if (args.Length < 3)
		{
			Console.WriteLine("Usage: FolderSynchronization.exe log_file_path source_folder_path replica_folder_path synchronization_interval_in_ms");
			return;
		}

		try
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File(args[0], rollingInterval: RollingInterval.Day)
				.CreateLogger();

			source = args[1];
			replica = args[2];
			syncInterval = int.Parse(args[3]);

			Log.Information($"Source Folder: {source}");
			Log.Information($"Replica Folder: {replica}");
			Log.Information($"Synchronization Interval: {syncInterval} ms.");

			timer = new Timer(Sync, null, 0, syncInterval);

			Console.WriteLine("Synchronization running... Press Enter to exit.");
			Console.ReadLine();
		}
		catch (FormatException)
		{
			Log.Error("Invalid synchronization interval. Should be a time interval in [ms].");
		}
		catch (DirectoryNotFoundException ex)
		{
			Log.Error(ex, "Directory not found.");
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Unexpected error.");
		}
		finally
		{
			timer.Dispose();
			Log.CloseAndFlush();
		}
	}

	private static void Sync(object state)
	{
		if (!Directory.Exists(source))
		{
			throw new DirectoryNotFoundException($"Source folder not found: {source}");
		}
		Log.Information($"Synchronizing...");
		SyncFolders(source, replica);
		Log.Information($"Synchronization complete!");
	}

	private static void SyncFiles(string sourceFolderPath, string replicaFolderPath)
	{

		foreach (string sourceFilePath in Directory.GetFiles(sourceFolderPath))
		{
			string fileName = Path.GetFileName(sourceFilePath);
			string replicaFilePath = Path.Combine(replicaFolderPath, fileName);
			try
			{
				if (!File.Exists(replicaFilePath) || File.GetLastWriteTime(sourceFilePath) > File.GetLastWriteTime(replicaFilePath))
				{
					Log.Information($"Copying file {fileName}...");
					File.Copy(sourceFilePath, replicaFilePath, true);
					Log.Information($"Copied file {fileName}!");
				}
				else
				{
					Log.Information($"File {fileName} unchanged.");
				}
			}
			catch (IOException ex)
			{
				Log.Error(ex, "Error copying files.");
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
	}

	private static void SyncFolders(string sourceFolderPath, string replicaFolderPath)
	{

		try
		{
			if (!Directory.Exists(replicaFolderPath))
			{
				Directory.CreateDirectory(replicaFolderPath);
			}
		}
		catch (IOException ex)
		{
			Log.Error(ex, "Error copying folder.");
		}
		catch (UnauthorizedAccessException ex)
		{
			Log.Error(ex, "Permission denied.");
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Unexpected error.");
		}
		SyncFiles(sourceFolderPath, replicaFolderPath);
		DeleteFiles(sourceFolderPath, replicaFolderPath);
		DeleteFolders(sourceFolderPath, replicaFolderPath);
		foreach (string sourceSubfolderPath in Directory.GetDirectories(sourceFolderPath))
		{
			string subfolderName = Path.GetFileName(sourceSubfolderPath);
			string replicaSubfolderPath = Path.Combine(replicaFolderPath, subfolderName);
			try
			{
				Log.Information($"Copying folder {subfolderName}...");
				SyncFolders(sourceSubfolderPath, replicaSubfolderPath);
				Log.Information($"Copied folder {subfolderName}!");
			}
			catch (IOException ex)
			{
				Log.Error(ex, "Error copying folder.");
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

	}

	private static void DeleteFiles(string sourceFolderPath, string replicaFolderPath)
	{

		foreach (string replicaFilePath in Directory.GetFiles(replicaFolderPath))
		{
			string fileName = Path.GetFileName(replicaFilePath);
			string sourceFilePath = Path.Combine(sourceFolderPath, fileName);
			if (!File.Exists(sourceFilePath))
			{
				try
				{
					Log.Information($"Deleting file {fileName}...");
					File.Delete(replicaFilePath);
					Log.Information($"Deleted file {fileName}!");
				}
				catch (IOException ex)
				{
					Log.Error(ex, "Error deleting file.");
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
		}

	}

	private static void DeleteFolders(string sourceFolderPath, string replicaFolderPath)
	{

		foreach (string replicaSubfolderPath in Directory.GetDirectories(replicaFolderPath))
		{
			string subfolderName = Path.GetFileName(replicaSubfolderPath);
			string sourceSubfolderPath = Path.Combine(sourceFolderPath, subfolderName);
			if (!Directory.Exists(sourceSubfolderPath))
			{
				try
				{
					Log.Information($"Deleting folder {subfolderName}...");
					Directory.Delete(replicaSubfolderPath, true);
					Log.Information($"Deleted folder {subfolderName}!");
				}
				catch (IOException ex)
				{
					Log.Error(ex, "Error deleting folder.");
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
		}

	}
}