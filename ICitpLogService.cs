//  This file is part of CitpSharp.
//
//  CitpSharp is free software: you can redistribute it and/or modify
//	it under the terms of the GNU Lesser General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	CitpSharp is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU Lesser General Public License for more details.

//	You should have received a copy of the GNU Lesser General Public License
//	along with CitpSharp.  If not, see <http://www.gnu.org/licenses/>.

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
			Console.WriteLine("CitpSharp ({0}): {1}", level, message);
		}
	}
}