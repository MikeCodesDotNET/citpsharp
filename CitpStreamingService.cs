using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imp.CitpSharp
{
	internal sealed class CitpStreamingService : IDisposable
	{
		readonly ICitpLogService _log;
		readonly ICitpMediaServerInfo _serverInfo;


		public CitpStreamingService(ICitpLogService log, ICitpMediaServerInfo serverInfo)
		{
			_log = log;
			_serverInfo = serverInfo;
		}

		public void Start()
		{

		}

		public void Dispose()
		{

		}
	}
}
