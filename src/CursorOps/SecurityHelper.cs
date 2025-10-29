namespace CursorOps;

using System.Security;

/// <summary>
/// Provides security utilities for path validation and sanitization.
/// Prevents path traversal attacks and ensures file operations stay within allowed boundaries.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Maximum path length to prevent buffer overflow issues.
    /// </summary>
    private const int MAX_PATH_LENGTH = 32767; // Windows MAX_PATH extended length

    /// <summary>
    /// Characters that are invalid in file names and paths.
    /// </summary>
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars()
        .Concat(new[] { '\0' }) // Add null byte explicitly
        .ToArray();

    /// <summary>
    /// Characters that are invalid in file names only.
    /// </summary>
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars()
        .Concat(new[] { '\0' }) // Add null byte explicitly
        .ToArray();

    /// <summary>
    /// Validates and canonicalizes an output path to prevent path traversal attacks.
    /// </summary>
    /// <param name="outputPath">The user-provided output path to validate.</param>
    /// <param name="baseDirectory">The base directory that output must be within.</param>
    /// <returns>The canonicalized, validated absolute path.</returns>
    /// <exception cref="SecurityException">Thrown when path validation fails.</exception>
    /// <exception cref="ArgumentNullException">Thrown when outputPath is null or empty.</exception>
    public static string ValidateOutputPath(string? outputPath, string baseDirectory)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentNullException(nameof(outputPath), "Output path cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentNullException(nameof(baseDirectory), "Base directory cannot be null or empty");
        }

        // Check for null bytes (path injection attack)
        if (outputPath.Contains('\0'))
        {
            throw new SecurityException("Path contains null bytes");
        }

        // Check for invalid path characters
        if (outputPath.IndexOfAny(InvalidPathChars) >= 0)
        {
            throw new SecurityException("Path contains invalid characters");
        }

        // Check path length
        if (outputPath.Length > MAX_PATH_LENGTH)
        {
            throw new SecurityException($"Path exceeds maximum length of {MAX_PATH_LENGTH} characters");
        }

        // Resolve to absolute path
        string absolutePath;
        try
        {
            // If path is relative, combine with base directory
            if (!Path.IsPathRooted(outputPath))
            {
                absolutePath = Path.Combine(baseDirectory, outputPath);
            }
            else
            {
                absolutePath = outputPath;
            }

            // Canonicalize the path (resolve .. and . and symbolic links)
            absolutePath = Path.GetFullPath(absolutePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new SecurityException($"Invalid path format: {ex.Message}", ex);
        }

        // Canonicalize base directory for comparison
        string canonicalBase;
        try
        {
            canonicalBase = Path.GetFullPath(baseDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new SecurityException($"Invalid base directory format: {ex.Message}", ex);
        }

        // Verify the resolved path is within the base directory
        if (!IsPathSafe(absolutePath, canonicalBase))
        {
            throw new SecurityException(
                $"Path traversal detected: '{outputPath}' resolves outside allowed directory '{baseDirectory}'");
        }

        return absolutePath;
    }

    /// <summary>
    /// Validates an input name (like prompt name) to ensure it doesn't contain path separators
    /// or other characters that could be used for path traversal.
    /// </summary>
    /// <param name="name">The name to validate (e.g., prompt name, file name).</param>
    /// <returns>The validated name.</returns>
    /// <exception cref="SecurityException">Thrown when name contains invalid characters.</exception>
    /// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
    public static string ValidateInputName(string? name)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "Name cannot be null or empty");
        }

        // Check for null bytes
        if (name.Contains('\0'))
        {
            throw new SecurityException("Name contains null bytes");
        }

        // Check for path separators (prevent path traversal)
        if (name.Contains(Path.DirectorySeparatorChar) ||
            name.Contains(Path.AltDirectorySeparatorChar) ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            throw new SecurityException(
                "Name cannot contain path separators (/, \\). Use only the file name without path.");
        }

        // Check for invalid file name characters
        if (name.IndexOfAny(InvalidFileNameChars) >= 0)
        {
            throw new SecurityException("Name contains invalid file name characters");
        }

        // Check for path traversal sequences
        if (name.Contains("..") || name.Contains("./") || name.Contains(".\\"))
        {
            throw new SecurityException("Name contains path traversal sequences");
        }

        // Check for reserved Windows names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)
        string upperName = name.ToUpperInvariant();
        string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
                                   "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2",
                                   "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        // Check if name (without extension) is a reserved name
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(upperName);
        if (reservedNames.Contains(nameWithoutExtension))
        {
            throw new SecurityException($"Name '{name}' is a reserved Windows device name");
        }

        // Check name length (Windows has 255 character limit for file names)
        if (name.Length > 255)
        {
            throw new SecurityException("Name exceeds maximum file name length of 255 characters");
        }

        return name;
    }

    /// <summary>
    /// Checks if a path is within an allowed directory (path is safe).
    /// This prevents path traversal attacks by ensuring the canonical path
    /// starts with the canonical allowed directory path.
    /// </summary>
    /// <param name="path">The path to check (should be absolute).</param>
    /// <param name="allowedDirectory">The directory that the path must be within (should be absolute).</param>
    /// <returns>True if the path is safe (within allowed directory), false otherwise.</returns>
    public static bool IsPathSafe(string path, string allowedDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(allowedDirectory))
        {
            return false;
        }

        try
        {
            // Canonicalize both paths
            string canonicalPath = Path.GetFullPath(path);
            string canonicalAllowed = Path.GetFullPath(allowedDirectory);

            // Ensure paths end with directory separator for comparison
            // This prevents false positives like:
            // canonicalPath = "C:\foo\bar" matching allowedDirectory = "C:\foo\ba"
            if (!canonicalAllowed.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                canonicalAllowed += Path.DirectorySeparatorChar;
            }

            // Check if the canonical path starts with the canonical allowed directory
            // Use case-insensitive comparison on Windows
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            StringComparison comparison = isWindows
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return canonicalPath.StartsWith(canonicalAllowed, comparison);
        }
        catch (Exception ex) when (ex is ArgumentException or
                                       NotSupportedException or
                                       PathTooLongException or
                                       UnauthorizedAccessException)
        {
            // If we can't canonicalize the paths, it's not safe
            return false;
        }
    }

    /// <summary>
    /// Creates the directory for a file path if it doesn't exist, with security validation.
    /// </summary>
    /// <param name="filePath">The file path whose directory should be created.</param>
    /// <param name="baseDirectory">The base directory that must contain the file.</param>
    /// <exception cref="SecurityException">Thrown if directory creation would be outside allowed base.</exception>
    /// <exception cref="IOException">Thrown if directory creation fails.</exception>
    public static void EnsureDirectoryExists(string filePath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                // Validate that the directory is within allowed base
                string canonicalDir = Path.GetFullPath(directory);
                if (!IsPathSafe(canonicalDir, baseDirectory))
                {
                    throw new SecurityException(
                        $"Cannot create directory outside allowed base: {directory}");
                }

                Directory.CreateDirectory(directory);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw new IOException($"Failed to create directory for '{filePath}': {ex.Message}", ex);
        }
    }
}
