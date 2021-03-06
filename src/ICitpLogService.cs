﻿using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	/// Level of messages reported to the ICitpLogService implementation
	/// </summary>
	[PublicAPI]
	public enum CitpLoggerLevel
	{
		Debug = 0,
		Info = 1,
		Warning = 2,
		Error = 3,
		Disabled = 4
	}


	/// <summary>
	/// Interface to be implemented to provide access to the hosts logging framework
	/// </summary>
	[PublicAPI]
	public interface ICitpLogService
	{
		void LogDebug([NotNull] string message);
		void LogInfo([NotNull] string message);
		void LogWarning([NotNull] string message);
		void LogError([NotNull] string message);
		void LogException([NotNull] Exception ex);
	}



	internal class CitpDebugLogger : ICitpLogService
	{
		private readonly CitpLoggerLevel _logLevel;

		public CitpDebugLogger(CitpLoggerLevel logLevel)
		{
			_logLevel = logLevel;
		}

		public void LogDebug(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Debug)
				writeToDebug(CitpLoggerLevel.Debug, message);
		}

		public void LogInfo(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Info)
				writeToDebug(CitpLoggerLevel.Info, message);
		}

		public void LogWarning(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Warning)
				writeToDebug(CitpLoggerLevel.Warning, message);
		}

		public void LogError(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Error)
				writeToDebug(CitpLoggerLevel.Error, message);
		}

		public void LogException(Exception ex)
		{
			if (_logLevel <= CitpLoggerLevel.Error)
				writeToDebug(CitpLoggerLevel.Error, ex.ToString());
		}

		private void writeToDebug(CitpLoggerLevel level, string message)
		{
			Debug.WriteLine("CitpSharp ({0}): {1}", level, message);
		}
	}
}