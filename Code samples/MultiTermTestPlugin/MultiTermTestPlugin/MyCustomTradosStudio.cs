﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sdl.Core.Globalization;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.MultiTerm.TMO.Interop;
using Sdl.ProjectAutomation.Core;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Presentation.DefaultLocations;

namespace MultiTermTestPlugin
{
	[Action("MultiTermTest")]
	[ActionLayout(typeof(TranslationStudioDefaultContextMenus.ProjectsContextMenuLocation))]
	public class MyCustomTradosStudio : AbstractAction
	{
		protected override void Execute()
		{
			var oMt = new Application();
			var localRep = oMt.LocalRepository;
			localRep.Connect("", "");
			var termbases = localRep.Termbases;
			var path = "";// termbase local path
			termbases.Add(path,"","");
			var termbase = termbases[path];
			var entries = termbase.Entries;
			//Concept with terms for English and German
			var entryText =
				"<conceptGrp><languageGrp><language type=\"English\" lang=\"en-US\"></language><termGrp><term>for</term></termGrp></languageGrp><languageGrp><language type=\"German\" lang=\"de-DE\"></language><termGrp><term>für</term></termGrp></languageGrp></conceptGrp>";
			entries.New(entryText, true);

			//var oServerRep = oMt.ServerRepository;
			//oServerRep.Location = "";
			//oServerRep.Connect("", "");
			//Console.WriteLine("Connection successful: " + oServerRep.IsConnected);

			//var oTbs = oServerRep.Termbases[0];
			//oTbs.Entries.New()
		}
	}
	[Action("Community: Add term", Name = "Community: Add Term to termbase", Icon = "")]
	[ActionLayout(typeof(TranslationStudioDefaultContextMenus.EditorDocumentContextMenuLocation), 10, DisplayType.Large)]
	public class AddNewConceptAction : AbstractAction
	{
		// Add new concept in TB based on user selection
		protected override void Execute()
		{
			var editorController = SdlTradosStudio.Application.GetController<EditorController>();
			var activeDocument = editorController?.ActiveDocument;
			
			if (activeDocument != null)
			{
				var sourceSelection = activeDocument.Selection?.Source?.ToString().TrimStart().TrimEnd();
				var targetSelection = activeDocument.Selection?.Target?.ToString().TrimStart().TrimEnd();
				if (!string.IsNullOrEmpty(sourceSelection) && !string.IsNullOrEmpty(targetSelection))
				{
					var sourceLanguageCode = activeDocument.ActiveFile?.SourceFile.Language.CultureInfo.Name;
					var targetLanguageCode = activeDocument.ActiveFile?.Language.CultureInfo.Name;
					// Add concept to default termbase for source and target language of active file
					var defaultTermbasePath = GetTermbasePath();
					var languageIndexes = GetDefaultTermbaseConfiguration().LanguageIndexes;
					if (!string.IsNullOrEmpty(defaultTermbasePath))
					{
						var entries = GetTermbaseEntries(defaultTermbasePath);
						var sourceIndexName = GetTermbaseIndex(languageIndexes, activeDocument.ActiveFile?.SourceFile.Language);
						var targetIndexName = GetTermbaseIndex(languageIndexes, activeDocument.ActiveFile?.Language);
						// get the number of term entries for a specific language, using the languageIndex
						//var numberOfLanguageEntries  = termbase.Information.NumberOfEntriesInIndex["sourceIndexName"];
						var entryText =	$"<conceptGrp><languageGrp><language type=\"{sourceIndexName}\" lang=\"{sourceLanguageCode}\"></language><termGrp><term>{sourceSelection}</term></termGrp></languageGrp><languageGrp><language type=\"{targetIndexName}\" lang=\"{targetLanguageCode}\"></language><termGrp><term>{targetSelection}</term></termGrp></languageGrp></conceptGrp>";
						entries.New(entryText, true);
					}
				}
			}
		}

		private string GetTermbaseIndex(List<TermbaseLanguageIndex> termbaseIndexes,Language currentLanguage)
		{
			if (termbaseIndexes.Any())
			{
				var termbaseIndex =
					termbaseIndexes.FirstOrDefault(t => t.ProjectLanguage.CultureInfo.Name.Equals(currentLanguage.CultureInfo.Name));
				if (termbaseIndex != null)
				{
					return termbaseIndex.TermbaseIndex;
				}
			}
			return string.Empty;
		}

		private TermbaseConfiguration GetDefaultTermbaseConfiguration()
		{
			var projectsController = SdlTradosStudio.Application.GetController<ProjectsController>();
			var activeProject = projectsController?.CurrentProject;

			return activeProject?.GetTermbaseConfiguration();
		}
		private string GetTermbasePath()
		{
			var termbConfig = GetDefaultTermbaseConfiguration();
			var termbaseSettingsXml = termbConfig?.Termbases.FirstOrDefault()?.SettingsXML;
			if (!string.IsNullOrEmpty(termbaseSettingsXml))
			{
				var xml = new XmlDocument();
				xml.LoadXml(termbaseSettingsXml);
				var xnList = xml.SelectNodes("/TermbaseSettings/Path");
				if (xnList?.Count > 0)
				{
					if (xnList[0].HasChildNodes)
					{
						return xnList[0].ChildNodes[0].Value;
					}
				}
			}
			return string.Empty;
		}

		private Entries GetTermbaseEntries(string termbasePath)
		{
			var multiTermApplication = new Application();
			var localRep = multiTermApplication.LocalRepository;
			localRep.Connect("", "");	

			var termbases = localRep.Termbases;
			var path = termbasePath;
			termbases.Add(path, "", "");
			var termbase = termbases[path];
			return termbase.Entries;
		}
	}
}
