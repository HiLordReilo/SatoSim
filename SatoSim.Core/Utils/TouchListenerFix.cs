using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Extended.Input.InputListeners;

namespace SatoSim.Core.Utils
{
    // This class is a modified version of MonoGame.Extended.InputListeners.TouchListener
    public class TouchListenerFix : InputListener
    {
        public event EventHandler<TouchEventArgs> TouchStarted;

        public event EventHandler<TouchEventArgs> TouchEnded;

        public event EventHandler<TouchEventExArgs> TouchMoved;

        public event EventHandler<TouchEventArgs> TouchCancelled;

        public TouchCollection LastState { get; private set; }
        public TouchCollection CurrentState { get; private set; }

        public TouchListenerFix()
        {
            LastState = TouchPanel.GetState();
            CurrentState = TouchPanel.GetState();
        }
        
        public override void Update(GameTime gameTime)
        {
            CurrentState = TouchPanel.GetState();
            
            foreach (TouchLocation location in CurrentState)
            {
                location.TryGetPreviousLocation(out var prev);
                
                switch (location.State)
                {
                    case TouchLocationState.Invalid:
                        EventHandler<TouchEventArgs> touchCancelled = this.TouchCancelled;
                        if (touchCancelled != null)
                        {
                            touchCancelled(this, new TouchEventArgs(null, gameTime.TotalGameTime, location));
                        }
                        continue;
                    case TouchLocationState.Moved: // Works
                        // [FIX]
                        // MonoGame's implementation of touch states is broken, due to which location state from
                        // TouchPanel.GetState() would never be Pressed, but it would be on its previous state
                        // We check if previous' state is Pressed, and if so, call TouchStarted event
                        if (prev.State == TouchLocationState.Pressed)
                        {
                            EventHandler<TouchEventArgs> touchStartedFix = this.TouchStarted;
                            if (touchStartedFix != null)
                            {
                                touchStartedFix(this, new TouchEventArgs(null, gameTime.TotalGameTime, location));
                                continue;
                            }
                        }
                        
                        // The touch is actually stationary, skip processing
                        if(prev.Position == location.Position) continue;
                        // [END OF FIX]
                        
                        EventHandler<TouchEventExArgs> touchMoved = this.TouchMoved;
                        if (touchMoved != null)
                        {
                            // [FIX]
                            // Use extended implementation of touch event args for ease of use
                            touchMoved(this, new TouchEventExArgs(null, gameTime.TotalGameTime, location, prev));
                            // [END OF FIX]
                        }
                        continue;
                    case TouchLocationState.Pressed:
                        EventHandler<TouchEventArgs> touchStarted = this.TouchStarted;
                        if (touchStarted != null)
                        {
                            touchStarted(this, new TouchEventArgs(null, gameTime.TotalGameTime, location));
                        }
                        continue;
                    case TouchLocationState.Released: // Works
                        EventHandler<TouchEventArgs> touchEnded = this.TouchEnded;
                        if (touchEnded != null)
                        {
                            touchEnded(this, new TouchEventArgs(null, gameTime.TotalGameTime, location));
                        }
                        continue;
                    default:
                        continue;
                }
            }

            LastState = CurrentState;
        }
    }
}