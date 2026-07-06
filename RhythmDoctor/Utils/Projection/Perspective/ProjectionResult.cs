using RhythmBase.Global.Components.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Perspective
{
    public readonly struct ProjectionResult
    {
        /// <summary>屏幕坐标（像素单位，左上0,0 右下 PixelateWidth×PixelateHeight）</summary>
        public readonly PointN ScreenPoint;
        public readonly float Distance;
        public readonly bool InFront;

        public ProjectionResult(PointN screen, float dist, bool inFront)
        {
            ScreenPoint = screen;
            Distance = dist;
            InFront = inFront;
        }
    }
}
