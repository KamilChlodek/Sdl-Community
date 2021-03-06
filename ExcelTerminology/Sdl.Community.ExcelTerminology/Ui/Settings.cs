﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Sdl.Community.ExcelTerminology.Model;

namespace Sdl.Community.ExcelTerminology.Ui
{
	public partial class Settings : Form
	{
		private ProviderSettings _providerSettings;

		public Settings()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			sourceBox.Text = @"A";
			targetBox.Text = @"B";
			approvedBox.Text = @"C";
			separatorTextBox.Text = @"|";
			descriptionLbl.Text =
				@"From this screen you can fill your settings from your excel document.";

			var source = GetCultureNames();
			sourceLanguageComboBox.DataSource = source;
			sourceLanguageComboBox.DisplayMember = "DisplayName";
			sourceLanguageComboBox.ValueMember = "Name";
			var selectedSourceItem = source.Where(s => s.Name == "en-GB");
			sourceLanguageComboBox.SelectedItem = selectedSourceItem.FirstOrDefault();

			var target = GetCultureNames();
			targetLanguageComboBox.DataSource = target;
			targetLanguageComboBox.DisplayMember = "DisplayName";
			targetLanguageComboBox.ValueMember = "Name";
			var selectedTargetItem = target.Where(t => t.Name == "de-DE");
			targetLanguageComboBox.SelectedItem = selectedTargetItem.FirstOrDefault();
		}

		private void browseBtn_Click(object sender, EventArgs e)
		{
			var filePath = string.Empty;
			var file = new OpenFileDialog { Filter = @"Office Files|*.xlsx" };
			if (file.ShowDialog() == DialogResult.OK)
			{
				filePath = file.FileName;
			}

			pathTextBox.Text = filePath;
		}

		protected virtual List<CultureInfo> GetCultureNames()
		{
			return CultureInfo
				.GetCultures(CultureTypes.SpecificCultures).OrderBy(culture => culture.DisplayName)
				.ToList();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (DialogResult == DialogResult.Cancel ||
			    DialogResult == DialogResult.No ||
			    DialogResult == DialogResult.Abort) return;
			if (string.IsNullOrWhiteSpace(separatorTextBox.Text) || string.IsNullOrWhiteSpace(pathTextBox.Text))
			{
				MessageBox.Show(@"Please complete all fields", string.Empty, MessageBoxButtons.OK);
				e.Cancel = true;
				return;
			}

			var provider = new ProviderSettings
			{
				HasHeader = hasHeader.Checked,
				ApprovedColumn = approvedBox.Text.ToUpper(),
				SourceColumn = sourceBox.Text.ToUpper(),
				TargetColumn = targetBox.Text.ToUpper(),
				SourceLanguage = (CultureInfo)sourceLanguageComboBox.SelectedItem,
				TargetLanguage = (CultureInfo)targetLanguageComboBox.SelectedItem,
				Separator = separatorTextBox.Text[0],
				IsReadOnly = chkIsReadOnly.Checked,
				TermFilePath = pathTextBox.Text,
			};

			if (!provider.IsFileReady())
			{
				MessageBox.Show(
					@"The excel file configured as a terminology provider appears to be also opened in the Excel application. Please close the file!",
					@"Excel file is used by another process",
					MessageBoxButtons.OK);
				e.Cancel = true;
				return;
			}

			_providerSettings = provider;
		}

		public ProviderSettings GetSettings()
		{
			return _providerSettings;
		}

	}
}
