﻿using Sdl.Community.ExcelTerminology.Services;
using Sdl.Community.ExcelTerminology.Tests.Helper;
using Xunit;

namespace Sdl.Community.ExcelTerminology.Tests
{
	public class ExcelTermProviderServiceTests
    {
        [Fact]
        public async void Load_Entries()
        {
            var providerSettings = TestHelper.CreateProviderSettings();
            var excelTermLoaderService = new TestExcelLoader();
            var parser = new Parser(providerSettings);
            var transformerService = new EntryTransformerService(parser);
            var actualTerms = await excelTermLoaderService.LoadTerms();
            var excelTermProviderService =
                new ExcelTermProviderService(excelTermLoaderService, transformerService);
            var expected = await excelTermProviderService.LoadEntries();
            Assert.Equal(expected.Count, actualTerms.Count);
        }       
    }
}