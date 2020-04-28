﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sdl.Community.StarTransit.Shared.Import;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Utils;

namespace Sdl.Community.StarTransit.Shared.Services
{
	public class PackageService
	{
		private readonly List<KeyValuePair<string, string>> _dictionaryPropetries = new List<KeyValuePair<string, string>>();
		private readonly Dictionary<string, List<KeyValuePair<string, string>>> _pluginDictionary = new Dictionary<string, List<KeyValuePair<string, string>>>();

		private static PackageModel _package = new PackageModel();
		private const char LanguageTargetSeparator = ' ';

		/// <summary>
		/// Opens a ppf package and saves to files to temp folder
		/// </summary>
		/// <param name="packagePath"></param>
		/// <param name="pathToTempFolder"></param>
		public async Task<PackageModel> OpenPackage(string packagePath, string pathToTempFolder)
		{
			try
			{
				var entryName = string.Empty;
				if (File.Exists(packagePath))
				{
					using (var archive = ZipFile.OpenRead(packagePath))
					{
						foreach (var entry in archive.Entries)
						{
							var subdirectoryPath = Path.GetDirectoryName(entry.FullName);
							if (!Directory.Exists(Path.Combine(pathToTempFolder, subdirectoryPath)))
							{
								Directory.CreateDirectory(Path.Combine(pathToTempFolder, subdirectoryPath));
							}

							entry.ExtractToFile(Path.Combine(pathToTempFolder, entry.FullName));

							if (entry.FullName.EndsWith(".PRJ", StringComparison.OrdinalIgnoreCase))
							{
								entryName = entry.FullName;
							}
						}
					}

					return await ReadProjectMetadata(pathToTempFolder, entryName);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"OpenPackage method: {ex.Message}\n {ex.StackTrace}");
			}
			return new PackageModel();
		}

		/// <summary>
		/// Reads the metadata from .PRJ file
		/// </summary>
		/// <param name="pathToTempFolder"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private async Task<PackageModel> ReadProjectMetadata(string pathToTempFolder, string fileName)
		{
			try
			{
				var filePath = Path.Combine(pathToTempFolder, fileName);
				var keyProperty = string.Empty;

				using (var reader = new StreamReader(filePath, Encoding.Default))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (line.StartsWith("[") && line.EndsWith("]"))
						{
							var valuesDictionaries = new List<KeyValuePair<string, string>>();
							if (keyProperty != string.Empty && _dictionaryPropetries.Count != 0)
							{
								valuesDictionaries.AddRange(
									_dictionaryPropetries.Select(
										property => new KeyValuePair<string, string>(property.Key, property.Value)));
								if (_pluginDictionary.ContainsKey(keyProperty))
								{
									_pluginDictionary[keyProperty].AddRange(valuesDictionaries);
								}
								else
								{
									_pluginDictionary.Add(keyProperty, valuesDictionaries);
								}
								_dictionaryPropetries.Clear();
							}

							var firstPosition = line.IndexOf("[", StringComparison.Ordinal) + 1;
							var lastPosition = line.IndexOf("]", StringComparison.Ordinal) - 1;
							keyProperty = line.Substring(firstPosition, lastPosition);
						}
						else
						{
							var properties = line.Split('=');
							_dictionaryPropetries.Add(new KeyValuePair<string, string>(properties[0], properties[1]));
						}
					}
				}

				var packageModel = await CreateModel(pathToTempFolder);
				packageModel.PathToPrjFile = filePath;

