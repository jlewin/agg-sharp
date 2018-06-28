/*
Copyright (c) 2016, Kevin Pope, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using Newtonsoft.Json;

namespace MatterHackers.Agg.UI
{
	public class ThemeColors: IThemeColors
	{
		public bool IsDarkTheme { get; set; }

		public string Name { get; set; }

		public Color Transparent { get; set; } = new Color(0, 0, 0, 0);

		public Color SecondaryTextColor { get; set; }

		public Color PrimaryBackgroundColor { get; set; }

		public Color SecondaryBackgroundColor { get; set; }

		public Color TertiaryBackgroundColor { get; set; }

		public Color PrimaryTextColor { get; set; }

		public Color PrimaryAccentColor { get; set; }

		public Color SourceColor { get; private set; }

		public static ThemeColors Create(string name, Color accentColor, bool darkTheme = true)
		{
			var primaryBackgroundColor = new Color(darkTheme ? "#444" : "#D0D0D0");

			return new ThemeColors
			{
				IsDarkTheme = darkTheme,
				Name = name,
				SourceColor = accentColor,
				PrimaryBackgroundColor = primaryBackgroundColor,
				SecondaryBackgroundColor = new Color(darkTheme ? "#333" : "#B9B9B9"),
				TertiaryBackgroundColor = new Color(darkTheme ? "#3E3E3E" : "#BEBEBE"),
				PrimaryTextColor = new Color(darkTheme ? "#FFFFFF" : "#222"),
				SecondaryTextColor = new Color(darkTheme ? "#C8C8C8" : "#333"),

				PrimaryAccentColor = GetAdjustedAccentColor(accentColor, primaryBackgroundColor)
			};
		}

		public static Color GetAdjustedAccentColor(Color accentColor, Color backgroundColor)
		{
			return accentColor.AdjustContrast(backgroundColor).ToColor();
		}
	}
}