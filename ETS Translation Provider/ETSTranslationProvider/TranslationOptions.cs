﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using ETSLPConverter;
using ETSTranslationProvider.ETSApi;
using ETSTranslationProvider.Model;
using Newtonsoft.Json;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemoryApi;

namespace ETSTranslationProvider
{
	public class TranslationOptions
	{
		private string ResolvedHost { get; set; }
		
		readonly TranslationProviderUriBuilder _uriBuilder;

		public TranslationOptions()
		{
			_uriBuilder = new TranslationProviderUriBuilder(TranslationProvider.TranslationProviderScheme);
			UseBasicAuthentication = true;
			Port = 8001;
			LPPreferences = new Dictionary<CultureInfo, ETSLanguagePair>();
		}

		public TranslationOptions(Uri uri) : this()
		{
			_uriBuilder = new TranslationProviderUriBuilder(uri);
		}

		public static readonly TranslationMethod ProviderTranslationMethod = TranslationMethod.MachineTranslation;

		public Dictionary<CultureInfo, ETSLanguagePair> LPPreferences { get; }

		public bool PersistCredentials { get; set; }

		[JsonIgnore]
		public string ApiToken { get; set; }

		public bool UseBasicAuthentication { get; set; }

		#region URI Properties
		public string Host
		{
			get => _uriBuilder.HostName;
			set
			{
				_uriBuilder.HostName = value;
				ResolvedHost = null;
			}
		}

		public int Port
		{
			get => _uriBuilder.Port;
			set => _uriBuilder.Port = value;
		}

		public APIVersion ApiVersion { get; set; }

		public string ApiVersionString => ApiVersion == APIVersion.v1 ? "v1" : "v2";

		public Uri Uri
		{
			get
			{
				var resolvedUri = new UriBuilder(_uriBuilder.Uri)
				{
					Host = ResolveHost()
				};
				return resolvedUri.Uri;
			}
		}

		public TradosToETSLP[] SetPreferredLanguages(LanguagePair[] languagePairs)
		{
			var etsLanguagePairs = ETSTranslatorHelper.GetLanguagePairs(this);
			if (!etsLanguagePairs.Any())
			{
				return null;
			}
			var languagePairChoices = languagePairs.GroupJoin(
				etsLanguagePairs,
				requestedLP =>
					new
					{
						SourceLanguageId = requestedLP.SourceCulture.ToETSCode(),
						TargetLanguageId = requestedLP.TargetCulture.ToETSCode()
					},
				installedLP =>
					new
					{
						SourceLanguageId = installedLP.SourceLanguageId,
						TargetLanguageId = installedLP.TargetLanguageId
					},
				(requestedLP, installedLP) =>
					new ETSApi.TradosToETSLP(
						tradosCulture: requestedLP.TargetCulture,
						etsLPs: installedLP.OrderBy(lp => lp.LanguagePairId).ToList())
			).ToList();

			var customEnginesMapping = new CustomEngines();

			//Fix for Spanish latin amerincan flavours
			CheckLatinSpanish(languagePairs, languagePairChoices, etsLanguagePairs);

			// Fix for French Canada engine	 which has language code on server frc
			CheckForFrCanada(languagePairs, languagePairChoices, etsLanguagePairs);

			//there is no engine which has ptb as source, we need to map it to por engine
			CheckForPtbSource(languagePairs, languagePairChoices, etsLanguagePairs);

			return languagePairChoices.ToArray();
		}

		/// <summary>
		/// Set dictionaries for each languagePairChoices 
		/// </summary>
		/// <param name="languagePairChoices"></param>
		public void SetDictionaries(TradosToETSLP[] languagePairChoices)
		{
			foreach (var languagePair in languagePairChoices)
			{
				ETSTranslatorHelper.GetDictionaries(languagePair, this);
			}
		}


