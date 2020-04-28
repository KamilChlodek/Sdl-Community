﻿using System;
using System.IO;
using System.Linq;
using System.Xml;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Services;
using Sdl.Community.StarTransit.Shared.Utils;
using Sdl.Core.Globalization;
using Sdl.Core.Settings;
using Sdl.FileTypeSupport.Framework;
using Sdl.FileTypeSupport.Framework.NativeApi;

namespace Sdl.Community.StarTransit.Shared.Import
{
	public class TransitSniffer : INativeFileSniffer
	{
		static string _BilingualDocument = "Transit";
		private string _srcFileExtension;
		private string _trgFileExtension;
		private PackageModel _packageModel;
		private PackageService _packageService = new PackageService();

		public SniffInfo Sniff(string nativeFilePath, Language suggestedSourceLanguage,
			Codepage suggestedCodepage, INativeTextLocationMessageReporter messageReporter,
			ISettingsGroup settingsGroup)
		{
			var info = new SniffInfo();
			try
			{
				_packageModel = _packageService.GetPackageModel();
				var sourceLanguageExtension = string.Empty;
				if (_packageModel != null)
				{
					sourceLanguageExtension = _packageModel.LanguagePairs[0].SourceLanguage.ThreeLetterWindowsLanguageName;
				}

				if (File.Exists(nativeFilePath))
				{
					// call method to check if file is supported
					info.IsSupported = IsFileSupported(nativeFilePath, sourceLanguageExtension);
					// call method to determine the file language pair
					GetFileLanguages(ref info, nativeFilePath);
				}
				else
				{
					info.IsSupported = true;
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"Sniff method: {ex.Message}\n {ex.StackTrace}");
			}
			return info;
		}

		// determine whether a given file is supported based on the
		// root element
		private bool IsFileSupported(string nativeFilePath, string sourceLanguageExtension)
		{
			var result = false;
			try
			{
				var rootElementMatches = false;
				var sourceFileFound = false;

				// check whether header tag name equals Transit, if not the file cannot be opened in Studio
				var doc = new XmlDocument();
				doc.Load(nativeFilePath);
				if (doc.DocumentElement.Name == _BilingualDocument)
				{
					rootElementMatches = true;
				}
				var pos = nativeFilePath.LastIndexOf("\\");
				var path = nativeFilePath.Substring(0, pos);

				// check whether source file with the same name exists
				// if it does not exist, the file cannot be opened in Studio
				var files = Directory.GetFiles(path).ToList();
				if (files.Count != 0)
				{
					var sourceFileName = Path.GetFileNameWithoutExtension(nativeFilePath) + "." + sourceLanguageExtension;
					var sourceFilesFromFolder = files.Where(s => s.EndsWith(sourceLanguageExtension)).ToList();

					var sourceFile = sourceFilesFromFolder.FirstOrDefault(f => f.Contains(sourceFileName));
					if (sourceFile != null)
					{
						sourceFileFound = true;
						_srcFileExtension = sourceLanguageExtension;
						_trgFileExtension = Path.GetExtension(nativeFilePath).Replace(".", "");
					}
				}

				// if both conditions are met, then the file can be supported by Studio
				result = rootElementMatches && sourceFileFound ? true : false;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"IsFileSupported method: {ex.Message}\n {ex.StackTrace}");
			}
			return result;
		}

