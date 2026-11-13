using System.Diagnostics;

namespace Variant10.Services;

public sealed class ValidatorService
{
    public async Task<bool> ValidateInvoiceAsync(string filePath)
    {

        var validatorPath = "validator.bat";
        var validatorContent = @"@echo off
                            setlocal
                            set FILE=%1
                            if not exist ""%FILE%"" (
                            echo ERROR: File not found
                            exit /b 1
                            )
                            set /p FIRST_LINE=<""%FILE%""
                            echo %FIRST_LINE% | findstr /b ""INV-"" >nul
                            if %errorlevel% neq 0 (
                            echo ERROR: Invalid invoice format
                            exit /b 1
                            )
                            echo OK: Invoice is valid
                            exit /b 0
                            ";
        await File.WriteAllTextAsync(validatorPath, validatorContent);

        try
        {

            var startInfo = new ProcessStartInfo
            {
                FileName = validatorPath,
                Arguments = $"\"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,    
                CreateNoWindow = true      
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            Console.WriteLine($"  -> Валидатор: {output.Trim()}");
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"  -> Ошибки валидатора: {error.Trim()}");

            return process.ExitCode == 0;
        }
        finally
        {
            if (File.Exists(validatorPath))
                File.Delete(validatorPath);
        }
    }
}