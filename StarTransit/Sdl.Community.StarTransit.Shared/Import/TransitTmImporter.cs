﻿using System;
using System.Globalization;
using System.IO;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Utils;
using Sdl.FileTypeSupport.Framework.IntegrationApi;
using Sdl.LanguagePlatform.Core.Tokenization;
using Sdl.LanguagePlatform.TranslationMemory;
using Sdl.LanguagePlatform.TranslationMemoryApi;
using Sdl.ProjectAutomation.Core;

namespace Sdl.Community.StarTransit.Shared.Import
{
	public class TransitTmImporter
	{
		#region Private Fields
		private readonly IFileTypeManager _fileTypeManager;
		private readonly FileBasedTranslationMemory _fileBasedTM;
		#endregion

		#region Constructors
		public TransitTmImporter(LanguagePair pair,	IFileTypeManager fileTypeManager, string studioTranslationMemory)
		{
			_fileTypeManager = fileTypeManager;

			if (pair.CreateNewTm)
			{
				_fileBasedTM = new FileBasedTranslationMemory(
					studioTranslationMemory,
					string.Empty,
					pair.SourceLanguage,
					pair.TargetLanguage,
					GetFuzzyIndexes(),
					GetRecognizers(),
					TokenizerFlags.DefaultFlags,
					GetWordCountFlags());
			}
			else
			{
				_fileBasedTM = new FileBasedTranslationMemory(pair.TmPath);
			}
		}

		public TransitTmImporter(IFileTypeManager fileTypeManager, LanguagePair pair, string projectPath, string fileName)
		{
			_fileTypeManager = fileTypeManager;

			_fileBasedTM = new FileBasedTranslationMemory(
						Path.Combine(projectPath, string.Concat(fileName, ".sdltm")),
						string.Concat(fileName, " description"),
						pair.SourceLanguage,
						pair.TargetLanguage,
						GetFuzzyIndexes(),
				 		GetRecognizers(),
						TokenizerFlags.DefaultFlags,
						GetWordCountFlags());

			_fileBasedTM.LanguageResourceBundles.Clear();
			_fileBasedTM.Save();
			TMFilePath = _fileBasedTM.FilePath;
		}

		#endregion

		#region Public Properties
		public string TMFilePath { get; set; }
		#endregion

		#region Public Methods
		public void ImportStarTransitTm(string starTransitTm)
		{
			try
			{
				var sdlXliffFullPath = CreateTemporarySdlXliff(starTransitTm);
				ImportSdlXliffIntoTm(sdlXliffFullPath);
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ImportStarTransitTm method: {ex.Message}\n {ex.StackTrace}");
			}
		}

	    public TranslationProviderReference GetTranslationProviderReference()
		{
			return new TranslationProviderReference(_fileBasedTM.FilePath, true);
		}
		#endregion

		#region Private Methods
		private void ImportSdlXliffIntoTm(string sdlXliffFullPath)
		{
			try
			{
				var tmImporter = new TranslationMemoryImporter(_fileBasedTM.LanguageDirection);
				var importSettings = new ImportSettings()
				{
					IsDocumentImport = false,
					CheckMatchingSublanguages = false,
					IncrementUsageCount = false,
					NewFields = ImportSettings.NewFieldsOption.Ignore,
					PlainText = false,
					ExistingTUsUpdateMode = ImportSettings.TUUpdateMode.AddNew
				};
				tmImporter.ImportSettings = importSettings;
				tmImporter.Import(sdlXliffFullPath);
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ImportSdlXliffIntoTm method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Create temporary bilingual file (sdlxliff) used to import the information
		/// in Studio translation memories
		/// </summary>
		/// <param name="starTransitTM"></param>
		/// <param name="pathToExtractFolder"></param>
		/// <returns></returns>
		private string CreateTemporarySdlXliff(string starTransitTM)
		{
			try
			{
				var pathToExtractFolder = CreateFolderToExtract(Path.GetDirectoryName(starTransitTM));
				var generatedXliffName = string.Format("{0}{1}", Path.GetFileNameWithoutExtension(starTransitTM), ".sdlxliff");

				var sdlXliffFullPath = Path.Combine(pathToExtractFolder, generatedXliffName);
				var converter = _fileTypeManager.GetConverterToDefaultBilingual(starTransitTM, sdlXliffFullPath, null);

				converter.Parse();
				return sdlXliffFullPath;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"CreateTemporarySdlXliff method: {ex.Message}\n {ex.StackTrace}");
			}
			return string.Empty;
		}

		/// <summary>
		/// Create temporary folder for TM import
		/// </summary>
		/// <param name="pathToTemp"></param>
		/// <returns></returns>
		private string CreateFolderToExtract(string pathToTemp)
		{
			var pathToExtractFolder = Path.Combine(pathToTemp, "TmExtract");
			if (!Directory.Exists(pathToExtractFolder))
			{
				Directory.CreateDirectory(pathToExtractFolder);
			}
			return pathToExtractFolder;
		}
		
		private static FuzzyIndexes GetFuzzyIndexes()
		{
			return FuzzyIndexes.SourceCharacterBased |
				FuzzyIndexes.SourceWordBased |
				FuzzyIndexes.TargetCharacterBased |
				FuzzyIndexes.TargetWordBased;
		}

		private static BuiltinRecognizers GetRecognizers()
		{
			return BuiltinRecognizers.RecognizeAll;
		}

		private static WordCountFlags GetWordCountFlags()
		{
			return WordCountFlags.BreakOnTag | WordCountFlags.BreakOnDash | WordCountFlags.BreakOnApostrophe;
		}
		#endregion
	}
}
