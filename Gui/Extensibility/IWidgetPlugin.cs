using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MatterHackers.Agg.UI;

namespace MatterHackers.Agg.Extensibility
{
	public interface IWidgetPlugin : IApplicationPlugin
	{
		void Initialize(GuiWidget application);
	}
}