		// retrieve the source and target language
		// from the file header
		private void GetFileLanguages(ref SniffInfo info, string nativeFilePath)
		{
			try
			{
				var doc = new XmlDocument();
				doc.Load(nativeFilePath);

				info.DetectedSourceLanguage = new Pair<Language, DetectionLevel>(new Language(MapLanguage(_srcFileExtension)), DetectionLevel.Certain);
				info.DetectedTargetLanguage = new Pair<Language, DetectionLevel>(new Language(MapLanguage(_trgFileExtension)), DetectionLevel.Certain);
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"GetFileLanguages method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private string MapLanguage(string fileExtension)
		{
			fileExtension = fileExtension.ToUpper();
			switch (fileExtension)
			{
				case "DEU":
					return "de-DE";
				case "AFK":
					return "af-ZA";
				case "AMH":
					return "am-ET";
				case "SQI":
					return "sq-AL";
				case "ARG":
					return "ar-DZ";
				case "ARH":
					return "ar-BH";
				case "ARE":
					return "ar-EG";
				case "ARI":
					return "ar-IQ";
				case "ARJ":
					return "ar-JO";
				case "ARK":
					return "ar-KW";
				case "ARB":
					return "ar-LB";
				case "ARL":
					return "ar-LY";
				case "ARM":
					return "ar-MA";
				case "ARO":
					return "ar-OM";
				case "ARQ":
					return "ar-QA";
				case "ARA":
					return "ar-SA";
				case "ARS":
					return "ar-SY";
				case "ART":
					return "ar-TN";
				case "ARU":
					return "ar-AE";
				case "ARY":
					return "ar-YE";
				case "EUQ":
					return "eu-ES";
				case "BEL":
					return "be-BY";
				case "BGR":
					return "bg-BG";
				case "CAT":
					return "ca-ES";
				case "CHS":
					return "zh-CN";
				case "ZHH":
					return "zh-HK";
				case "ZHI":
					return "zh-SG";
				case "CHT":
					return "zh-TW";
				case "HRV":
					return "hr-HR";
				case "CSY":
					return "cs-CZ";
				case "DAN":
					return "da-DK";
				case "NLB":
					return "nl-BE";
				case "NLD":
					return "nl-NL";
				case "ENA":
					return "en-AU";
				case "ENL":
					return "en-BZ";
				case "ENC":
					return "en-CA";
				case "ENI":
					return "en-IE";
				case "ENJ":
					return "en-JM";
				case "ENZ":
					return "en-NZ";
				case "ENS":
					return "en-ZA";
				case "ENT":
					return "en-TT";
				case "ENG":
					return "en-GB";
				case "ENU":
					return "en-US";
				case "ETI":
					return "et-EE";
				case "FOS":
					return "fo-FO";
				case "FAR":
					return "fa-IR";
				case "FIN":
					return "fi-FI";
				case "FRB":
					return "fr-BE";
				case "FRC":
					return "fr-CA";
				case "FRL":
					return "fr-LU";
				case "FRS":
					return "fr-CH";
				case "DEA":
					return "de-AT";
				case "DEC":
					return "de-LI";
				case "DEL":
					return "de-LU";
				case "DES":
					return "de-CH";
				case "ELL":
					return "el-GR";
				case "HEB":
					return "he-IL";
				case "HIN":
					return "hi-IN";
				case "HUN":
					return "hu-HU";
				case "ISL":
					return "is-IS";
				case "ITA":
					return "it-IT";
				case "ITS":
					return "it-CH";
				case "JPN":
					return "ja-JP";
				case "KOR":
					return "ko-KR";
				case "LVI":
					return "lv-LV";
				case "LTH":
					return "lt-LT";
				case "MKD":
					return "mk-MK";
				case "PLK":
					return "pl-PL";
				case "PTB":
					return "pt-BR";
				case "ROM":
					return "ro-RO";
				case "RUS":
					return "ru-RU";
				case "SKY":
					return "sk-SK";
				case "SLV":
					return "sl-SI";
				case "ESS":
					return "es-AR";
				case "ESB":
					return "es-BO";
				case "ESL":
					return "es-CL";
				case "ESO":
					return "es-CO";
				case "ESC":
					return "es-CR";
				case "ESD":
					return "es-DO";
				case "ESF":
					return "es-EC";
				case "ESE":
					return "es-SV";
				case "ESG":
					return "es-GT";
				case "ESH":
					return "es-HN";
				case "ESM":
					return "es-MX";
				case "ESI":
					return "es-NI";
				case "ESA":
					return "es-PA";
				case "ESZ":
					return "es-PY";
				case "ESR":
					return "es-PE";
				case "ES":
					return "es-PR";
				case "ESP":
					return "es-ES";
				case "ESY":
					return "es-UY";
				case "ESV":
					return "es-VE";
				case "SVF":
					return "sv-FI";
				case "THA":
					return "th-TH";
				case "TRK":
					return "tr-TR";
				case "UKR":
					return "uk-UA";
				case "URD":
					return "ur-PK";
				case "VIT":
					return "vi-VN";
				case "AZC":
					return "az-Cyrl-AZ";
				case "AZE":
					return "az-Cyrl-AZ";
				case "ENN":
					return "en-IN";
				case "ENM":
					return "en-MY";
				case "ENP":
					return "en-PH";
				case "FRA":
					return "fr-FR";
				case "FRM":
					return "fr-MC";
				case "FRO":
					return "fo-FO";
				case "FRY":
					return "fy-NL";
				case "GAL":
					return "gl-ES";
				case "GRC":
					return "el-GR";
				case "IBO":
					return "ig-NG";
				case "IND":
					return "id-ID";
				case "KAZ":
					return "kk-KZ";
				case "MNG":
					return "mn-MN";
				case "MSB":
					return "ms-BN";
				case "MSL":
					return "ms-MY";
				case "NON":
					return "nn-NO";
				case "NOR":
					return "nb-NO";
				case "NSO":
					return "nso-ZA";
				case "PTG":
					return "pt-PT";
				case "SRL":
					return "sr";
				case "SVE":
					return "sv-SE";
				case "SRB":
					return "sr-Latn";
				case "SWK":
					return "sw-KE";
				case "TKM":
					return "tk-TM";
				case "UZB":
					return "es-VE";
				case "VEN":
					return "uz-Cyrl-UZ";
				case "ZHM":
					return "zh-MO";
				case "ZUL":
					return "zu-ZA";
				case "ROU":
					return "ro-RO";
				case "WEL":
					return "cy-GB";

				default:
					return "";
			}
		}
	}
}