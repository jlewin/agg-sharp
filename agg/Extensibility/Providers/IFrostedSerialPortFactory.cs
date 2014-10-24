using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatterHackers.Agg.PlatformAbstract
{
	public interface IFrostedSerialPortFactory
	{
		bool IsWindows { get; }

		bool SerialPortAlreadyOpen(string portName);
	

		IFrostedSerialPort Create(string serialPortName);
		IFrostedSerialPort CreateAndOpen(string serialPortName, int baudRate, bool DtrEnableOnConnect);
	}
}
