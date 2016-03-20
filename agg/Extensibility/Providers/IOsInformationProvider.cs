using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatterHackers.Agg.PlatformAbstract
{
	public interface IOsInformationProvider
	{
		OSType OperatingSystem { get; }
	}
}
