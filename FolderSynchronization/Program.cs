﻿/*
 * Periodic Folder Synchronization Program
 * Author: Borivoj Kronowetter
 * Created: 13.03.2025
 *
 */

using System.IO;
using System.Security.Cryptography;
using Serilog;

class Program
{
	private static string source = "";
	private static string replica = "";
	private static bool syncingBool = false;
	private const long hashThreshold = 10 * 1024 * 1024; // 10 MB hashing threshold

	static void Main(string[] args)
	{
		if (args.Length < 4)
		{
			Console.WriteLine("Usage: FolderSynchronization.exe log_file_path source_folder_path replica_folder_path sync_interval_in_ms");
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
			int syncInterval = int.Parse(args[3]);

			if (syncInterval <= 0)
				throw new FormatException();
			if (!Directory.Exists(source))
				throw new DirectoryNotFoundException($"Source folder not found: {source}");

			Log.Information($"Source Folder: {source}");
			Log.Information($"Replica Folder: {replica}");
			Log.Information($"Synchronization Interval: {syncInterval} ms.");

			Timer timer = new(Sync, null, 0, syncInterval);

			Console.WriteLine("Synchronization running... Press Enter to exit.");
			Console.ReadLine();
			timer.Dispose();
		}
		catch (FormatException ex)
		{
			Log.Fatal(ex, "Invalid synchronization interval. Should be a positive integer [ms].");
		}
		catch (DirectoryNotFoundException ex)
		{
			Log.Fatal(ex, "Directory not found.");
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Unexpected error.");
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}

	private static void Sync(object? state)
	{
		if (syncingBool)
		{
			Log.Warning("Previous synchronization is still running. Skipping this interval.");
			return;
		}

		syncingBool = true;
		Log.Information($"Synchronizing...");
		SyncFolders(source, replica);
		Log.Information($"Synchronization complete!");
		syncingBool = false;
	}

	private static void SyncFolders(string sourceFolderPath, string replicaFolderPath)
	{

		try
		{
			if (!Directory.Exists(replicaFolderPath))
			{
				Directory.CreateDirectory(replicaFolderPath);
				Log.Information($"Created folder {replicaFolderPath}!");
			}
		}
		catch (IOException ex)
		{
			Log.Error(ex, $"Error copying folder {sourceFolderPath}.");
		}
		catch (UnauthorizedAccessException ex)
		{
			Log.Error(ex, $"Permission denied while copying folder {sourceFolderPath}.");
		}
		catch (Exception ex)
		{
			Log.Error(ex, $"Unexpected error copying folder {sourceFolderPath}.");
		}
		SyncFiles(sourceFolderPath, replicaFolderPath);
		DeleteFiles(sourceFolderPath, replicaFolderPath);
		DeleteFolders(sourceFolderPath, replicaFolderPath);
		foreach (string sourceSubfolderPath in Directory.GetDirectories(sourceFolderPath))
		{
			string subfolderName = Path.GetFileName(sourceSubfolderPath);
			string replicaSubfolderPath = Path.Combine(replicaFolderPath, subfolderName);
			SyncFolders(sourceSubfolderPath, replicaSubfolderPath);
			Log.Information($"Copied folder {subfolderName}!");
		}
	}

	private static void SyncFiles(string sourceFolderPath, string replicaFolderPath)
	{

		foreach (string sourceFilePath in Directory.GetFiles(sourceFolderPath))
		{
			FileInfo sourceFileInfo = new(sourceFilePath);
			string fileName = sourceFileInfo.Name;
			string replicaFilePath = Path.Combine(replicaFolderPath, fileName);
			FileInfo replicaFileInfo = new(replicaFilePath);
			try
			{
				if (!replicaFileInfo.Exists)
				{
					File.Copy(sourceFilePath, replicaFilePath);
					Log.Information($"Created file {fileName}!");
				}
				else if (sourceFileInfo.Length < hashThreshold || CalcMD5(sourceFilePath) != CalcMD5(replicaFilePath))
				{
					File.Copy(sourceFilePath, replicaFilePath, true);
				}
				Log.Information($"Copied file {fileName}!");
			}
			catch (IOException ex)
			{
				Log.Error(ex, $"Error copying files {fileName}.");
			}
			catch (UnauthorizedAccessException ex)
			{
				Log.Error(ex, $"Permission denied while copying file {fileName}.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Unexpected error copying file {fileName}.");
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
					Log.Error(ex, $"Error deleting file {fileName}.");
				}
				catch (UnauthorizedAccessException ex)
				{
					Log.Error(ex, $"Permission denied while deleting file {fileName}.");
				}
				catch (Exception ex)
				{
					Log.Error(ex, $"Unexpected error deleting file {fileName}.");
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
					Directory.Delete(replicaSubfolderPath, true);
					Log.Information($"Deleted folder {subfolderName}!");
				}
				catch (IOException ex)
				{
					Log.Error(ex, $"Error deleting folder {subfolderName}.");
				}
				catch (UnauthorizedAccessException ex)
				{
					Log.Error(ex, $"Permission denied while deleting folder {subfolderName}.");
				}
				catch (Exception ex)
				{
					Log.Error(ex, $"Unexpected error deleting folder {subfolderName}.");
				}
			}
		}
	}

	private static string CalcMD5(string filePath)
	{
		using (var md5 = MD5.Create())
		using (var stream = File.OpenRead(filePath))
		{
			byte[] md5Hash = md5.ComputeHash(stream);
			return Convert.ToHexStringLower(md5Hash);
		}
	}
}