		private void CheckForPtbSource(LanguagePair[] languagePairs, List<TradosToETSLP> languagePairChoices, ETSLanguagePair[] etsLanguagePairs)
		{
			var customEnginesMapping = new CustomEngines();
			var ptbSource = languagePairs.FirstOrDefault(lp => lp.SourceCulture.ThreeLetterWindowsLanguageName.Equals("PTB"));
			if (ptbSource != null)
			{
				var etsLangPairEngines = etsLanguagePairs
					.Where(lp => lp.SourceLanguageId.Equals(customEnginesMapping.PortugueseSourceEngineCode.ToLower())).ToList();
				var projectSourceLanguage =
					languagePairChoices.FirstOrDefault(s =>
						s.TradosCulture.ThreeLetterWindowsLanguageName.Equals(ptbSource.TargetCulture.ThreeLetterWindowsLanguageName));
				foreach (var etsEngine in etsLangPairEngines)
				{
					projectSourceLanguage?.ETSLPs?.Add(etsEngine);
				}
			}
		}

		private void CheckForFrCanada(LanguagePair[] languagePairs, List<TradosToETSLP> languagePairChoices, ETSLanguagePair[] etsLanguagePairs)
		{
			var customEnginesMapping = new CustomEngines();

			var frenchCanadianLp = languagePairs.FirstOrDefault(lp =>
				lp.SourceCulture.ThreeLetterWindowsLanguageName.Equals("FRC") ||
				lp.TargetCulture.ThreeLetterWindowsLanguageName.Equals("FRC"));
			if (frenchCanadianLp != null)
			{
				AddAditionalETSEngine(customEnginesMapping.FrenchCanadaEngineCode, customEnginesMapping.FrenchCanadaEngineCode,
					languagePairChoices, etsLanguagePairs);
			}
		}

		private void CheckLatinSpanish(LanguagePair[] languagePairs, List<TradosToETSLP> languagePairChoices, ETSLanguagePair[] etsLanguagePairs)
		{
			var customEnginesMapping = new CustomEngines();
			foreach (var languagePair in languagePairs)
			{
				var sourceSpanish = customEnginesMapping.LatinAmericanLanguageCodes.FirstOrDefault(s =>
					s.Equals(languagePair.SourceCulture.ThreeLetterWindowsLanguageName));

				if (sourceSpanish != null)
				{
					AddAditionalETSEngine(languagePair.SourceCulture.ThreeLetterWindowsLanguageName,
						customEnginesMapping.SpanishLatinAmericanEngineCode, languagePairChoices,
						etsLanguagePairs);
				}
				else
				{
					var targetSpanish = customEnginesMapping.LatinAmericanLanguageCodes.FirstOrDefault(s =>
						s.Equals(languagePair.TargetCulture.ThreeLetterWindowsLanguageName));
					if (targetSpanish != null)
					{
						AddAditionalETSEngine(languagePair.TargetCulture.ThreeLetterWindowsLanguageName,
							customEnginesMapping.SpanishLatinAmericanEngineCode, languagePairChoices,
							etsLanguagePairs);
					}
				}
			}
		}


		/// <summary>
		/// Used for flavours of a language to map the flavour to parent language code
		/// </summary>
		private void AddAditionalETSEngine(string languageWindowsCode, string engineCode,List<TradosToETSLP> languagePairChoices, ETSLanguagePair[] etsLanguagePairs)
		{
			if (!string.IsNullOrEmpty(engineCode))
			{
				{
					var etsLangPairEngines = etsLanguagePairs.Where(lp => lp.SourceLanguageId.Equals(engineCode.ToLower()) ||
					                                                      lp.TargetLanguageId.Equals(engineCode.ToLower())).ToList();
					var projectSourceLanguage = languagePairChoices.FirstOrDefault(s => s.TradosCulture.ThreeLetterWindowsLanguageName.Equals(languageWindowsCode));
					foreach (var etsEngine in etsLangPairEngines)
					{
						projectSourceLanguage?.ETSLPs?.Add(etsEngine);
					}
				}
			}
		}

		private string ResolveHost()
		{
			if (ResolvedHost != null)
			{
				return ResolvedHost;
			}
			// If the host is an IP address, preserve that, otherwise get the DNS host and cache it.
			ResolvedHost = IPAddress.TryParse(Host, out var address) ? Host : Dns.GetHostEntry(Host).HostName;
			return ResolvedHost;
		}
		#endregion
	}
}