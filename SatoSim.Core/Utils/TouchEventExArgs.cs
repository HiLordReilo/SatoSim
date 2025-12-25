using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;

namespace SatoSim.Core.Utils
{
    public class TouchEventExArgs : TouchEventArgs
    {
        public TouchLocation LastRawTouchLocation { get; }
        public Vector2 LastPosition => LastRawTouchLocation.Position;
        public Vector2 DistanceMoved => RawTouchLocation.Position - LastRawTouchLocation.Position;
        
        public TouchEventExArgs(ViewportAdapter viewportAdapter, TimeSpan time, TouchLocation location, TouchLocation lastLocation) : base(viewportAdapter, time, location)
        {
            LastRawTouchLocation = lastLocation;
        }
    }
}