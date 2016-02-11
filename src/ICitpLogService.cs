using System;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	[PublicAPI]
	public enum CitpLoggerLevel
	{
		Debug = 0,
		Info = 1,
		Warning = 2,
		Error = 3,
		Disabled = 4
	}



	[PublicAPI]
	public interface ICitpLogService
	{
		void LogDebug(string message);
		void LogInfo(string message);
		void LogWarning(string message);
		void LogError(string message);
		void LogException(Exception ex);
	}



	internal class CitpConsoleLogger : ICitpLogService
	{
		private readonly CitpLoggerLevel _logLevel;

		public CitpConsoleLogger(CitpLoggerLevel logLevel)
		{
			_logLevel = logLevel;
		}

		public void LogDebug(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Debug)
				writeToConsole(CitpLoggerLevel.Debug, message);
		}

		public void LogInfo(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Info)
				writeToConsole(CitpLoggerLevel.Info, message);
		}

		public void LogWarning(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Warning)
				writeToConsole(CitpLoggerLevel.Warning, message);
		}

		public void LogError(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Error)
				writeToConsole(CitpLoggerLevel.Error, message);
		}

		public void LogException(Exception ex)
		{
			if (_logLevel <= CitpLoggerLevel.Error)
				writeToConsole(CitpLoggerLevel.Error, ex.ToString());
		}

		private void writeToConsole(CitpLoggerLevel level, string message)
		{
			//Console.WriteLine("CitpSharp ({0}): {1}", level, message);
		}
	}
}