				_package = packageModel;
				return packageModel;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ReadProjectMetadata method: {ex.Message}\n {ex.StackTrace}");
			}
			return new PackageModel();
		}

		public PackageModel GetPackageModel()
		{
			return _package;
		}

		/// <summary>
		/// Creates a package model
		/// </summary>
		/// <param name="pathToTempFolder"></param>
		/// <returns></returns>
		private async Task<PackageModel> CreateModel(string pathToTempFolder)
		{
			var model = new PackageModel();
			try
			{
				CultureInfo sourceLanguage = null;
				var languagePairList = new List<LanguagePair>();

				if (_pluginDictionary.ContainsKey("Admin"))
				{
					var propertiesDictionary = _pluginDictionary["Admin"];
					foreach (var item in propertiesDictionary)
					{
						if (item.Key == "ProjectName")
						{
							model.Name = item.Value;
						}
					}
				}

				if (_pluginDictionary.ContainsKey("Languages"))
				{
					var propertiesDictionary = _pluginDictionary["Languages"];
					foreach (var item in propertiesDictionary)
					{
						if (item.Key == "SourceLanguage")
						{
							var sourceLanguageCode = int.Parse(item.Value);
							sourceLanguage = Language(sourceLanguageCode);

						}
						if (item.Key == "TargetLanguages")
						{
							//we assume languages code are separated by " "
							var languages = item.Value.Split(LanguageTargetSeparator);

							foreach (var language in languages)
							{
								var targetLanguageCode = int.Parse(language);
								var cultureInfo = Language(targetLanguageCode);
								var pair = new LanguagePair
								{
									SourceLanguage = sourceLanguage,
									TargetLanguage = cultureInfo
								};
								languagePairList.Add(pair);
							}
						}
					}
				}
				model.LanguagePairs = languagePairList;
				if (model.LanguagePairs.Count > 0)
				{
					//for source
					var sourceFilesAndTmsPath = GetFilesAndTmsFromTempFolder(pathToTempFolder, sourceLanguage);
					var filesAndMetadata = ReturnSourceFilesNameAndMetadata(sourceFilesAndTmsPath);

					//for target
					foreach (var languagePair in model.LanguagePairs)
					{
						var targetFilesAndTmsPath = GetFilesAndTmsFromTempFolder(pathToTempFolder, languagePair.TargetLanguage);
						AddFilesAndTmsToModel(languagePair, filesAndMetadata, targetFilesAndTmsPath);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"CreateModel method: {ex.Message}\n {ex.StackTrace}");
			}
			return model;
		}

		private void AddFilesAndTmsToModel(LanguagePair languagePair,
			Tuple<List<string>, List<StarTranslationMemoryMetadata>> sourceFilesAndTmsPath,
			List<string> targetFilesAndTmsPath)
		{
			try
			{
				var pathToTargetFiles = new List<string>();
				var tmMetaDatas = new List<StarTranslationMemoryMetadata>();
				var sourcefileList = sourceFilesAndTmsPath.Item1;
				var tmMetadataList = sourceFilesAndTmsPath.Item2;

				languagePair.HasTm = tmMetadataList.Count > 0;
				languagePair.SourceFile = sourcefileList;

				foreach (var file in targetFilesAndTmsPath)
				{
					var isTm = IsTmFile(file);
					if (isTm)
					{
						var targetFileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
						//selects the source tm which has the same id with the target tm id
						var metaData = tmMetadataList?.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.SourceFile).Equals(targetFileNameWithoutExtension));

						if (metaData != null)
						{
							metaData.TargetFile = file;
						}
						tmMetaDatas.Add(metaData);
					}
					else
					{
						pathToTargetFiles.Add(file);
					}
				}
				languagePair.StarTranslationMemoryMetadatas = tmMetaDatas;
				languagePair.TargetFile = pathToTargetFiles;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"AddFilesAndTmsToModel method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Check if is a tm
		/// </summary>
		/// <param name="file"></param>
		/// <returns>true if this is a tm file</returns>
		private bool IsTmFile(string file)
		{
			var result = false;
			try
			{
				var tmFile = XElement.Load(file);
				var fileName = Path.GetFileName(file);
				if (tmFile.Attribute("ExtFileType") != null || fileName.StartsWith("_AEXTR", StringComparison.InvariantCultureIgnoreCase))
				{
					result = true;
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"IsTmFile method: {ex.Message}\n {ex.StackTrace}");
			}
			return result;
		}

		private List<string> GetFilesAndTmsFromTempFolder(string pathToTempFolder, CultureInfo language)
		{
			var filesAndTms = new List<string>();
			try
			{
				var extension = language.ThreeLetterWindowsLanguageName;
				extension = TransitExtension.MapStarTransitLanguage(extension);

				// used for following scenario: for one Windows language (Ex: Nigeria), Star Transit might use different extensions (eg: EDO,EFI)
				var multiLanguageExtensions = extension.Split(',');
				foreach (var multiLangExtension in multiLanguageExtensions)
				{
					var langExtension = multiLangExtension.TrimEnd().TrimStart();
					var files = Directory.GetFiles(pathToTempFolder, "*." + langExtension, SearchOption.TopDirectoryOnly).ToList();
					filesAndTms.AddRange(files);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"GetFilesAndTmsFromTempFolder method: {ex.Message}\n {ex.StackTrace}");
			}
			return filesAndTms;
		}

		private Tuple<List<string>, List<StarTranslationMemoryMetadata>> ReturnSourceFilesNameAndMetadata(List<string> filesAndTmsList)
		{
			var translationMemoryMetadataList = new List<StarTranslationMemoryMetadata>();
			var fileNames = new List<string>();

			foreach (var file in filesAndTmsList)
			{
				var isTm = IsTmFile(file);
				if (isTm)
				{
					var metadata = new StarTranslationMemoryMetadata
					{
						SourceFile = file
					};
					translationMemoryMetadataList.Add(metadata);

				}
				else
				{
					fileNames.Add(file);
				}
			}
			return new Tuple<List<string>, List<StarTranslationMemoryMetadata>>(fileNames, translationMemoryMetadataList);
		}

		/// <summary>
		/// Helper method which to get language from language code
		/// </summary>
		/// <param name="languageCode"></param>
		/// <returns>CultureInfo of the language</returns>
		private CultureInfo Language(int languageCode)
		{
			return new CultureInfo(languageCode);
		}
	}
}