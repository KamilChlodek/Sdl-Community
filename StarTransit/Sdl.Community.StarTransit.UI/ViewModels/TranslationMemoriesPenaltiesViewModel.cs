﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.UI.Commands;

namespace Sdl.Community.StarTransit.UI.ViewModels
{
	public class TranslationMemoriesPenaltiesViewModel : BaseViewModel
	{
		#region Private Fields
		private PackageModel _packageModel;
		private ObservableCollection<TranslationMemoriesPenaltiesModel> _translationMemoriesPenaltiesModelList;
		private string _translationMemoryName;
		private string _translationMemoryPath;
		private int _tmPenalty;
		private ICommand _okCommand;

		#endregion

		#region Constructors
		public TranslationMemoriesPenaltiesViewModel(PackageModel packageModel)
		{
			_packageModel = packageModel;
			
			if(!(_packageModel is null))
			{
				_packageModel.TMPenalties = new Dictionary<string, int>();
			}
			_translationMemoriesPenaltiesModelList = new ObservableCollection<TranslationMemoriesPenaltiesModel>();
			LoadTranslationMemories();
		}
		#endregion

		#region Public Properties
		public ObservableCollection<TranslationMemoriesPenaltiesModel> TranslationMemoriesPenaltiesModelList
		{
			get => _translationMemoriesPenaltiesModelList;
			set
			{
				_translationMemoriesPenaltiesModelList = value;
				OnPropertyChanged(nameof(TranslationMemoriesPenaltiesModelList));
			}
		}

		public string TranslationMemoryName
		{
			get => _translationMemoryName;
			set
			{
				if (Equals(value, _translationMemoryName))
				{
					return;
				}
				_translationMemoryName = value;
				OnPropertyChanged(nameof(TranslationMemoryName));
			}
		}

		public string TranslationMemoryPath
		{
			get => _translationMemoryPath;
			set
			{
				if (Equals(value, _translationMemoryPath))

				{
					return;
				}
				_translationMemoryPath = value;
				OnPropertyChanged(nameof(TranslationMemoryPath));
			}
		}

		public int TMPenalty
		{
			get => _tmPenalty;
			set
			{
				if (Equals(value, _tmPenalty))
				{
					return;
				}
				_tmPenalty = value;
				OnPropertyChanged(nameof(TMPenalty));
			}
		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Load translation memories
		/// </summary>
		private void LoadTranslationMemories()
		{
			if (_packageModel != null)
			{
				foreach (var langPair in _packageModel.LanguagePairs)
				{
					if (langPair.HasTm)
					{
						foreach (var filePath in langPair.StarTranslationMemoryMetadatas)
						{
							if (!filePath.TargetFile.Contains("_AEXTR_MT_"))
							{
								TranslationMemoryName = Path.GetFileName(filePath.TargetFile);
								TranslationMemoryPath = filePath.TargetFile;

								var translationMemoriesPenaltiesModel = new TranslationMemoriesPenaltiesModel()
								{
									TranslationMemoryName = TranslationMemoryName,
									TranslationMemoryPath = TranslationMemoryPath,
									TMPenalty = TMPenalty
								};
								TranslationMemoriesPenaltiesModelList.Add(translationMemoriesPenaltiesModel);
							}
						}
					}
				}
			}
		}
		#endregion

		#region Commands
		public ICommand OkCommand => _okCommand ?? (_okCommand = new CommandHandler(OkAction, true));
		#endregion

		#region Actions
		private void OkAction()
		{
			foreach (var tm in TranslationMemoriesPenaltiesModelList)
			{
				if (tm.TMPenalty > 0)
				{
					_packageModel.TMPenalties.Add(tm.TranslationMemoryPath, tm.TMPenalty);
				}
			}
		}
		#endregion
	}
}