using System.Diagnostics;

namespace MDS_PROJECT.Models
{
    public class PythonScriptExecutor
    {
        private readonly string _pythonInterpreterPath;

        public PythonScriptExecutor(string pythonInterpreterPath)
        {
            _pythonInterpreterPath = pythonInterpreterPath;
        }

        public string ExecutePythonScript(string scriptPath, string query)
        {
            try
            {
                // Create process start info
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = _pythonInterpreterPath; // Path to the Python interpreter
                start.Arguments = $"{scriptPath} {query}";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;

                // Start the process
                using (Process process = Process.Start(start))
                {
                    // Read the output and error streams
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Wait for the process to exit
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"Python script execution error: {error}");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }
    }
}
