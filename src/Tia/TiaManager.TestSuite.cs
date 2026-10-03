using System;
using System.Collections.Generic;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.TestSuite;
using Siemens.Engineering.TestSuite.ApplicationTest;

namespace TiaOpennessMcp.Tia
{
    public partial class TiaManager
    {
        public List<TestCaseSummary> ListTestCases()
        {
            EnsureProjectOpen();
            var testSuite = _project!.GetService<TestSuiteService>();
            if (testSuite?.ApplicationTestGroup == null)
            {
                return new List<TestCaseSummary>();
            }

            var list = new List<TestCaseSummary>();
            foreach (TestCase tc in testSuite.ApplicationTestGroup.TestCases)
            {
                list.Add(new TestCaseSummary
                {
                    Name = tc.Name,
                    Scope = tc.GetScope()?.ToString() ?? ""
                });
            }
            return list;
        }

        public TestCaseImportResult LoadTestCase(string tatFilePath)
        {
            EnsureProjectOpen();
            if (!File.Exists(tatFilePath))
            {
                throw new FileNotFoundException($"Test case file not found: '{tatFilePath}'");
            }

            var testSuite = _project!.GetService<TestSuiteService>();
            if (testSuite?.ApplicationTestGroup == null)
            {
                throw new InvalidOperationException("TestSuiteService or ApplicationTestGroup is not available in the current project.");
            }

            var fi = new FileInfo(tatFilePath);
            var imported = testSuite.ApplicationTestGroup.TestCases.LoadFromFile(fi, ImportOptions.Override, TCLoadOptions.None);

            var res = new TestCaseImportResult
            {
                Success = true,
                Message = $"Successfully loaded {imported.Count} test case(s) from '{Path.GetFileName(tatFilePath)}'."
            };

            foreach (TestCase tc in imported)
            {
                res.LoadedCases.Add(new TestCaseSummary
                {
                    Name = tc.Name,
                    Scope = tc.GetScope()?.ToString() ?? ""
                });
            }

            return res;
        }
    }

    public class TestCaseSummary
    {
        public string Name { get; set; } = "";
        public string Scope { get; set; } = "";
    }

    public class TestCaseImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<TestCaseSummary> LoadedCases { get; } = new List<TestCaseSummary>();
    }
}
