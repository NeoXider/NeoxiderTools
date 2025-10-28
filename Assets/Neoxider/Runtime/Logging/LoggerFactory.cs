using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace Neo.Runtime.Logging
{
    /// <summary>
    /// Factory for creating configured Serilog logger instances.
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Creates a logger from configuration.
        /// </summary>
        public static ILogger CreateLogger(LoggingConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("LoggingConfig is null, using default logger");
                return CreateDefaultLogger();
            }

            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(config.minimumLevel);

            // Unity Console output
            if (config.enableUnityConsole)
            {
                logConfig.WriteTo.Sink(new UnityConsoleSink(config));
            }

            // File output
            if (config.enableFileLogging)
            {
                string logPath = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    "logs",
                    "game-.log"
                );

                logConfig.WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: config.maxFileSizeMB * 1024 * 1024,
                    retainedFileCountLimit: config.retainedFileCountLimit,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                );
            }

            return logConfig.CreateLogger();
        }

        private static ILogger CreateDefaultLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(new UnityConsoleSink(null))
                .CreateLogger();
        }

        /// <summary>
        /// Simple sink for Unity Console output.
        /// </summary>
        private class UnityConsoleSink : ILogEventSink
        {
            private readonly LoggingConfig _config;
            private static readonly System.Text.RegularExpressions.Regex ClassMethodPattern = 
                new System.Text.RegularExpressions.Regex(@"(\[[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\])", 
                    System.Text.RegularExpressions.RegexOptions.Compiled);

            public UnityConsoleSink(LoggingConfig config)
            {
                _config = config;
            }

            public void Emit(LogEvent logEvent)
            {
                if (logEvent == null) return;

                // Check filtering
                if (_config != null)
                {
                    string sourceContext = logEvent.Properties.ContainsKey("SourceContext")
                        ? logEvent.Properties["SourceContext"].ToString().Trim('"')
                        : null;

                    if (!string.IsNullOrEmpty(sourceContext) && !_config.IsEnabled(sourceContext))
                        return;
                }

                string message = FormatMessage(logEvent);

                switch (logEvent.Level)
                {
                    case LogEventLevel.Verbose:
                    case LogEventLevel.Debug:
                    case LogEventLevel.Information:
                        Debug.Log(message);
                        break;

                    case LogEventLevel.Warning:
                        Debug.LogWarning(message);
                        break;

                    case LogEventLevel.Error:
                    case LogEventLevel.Fatal:
                        if (logEvent.Exception != null)
                            Debug.LogException(logEvent.Exception);
                        else
                            Debug.LogError(message);
                        break;
                }
            }

            private string FormatMessage(LogEvent logEvent)
            {
                string message = logEvent.RenderMessage();
                
                if (_config?.showTimestamps == true)
                {
                    string timestamp = $"[{logEvent.Timestamp:HH:mm:ss}]";
                    if (_config?.enableColors == true)
                        timestamp = $"<color=#888888>{timestamp}</color>";
                    message = $"{timestamp} {message}";
                }

                // Ищем и форматируем паттерн [ClassName.MethodName] в сообщении только если цвета включены
                if (_config?.enableColors == true)
                {
                    message = FormatClassNameMethodPattern(message, logEvent.Level);
                }

                if (_config?.showSourceContext == true && logEvent.Properties.ContainsKey("SourceContext"))
                {
                    string source = logEvent.Properties["SourceContext"].ToString().Trim('"');
                    // Shorten namespace for readability
                    int lastDot = source.LastIndexOf('.');
                    if (lastDot > 0)
                        source = source.Substring(lastDot + 1);
                    
                    if (_config?.enableColors == true)
                    {
                        // Цвета для разных уровней логирования
                        string color = logEvent.Level switch
                        {
                            LogEventLevel.Debug => "#888888",      // Серый
                            LogEventLevel.Information => "#4CAF50", // Зеленый
                            LogEventLevel.Warning => "#FF9800",     // Оранжевый
                            LogEventLevel.Error => "#F44336",       // Красный
                            LogEventLevel.Fatal => "#9C27B0",       // Фиолетовый
                            _ => "#2196F3"                          // Синий по умолчанию
                        };
                        source = $"<color={color}>[{source}]</color>";
                    }
                    else
                    {
                        source = $"[{source}]";
                    }
                    
                    message = $"{source} {message}";
                }

                if (logEvent.Level >= LogEventLevel.Error)
                {
                    string level = logEvent.Level.ToString().ToUpper();
                    if (_config?.enableColors == true)
                    {
                        string color = logEvent.Level switch
                        {
                            LogEventLevel.Error => "#F44336",       // Красный
                            LogEventLevel.Fatal => "#9C27B0",       // Фиолетовый
                            _ => "#FF9800"                          // Оранжевый
                        };
                        level = $"<color={color}>[{level}]</color>";
                    }
                    else
                    {
                        level = $"[{level}]";
                    }
                    message = $"{level} {message}";
                }

                return message;
            }

            private string FormatClassNameMethodPattern(string message, LogEventLevel level)
            {
                // Быстрая проверка - если сообщение не содержит [, то не парсим
                if (string.IsNullOrEmpty(message) || !message.Contains("["))
                    return message;
                
                // Ищем паттерн [ClassName.MethodName] в сообщении используя кэшированный Regex
                var match = ClassMethodPattern.Match(message);
                
                if (match.Success)
                {
                    string fullMatch = match.Groups[1].Value; // [ClassName.MethodName]
                    string content = fullMatch.Substring(1, fullMatch.Length - 2); // ClassName.MethodName
                    
                    string[] parts = content.Split('.');
                    if (parts.Length == 2)
                    {
                        string className = parts[0];
                        string methodName = parts[1];
                        
                        // Цвета для разных уровней логирования
                        string color = level switch
                        {
                            LogEventLevel.Debug => "#888888",      // Серый
                            LogEventLevel.Information => "#4CAF50", // Зеленый
                            LogEventLevel.Warning => "#FF9800",     // Оранжевый
                            LogEventLevel.Error => "#F44336",       // Красный
                            LogEventLevel.Fatal => "#9C27B0",       // Фиолетовый
                            _ => "#2196F3"                          // Синий по умолчанию
                        };
                        
                        string coloredPattern = $"<color={color}>[{className}</color><color=#FFC107>.{methodName}</color>]";
                        message = message.Replace(fullMatch, coloredPattern);
                    }
                }
                
                return message;
            }
        }
    }
}


