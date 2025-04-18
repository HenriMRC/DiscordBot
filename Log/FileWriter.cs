﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace DiscordBot.Log;

internal class FileWriter : ILogWriter
{
    private const int FILE_MAX_SIZE = 1024 * 1024; //1MB
    private const int MAX_FILES = 20; //1MB
    private Stream? _stream;

    internal FileWriter()
    {
        AppDomain.CurrentDomain.ProcessExit += Dispose;
    }

    public void Write(Logger.Message logMessage)
    {
        string message = $"{logMessage}\n";
        byte[] buffer = Encoding.Default.GetBytes(message);

        if (buffer.Length < FILE_MAX_SIZE)
        {
            if (_stream == null)
                CreateFile(logMessage.Timestamp);
            else if (buffer.Length + _stream.Position > FILE_MAX_SIZE)
            {
                _stream.Flush();
                _stream.Dispose();

                CreateFile(logMessage.Timestamp);
            }
        }
        else
        {
            if (_stream != null)
            {
                _stream.Flush();
                _stream.Dispose();
            }

            CreateFile(logMessage.Timestamp);
        }

        _stream.Write(buffer, 0, buffer.Length);
    }

    [MemberNotNull(nameof(_stream))]
    private void CreateFile(DateTime timestamp)
    {
        FileInfo fileInfo = new($"./Logs/{timestamp:yyyy-MM-dd-HH-mm-ss-fff}.txt");
        DirectoryInfo directory = fileInfo.Directory!;

        Directory.CreateDirectory(directory!.FullName);
        _stream = fileInfo.Create();

        FileInfo[] files = [.. directory.EnumerateFiles().OrderByDescending(f => f.LastWriteTime)];
        for (int i = MAX_FILES; i < files.Length; i++)
            files[i].Delete();
    }

    private void Dispose(object? sender, EventArgs e)
    {
        AppDomain.CurrentDomain.ProcessExit -= Dispose;

        if (_stream != null)
        {
            _stream.Flush();
            _stream.Dispose();
        }
    }

    ~FileWriter()
    {
        Dispose(this, EventArgs.Empty);
    }
}
