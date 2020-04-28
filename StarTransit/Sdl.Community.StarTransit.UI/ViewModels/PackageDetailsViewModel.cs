﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using Sdl.Community.StarTransit.Shared.Interfaces;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Services;
using Sdl.Community.StarTransit.Shared.Utils;
using Sdl.Community.StarTransit.UI.Commands;
using Sdl.Community.StarTransit.UI.Helpers;
using Sdl.Community.StarTransit.UI.Interfaces;
using Sdl.Community.Toolkit.Core.Services;
using Sdl.ProjectAutomation.Core;

namespace Sdl.Community.StarTransit.UI.ViewModels
{
	public class PackageDetailsViewModel : BaseViewModel, IDataErrorInfo, IWindowActions
	{
	
		private readonly List<ProjectTemplateInfo> _studioTemplates;
        private readonly ObservableCollection<ProjectTemplateInfo> _templates;
        private readonly string _sourceLanguage;
        private readonly string _targetLanguage;
        private readonly PackageModel _packageModel;
        private readonly bool _canExecute;
        private readonly IMessageBoxService _messageBoxService;

		private string _textLocation;
        private string _txtName;
        private string _txtDescription;
		private bool _hasDueDate;
		private DateTime? _dueDate;
		private ProjectTemplateInfo _template;
		private Customer _selectedCustomer;
		
		private int _selectedHour;
		private int _selectedMinute;
		private string _selectedMoment;

        private ICommand _browseCommand;

		public List<int> HourList { get; set; }
		public List<int> MinutesList { get; set; }
		public List<string> MomentsList { get; set; }

		public PackageDetailsViewModel(PackageModel package, IMessageBoxService messageBoxService)
		{
			_packageModel = package;
			_txtName = package.Name;
			_txtDescription = package.Description;
			_studioTemplates = package.StudioTemplates;
			_textLocation = package.Location;
			_sourceLanguage = package.LanguagePairs[0].SourceLanguage.DisplayName;
			_templates = new ObservableCollection<ProjectTemplateInfo>(package.StudioTemplates);
			_hasDueDate = false;
			_targetLanguage = string.Empty;
            _messageBoxService = messageBoxService;

			foreach (var pair in package.LanguagePairs)
			{
				var targetLanguage = string.Concat(" ", pair.TargetLanguage.DisplayName);
				_targetLanguage = string.Concat(_targetLanguage, targetLanguage);
			}
			_canExecute = true;
			_selectedHour = -1;
			_selectedMinute = -1;
			_selectedMoment = string.Empty;
			GetCustomers();
			SetHours();
			SetMinutes();
			MomentsList = new List<string> { "AM", "PM" };
		}

		public int SelectedHour
		{
			get => _selectedHour;
			set
			{
				if (Equals(value, _selectedHour))
				{
					return;
				}
				_selectedHour = value;
				OnPropertyChanged(nameof(SelectedHour));
			}
		}

		public int SelectedMinute
		{
			get => _selectedMinute;
			set
			{
				if (Equals(value, _selectedMinute))
				{
					return;
				}
				_selectedMinute = value;
				OnPropertyChanged(nameof(SelectedMinute));
			}
		}

		public string SelectedMoment
		{
			get => _selectedMoment;
			set
			{
				if (Equals(value, _selectedMoment))
				{
					return;
				}
				_selectedMoment = value;
				OnPropertyChanged(nameof(SelectedMoment));
			}
		}

		public Customer SelectedCustomer
		{
			get => _selectedCustomer;
			set
			{
				if (Equals(_selectedCustomer, value))
				{
					return;
				}
				_selectedCustomer = value;
				OnPropertyChanged(nameof(SelectedCustomer));
			}
		}

		public ICommand BrowseCommand => _browseCommand ?? (_browseCommand = new CommandHandler(Browse, _canExecute));


		public List<Customer> Customers { get; set; }

		public ObservableCollection<ProjectTemplateInfo> Templates
		{
			get => _templates;
			set
			{
				OnPropertyChanged(nameof(Templates));
			}
		}

		public string TextLocation
		{
			get => _textLocation;
			set
			{
				if (Equals(value, _textLocation))
				{
					return;
				}
				_textLocation = value;
				OnPropertyChanged(nameof(TextLocation));
			}
		}

		public string Name
		{
			get => _txtName;
			set
			{
				if (Equals(value, _txtName))
				{
					return;
				}
				_txtName = value;
				OnPropertyChanged(nameof(Name));
			}
		}

		public string Description
		{
			get => _txtDescription;
			set
			{
				if (Equals(value, _txtDescription))
				{
					return;
				}
				_txtDescription = value;
				OnPropertyChanged(nameof(Description));
			}
		}

