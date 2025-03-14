using System.Diagnostics;
using System.IO;
using Serilog;

class Program
{
	static void Main(string[] args)
	{
		try
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: program.exe <logFilePath> <sourceFolderPath> <replicaFolderPath>");
				return;
			}

			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File(args[0], rollingInterval: RollingInterval.Day)
				.CreateLogger();

			Stopwatch sw = new Stopwatch();
			sw.Start();

			Sync(args[1], args[2]);

			sw.Stop();
			TimeSpan ts = sw.Elapsed;

			string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
			Log.Information($"Runtime: {elapsedTime}.");
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
			Log.CloseAndFlush();
		}
	}

	private static void Sync(string sourceFolderPath, string replicaFolderPath)
	{
		if (!Directory.Exists(sourceFolderPath))
		{
			throw new DirectoryNotFoundException($"Source folder not found: {sourceFolderPath}");
		}
		Log.Information($"Source Folder: {sourceFolderPath}");
		Log.Information($"Replica Folder: {replicaFolderPath}");
		Log.Information($"Synchronizing...");
		SyncFolders(sourceFolderPath, replicaFolderPath);
		Log.Information($"Synchronization complete!");
	}

	private static void SyncFiles(string sourceFolderPath, string replicaFolderPath)
	{
		try
		{
			foreach (string sourceFilePath in Directory.GetFiles(sourceFolderPath))
			{
				string fileName = Path.GetFileName(sourceFilePath);
				string replicaFilePath = Path.Combine(replicaFolderPath, fileName);
				Log.Information($"Copying file {fileName}...");
				File.Copy(sourceFilePath, replicaFilePath, true);
				Log.Information($"Copied file {fileName}!");
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

	private static void SyncFolders(string sourceFolderPath, string replicaFolderPath)
	{
		try
		{
			if (!Directory.Exists(replicaFolderPath))
			{
				Directory.CreateDirectory(replicaFolderPath);
			}
			SyncFiles(sourceFolderPath, replicaFolderPath);
			DeleteFiles(sourceFolderPath, replicaFolderPath);
			DeleteFolders(sourceFolderPath, replicaFolderPath);
			foreach (string sourceSubfolderPath in Directory.GetDirectories(sourceFolderPath))
			{
				string subfolderName = Path.GetFileName(sourceSubfolderPath);
				string replicaSubfolderPath = Path.Combine(replicaFolderPath, subfolderName);
				Log.Information($"Copying folder {subfolderName}...");
				SyncFolders(sourceSubfolderPath, replicaSubfolderPath);
				Log.Information($"Copied folder {subfolderName}!");
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
	}

	private static void DeleteFiles(string sourceFolderPath, string replicaFolderPath)
	{
		try
		{
			foreach (string replicaFilePath in Directory.GetFiles(replicaFolderPath))
			{
				string fileName = Path.GetFileName(replicaFilePath);
				string sourceFilePath = Path.Combine(sourceFolderPath, fileName);
				if (!File.Exists(sourceFilePath))
				{
					Log.Information($"Deleting file {fileName}...");
					File.Delete(replicaFilePath);
					Log.Information($"Deleted file {fileName}!");
				}
			}
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

	private static void DeleteFolders(string sourceFolderPath, string replicaFolderPath)
	{
		try
		{
			foreach (string replicaSubfolderPath in Directory.GetDirectories(replicaFolderPath))
			{
				string subfolderName = Path.GetFileName(replicaSubfolderPath);
				string sourceSubfolderPath = Path.Combine(sourceFolderPath, subfolderName);
				if (!Directory.Exists(sourceSubfolderPath))
				{
					Log.Information($"Deleting folder {subfolderName}...");
					Directory.Delete(replicaSubfolderPath, true);
					Log.Information($"Deleted folder {subfolderName}!");
				}
			}
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