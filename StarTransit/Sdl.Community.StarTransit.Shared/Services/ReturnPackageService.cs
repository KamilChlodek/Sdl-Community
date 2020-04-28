﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Utils;
using Sdl.ProjectAutomation.Core;
using Sdl.TranslationStudioAutomation.IntegrationApi;

namespace Sdl.Community.StarTransit.Shared.Services
{
	public class ReturnPackageService
	{
		private readonly ProjectsController _projectsController;
		private readonly List<bool> _isTransitProject;
		private readonly ReturnPackage _returnPackage;

		public ReturnPackageService()
		{
			_isTransitProject = new List<bool>();
			_returnPackage = new ReturnPackage();
			var helpers = new Helpers();
			_projectsController = helpers.GetProjectsController();
		}

		/// <summary>
		/// Returns a list of StarTransit return package and  true if the projects selected are a StarTransit projects 
		/// </summary>
		/// <returns></returns>
		public Tuple<ReturnPackage, string> GetReturnPackage()
		{
			try
			{
				_isTransitProject?.Clear();
				var projects = _projectsController?.SelectedProjects.ToList();
				var message = string.Empty;
				if (projects?.Count > 1)
				{
					message = @"Please select only one project.";
					return new Tuple<ReturnPackage, string>(null, message);
				}

				if (projects != null)
				{
					foreach (var project in projects)
					{
						var targetFiles = project.GetTargetLanguageFiles().ToList();
						var isTransit = IsTransitProject(targetFiles);

						if (isTransit)
						{
							_returnPackage.FileBasedProject = project;
							_returnPackage.ProjectLocation = project.FilePath;
							_returnPackage.TargetFiles = targetFiles;
							//we take only the first file location, because the other files are in the same location
							_returnPackage.LocalFilePath = targetFiles[0].LocalFilePath;
							_isTransitProject?.Add(true);
						}
						else
						{
							_isTransitProject?.Add(false);
						}
					}
				}

				if (_isTransitProject != null)
				{
					if (_isTransitProject.Contains(false))
					{
						message = @"Please select a StarTransit project!";
						return new Tuple<ReturnPackage, string>(_returnPackage, message);
					}
				}

				return new Tuple<ReturnPackage, string>(_returnPackage, message);
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"GetReturnPackage method: {ex.Message}\n {ex.StackTrace}");
			}
			return null;
		}

		/// <summary>
		/// Check to see if the file type is the same with the Transit File Type
		/// </summary>
		/// <param name="filesPath"></param>
		/// <returns></returns>
		public bool IsTransitProject(List<ProjectFile> filesPath)
		{
			if (filesPath != null)
			{
				return filesPath.Any(f => f.FileTypeId != null && f.FileTypeId.Equals("Transit File Type 1.0.0.0"));
			}
			return false;
		}