		public List<ProjectTemplateInfo> StudioTemplates
		{
			get => _studioTemplates;
			set
			{
				OnPropertyChanged(nameof(StudioTemplates));
			}
		}

		public ProjectTemplateInfo Template
		{
			get => _template;
			set
			{
				if (Equals(value, _template))
				{
					return;
				}
				_template = value;
				OnPropertyChanged(nameof(Template));
			}
		}

		public bool HasDueDate
		{
			get => _hasDueDate;
			set
			{
				if (Equals(value, _hasDueDate))
				{
					return;
				}
				_hasDueDate = value;
				OnPropertyChanged(nameof(HasDueDate));
			}
		}

		public DateTime? DueDate
		{
			get => _dueDate;
			set
			{
				if (Equals(value, _dueDate))
				{
					return;
				}
				_dueDate = value;
				OnPropertyChanged(nameof(DueDate));
			}
		}

		public string SourceLanguage
		{
			get => _sourceLanguage;
			set
			{
				if (Equals(value, _sourceLanguage))
				{
					return;

				}
				OnPropertyChanged(nameof(SourceLanguage));
			}
		}

		public string TargetLanguage
		{
			get => _targetLanguage;
			set
			{
				if (Equals(value, _targetLanguage))
				{
					return;
				}
				OnPropertyChanged(nameof(TargetLanguage));
			}
		}

		public string this[string columnName]
		{
			get
			{
				if (columnName.Equals("TextLocation") && string.IsNullOrEmpty(TextLocation))
				{
					return "Location is required";
				}
				if (columnName.Equals("Template") && Template == null)
				{
					return "Template is required";
				}
				if (columnName.Equals("SelectedHour") && SelectedHour == -1)
				{
					return "Please select an hour.";
				}
				if (columnName.Equals("SelectedMinute") && SelectedMinute == -1)
				{
					return "Please select minutes.";
				}
				return null;
			}
		}

		public string Error { get; }

		public Action CloseAction { get; set; }

		public PackageModel GetPackageModel()
		{
			try
			{
				_packageModel.Name = _txtName;
				_packageModel.Description = _txtDescription;
				_packageModel.Location = _textLocation;
				if (_hasDueDate)
				{
					_packageModel.DueDate = TimeHelper.SetDateTime(DueDate, SelectedHour, SelectedMinute, SelectedMoment);
				}

				_packageModel.ProjectTemplate = _template;
				_packageModel.HasDueDate = _hasDueDate;
				_packageModel.Customer = _selectedCustomer;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"GetPackageModel method: {ex.Message}\n {ex.StackTrace}");
			}
			return _packageModel;
		}

		public void Browse()
		{

			var folderDialog = new FolderSelectDialog();
			if (folderDialog.ShowDialog())
			{
				if (!Utils.IsFolderEmpty(folderDialog.FileName))
				{
					_messageBoxService.ShowWarningMessage("Please select an empty folder!","Folder not empty!");
				}
				else
				{
					TextLocation = folderDialog.FileName;
				}
			}
		}

		private void SetMinutes()
		{
			var minutesList = new List<int>();

			for (var i = 0; i <= 59; i++)
			{
				minutesList.Add(i);
			}
			MinutesList = minutesList;
		}

		private void GetCustomers()
		{
			try
			{
				var shortStudioVersion = GetInstalledStudioShortVersion();
				if (!string.IsNullOrEmpty(shortStudioVersion))
				{
					var projectsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $@"Studio {shortStudioVersion}\Projects\projects.xml");
					ReadCustomers(projectsPath);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"GetCustomers method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private string GetInstalledStudioShortVersion()
		{
			var studioService = new StudioVersionService();
			return studioService?.GetStudioVersion()?.ShortVersion;
		}

		private void ReadCustomers(string projectsPath)
		{
			try
			{
				var sourceProjectsXml = XElement.Load(projectsPath);
				if (!sourceProjectsXml.Element("Customers").HasElements) return;

				var customers = (sourceProjectsXml.Descendants("Customer")
					.Select(customer => new Customer
					{
						Guid = new Guid(customer?.Attribute("Guid")?.Value ?? throw new InvalidOperationException()),
						Name = customer?.Attribute("Name")?.Value,
						Email = customer?.Attribute("Email")?.Value
					})).OrderBy(c => c.Name).ToList();

				Customers = customers;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ReadCustomers method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void SetHours()
		{
			try
			{
				var hoursList = new List<int>();
				for (var i = 1; i <= 12; i++)
				{
					hoursList.Add(i);
				}
				HourList = hoursList;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"SetHours method: {ex.Message}\n {ex.StackTrace}");
			}
		}
	}
}