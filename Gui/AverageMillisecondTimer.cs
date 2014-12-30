﻿/*
Copyright (c) 2014, Lars Brubaker
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatterHackers.Agg.UI
{
    public class AverageMillisecondTimer
    {
        static readonly int averageCount = 64;
        int averageIndex = 0;
        int[] averageMsArray = new int[averageCount];
        int totalMsInArray = 0;

        public AverageMillisecondTimer()
        {
        }

        public void Update(int elapsedMillisendsForLastDraw)
        {
            totalMsInArray -= averageMsArray[averageIndex % averageCount];
            averageMsArray[averageIndex % averageCount] = elapsedMillisendsForLastDraw;
            totalMsInArray += averageMsArray[averageIndex % averageCount];
            averageIndex++;
        }

        public int GetAverage()
        {
            return totalMsInArray / averageCount;
        }

        public void DrawTopCenter(GuiWidget widgetToDrawOn, Graphics2D graphics2D)
        {
            graphics2D.DrawString("{0}ms".FormatWith(GetAverage()), widgetToDrawOn.Width / 2 - 15, widgetToDrawOn.Height - 40, 16, color: RGBA_Bytes.White, drawFromHintedCach: true);
        }
    }
}