		public void ExportFiles(ReturnPackage package)
		{
			try
			{
				var taskSequence = package.FileBasedProject.RunAutomaticTasks(package.TargetFiles.GetIds(), new string[]
				{
				 AutomaticTaskTemplateIds.GenerateTargetTranslations
				});

				var outputFiles = taskSequence.OutputFiles.ToList();
				CreateArchive(package);
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ExportFiles method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Creates an archive in the Return Package folder and add project files to it
		/// For the moment we add the files without runing any task on them
		/// </summary>
		/// <param name="package"></param>
		private void CreateArchive(ReturnPackage package)
		{
			try
			{
				ChangeMetadataFile(package.PathToPrjFile);

				var prjFileName = Path.GetFileNameWithoutExtension(package.PathToPrjFile);
				var archivePath = Path.Combine(package.FolderLocation, prjFileName + ".tpf");

				foreach (var targetFile in package.TargetFiles)
				{
					var pathToTargetFileFolder = targetFile.LocalFilePath.Substring(0, targetFile.LocalFilePath.LastIndexOf(@"\", StringComparison.Ordinal));

					if (!File.Exists(archivePath))
					{
						//create the archive, and add files to it
						using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
						{
							archive.CreateEntryFromFile(package.PathToPrjFile, string.Concat(prjFileName, ".PRJ"), CompressionLevel.Optimal);
							foreach (var file in package.TargetFiles)
							{
								pathToTargetFileFolder = file.LocalFilePath.Substring(0, file.LocalFilePath.LastIndexOf(@"\", StringComparison.Ordinal));
								var fileName = Path.GetFileNameWithoutExtension(file.LocalFilePath);

								archive.CreateEntryFromFile(Path.Combine(pathToTargetFileFolder, fileName), fileName, CompressionLevel.Optimal);
							}
						}
					}
					else
					{
						UpdateArchive(archivePath, prjFileName, package, pathToTargetFileFolder);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"CreateArchive method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void UpdateArchive(string archivePath, string prjFileName, ReturnPackage returnPackagePackage, string pathToTargetFileFolder)
		{
			try
			{
				// open the archive and delete old files
				// archive in update mode not overrides existing files 
				using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
				{
					var entriesCollection = new ObservableCollection<ZipArchiveEntry>(archive.Entries);
					foreach (var entry in entriesCollection)
					{

						if (entry.Name.Equals(string.Concat(prjFileName, ".PRJ")))
						{
							entry.Delete();
						}

						foreach (var project in returnPackagePackage.TargetFiles)
						{
							var projectFromArchiveToBeDeleted = archive.Entries.FirstOrDefault(n => n.Name.Equals(Path.GetFileNameWithoutExtension(project.Name)));
							projectFromArchiveToBeDeleted?.Delete();
						}
					}
				}

				//add files to archive
				using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
				{
					archive.CreateEntryFromFile(returnPackagePackage.PathToPrjFile, string.Concat(prjFileName, ".PRJ"), CompressionLevel.Optimal);
					foreach (var file in returnPackagePackage.TargetFiles)
					{
						var fileName = Path.GetFileNameWithoutExtension(file.LocalFilePath);
						pathToTargetFileFolder = file.LocalFilePath.Substring(0, file.LocalFilePath.LastIndexOf(@"\", StringComparison.Ordinal));
						archive.CreateEntryFromFile(Path.Combine(pathToTargetFileFolder, fileName), fileName, CompressionLevel.Optimal);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"UpdateArchive method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Reads the prj file and check if an update has been made already
		/// </summary>
		/// <param name="pathToPrjFile"></param>
		private void ChangeMetadataFile(string pathToPrjFile)
		{
			try
			{
				var metadata = new Metadata
				{
					ExchangedDate = CustomDateTime.CustomExchangeDate(DateTime.Now),
					ExchangedTime = CustomDateTime.CustomExchangeTime(DateTime.Now),
					LastChangedDate = CustomDateTime.CreateCustomDate(DateTime.Now)
				};

				using (var reader = new StreamReader(pathToPrjFile))
				{
					var fileContent = reader.ReadToEnd();
					reader.Close();

					MetadataBuilder(pathToPrjFile, metadata, !fileContent.Contains("PromptForNewWorkingDir"));
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ChangeMetadataFile method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Changes the metadata from prj file
		/// </summary>
		/// <param name="pathToPrjFile"></param>
		/// <param name="metadata"></param>
		/// <param name="createNewMetadata"></param>
		private void MetadataBuilder(string pathToPrjFile, Metadata metadata, bool createNewMetadata)
		{
			try
			{
				var builder = new StringBuilder();
				var lines = File.ReadAllLines(pathToPrjFile).ToList();
				foreach (var line in lines)
				{
					if (line.StartsWith("LastChangedDate"))
					{
						var newLine = line.Replace(line, string.Concat("LastChangedDate=", metadata.LastChangedDate));
						builder.Append(newLine + Environment.NewLine);
						continue;
					}

					//check to see if the project was edited before
					//if yes, that means we don't need to add this line anymore
					if (createNewMetadata)
					{
						if (line.Equals("[Exchange]"))
						{
							builder.AppendFormat("{0}{1}{2}{3}", "[Exchange]", Environment.NewLine, "PromptForNewWorkingDir=0", Environment.NewLine);
							continue;
						}
					}

					if (line.StartsWith("IsExchange"))
					{
						var newLine = line.Replace(line, "IsExchange=1");
						builder.Append(newLine + Environment.NewLine);
						continue;
					}
					if (line.StartsWith("ExchangeDate"))
					{
						var date = line.Replace(line, string.Concat("ExchangeDate=", metadata.ExchangedDate));
						builder.Append(date + Environment.NewLine);
						continue;
					}
					if (line.StartsWith("ExchangeTime"))
					{
						var time = line.Replace(line, string.Concat("ExchangeTime=", metadata.ExchangedTime));
						builder.Append(time + Environment.NewLine);
						continue;
					}
					builder.Append(line + Environment.NewLine);
				}

				File.WriteAllText(pathToPrjFile, builder.ToString());
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"MetadataBuilder method: {ex.Message}\n {ex.StackTrace}");
			}
		}
	}
}