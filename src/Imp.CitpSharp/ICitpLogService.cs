using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Imp.CitpSharp
{
	/// <summary>
	///     Level of messages reported to the ICitpLogService implementation
	/// </summary>
	[PublicAPI]
	public enum CitpLoggerLevel
	{
		/// <summary>
		///     Log messages only of interest when debugging
		/// </summary>
		Debug = 0,

		/// <summary>
		///     Log messages containing runtime event information
		/// </summary>
		Info = 1,

		/// <summary>
		///     Log messages containing warnings which are not critical
		/// </summary>
		Warning = 2,

		/// <summary>
		///     Log messages detailing critical errors
		/// </summary>
		Error = 3,

		/// <summary>
		///     Logging disabled
		/// </summary>
		Disabled = 4
	}



	/// <summary>
	///     Interface to be implemented to provide access to the hosts logging framework
	/// </summary>
	[PublicAPI]
	public interface ICitpLogService
	{
		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Debug" /> level
		/// </summary>
		/// <param name="message"></param>
		void LogDebug([NotNull] string message);

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Info" /> level
		/// </summary>
		/// <param name="message"></param>
		void LogInfo([NotNull] string message);

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Warning" /> level
		/// </summary>
		/// <param name="message"></param>
		void LogWarning([NotNull] string message);

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Error" /> level
		/// </summary>
		/// <param name="message"></param>
		void LogError([NotNull] string message);

		/// <summary>
		///     Log details of an exception
		/// </summary>
		/// <param name="ex"></param>
		void LogException([NotNull] Exception ex);
	}



	/// <summary>
	///		Implementation of <see cref="ICitpLogService"/> which logs to <see cref="Debug"/> and/or <see cref="Console"/>
	/// </summary>
	[PublicAPI]
	public class CitpDebugLogger : ICitpLogService
	{
		private readonly CitpLoggerLevel _logLevel;

		private readonly bool _isWriteToDebug;
		private readonly bool _isWriteToConsole;

		/// <summary>
		///		Constructs <see cref="CitpDebugLogger"/>
		/// </summary>
		/// <param name="logLevel">Maximum level at which to log messages</param>
		/// <param name="isWriteToDebug"></param>
		/// <param name="isWriteToConsole"></param>
		public CitpDebugLogger(CitpLoggerLevel logLevel, bool isWriteToDebug = true, bool isWriteToConsole = false)
		{
			_logLevel = logLevel;
			_isWriteToDebug = isWriteToDebug;
			_isWriteToConsole = isWriteToConsole;
		}

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Debug" /> level
		/// </summary>
		/// <param name="message"></param>
		public void LogDebug(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Debug)
			{
				writeToDebug(CitpLoggerLevel.Debug, message);
				writeToConsole(CitpLoggerLevel.Debug, message);
			}
		}

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Info" /> level
		/// </summary>
		/// <param name="message"></param>
		public void LogInfo(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Info)
			{
				writeToDebug(CitpLoggerLevel.Info, message);
				writeToConsole(CitpLoggerLevel.Info, message);
			}
		}

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Warning" /> level
		/// </summary>
		/// <param name="message"></param>
		public void LogWarning(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Warning)
			{
				writeToDebug(CitpLoggerLevel.Warning, message);
				writeToConsole(CitpLoggerLevel.Warning, message);
			}
		}

		/// <summary>
		///     Log message at <see cref="CitpLoggerLevel.Error" /> level
		/// </summary>
		/// <param name="message"></param>
		public void LogError(string message)
		{
			if (_logLevel <= CitpLoggerLevel.Error)
			{
				writeToDebug(CitpLoggerLevel.Error, message);
				writeToConsole(CitpLoggerLevel.Error, message);
			}
		}

		/// <summary>
		///     Log details of an exception
		/// </summary>
		/// <param name="ex"></param>
		public void LogException(Exception ex)
		{
			if (_logLevel <= CitpLoggerLevel.Error)
			{
				writeToDebug(CitpLoggerLevel.Error, ex.ToString());
				writeToConsole(CitpLoggerLevel.Error, ex.ToString());
			}
		}

		private void writeToDebug(CitpLoggerLevel level, string message)
		{
			if (_isWriteToDebug)
				Debug.WriteLine($"CitpSharp ({level}): {message}");
		}

		private void writeToConsole(CitpLoggerLevel level, string message)
		{
			if (_isWriteToConsole)
				Console.WriteLine($"{DateTime.Now} CitpSharp {level}: {message}");
		}
	}
}