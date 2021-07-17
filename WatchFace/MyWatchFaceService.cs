using System;
using System.Threading;
using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Text.Format;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Wearable.Watchface;
using Android.Service.Wallpaper;
using Java.Util.Concurrent;

namespace WatchFace
{
    public class MyWatchFaceService : CanvasWatchFaceService
    {
        // Used for logging:
        const String Tag = "MyWatchFaceService";

        // Must be implemented to return a new instance of the wallpaper's engine:
        public override WallpaperService.Engine OnCreateEngine () 
        {
            return new MyWatchFaceEngine (this);
        }

        public class MyWatchFaceEngine : CanvasWatchFaceService.Engine 
        {
            // Update every second:
           // static long InterActiveUpdateRateMs = TimeUnit.Seconds.ToMillis (1);

            CanvasWatchFaceService owner;

            Paint timePaint;
            Paint datePaint;
            Paint TopRect;
            Paint BotRect;

            Shader TopShader;
            Shader BotShader;

            Color TopStartColor;
            Color TopEndColor;

            public Time time;
            DateTime timedate;

            // Broadcast receiver for handling time zone changes:
            TimeZoneReceiver timeZoneReceiver;

            bool lowBitAmbient;

            // Saves a reference to the outer CanvasWatchFaceService
            public MyWatchFaceEngine (CanvasWatchFaceService owner) : base(owner)
            {
                this.owner = owner;
            }
 
            public override void OnCreate (ISurfaceHolder holder) 
            {
                base.OnCreate(holder);

                // Configure the system UI. Instantiates a WatchFaceStyle object that causes 
                // notifications to appear as small peek cards that are shown only briefly 
                // when interruptive. Also disables the system-style UI time from being drawn:

                SetWatchFaceStyle (new WatchFaceStyle.Builder (owner)
                    .SetCardPeekMode (WatchFaceStyle.PeekModeShort)
                    .SetBackgroundVisibility (WatchFaceStyle.BackgroundVisibilityInterruptive)
                    .SetShowSystemUiTime (false)
                    .Build ());

                // Time text
                timePaint = new Paint();
                timePaint.SetARGB(255, 255,255,255);
                timePaint.StrokeWidth = 2.0f;
                timePaint.AntiAlias = true;
                timePaint.TextSize = 70;
                timePaint.TextAlign = Paint.Align.Center;

                // Date text
                datePaint = new Paint();
                datePaint.SetARGB(255, 255, 255, 255);
                datePaint.StrokeWidth = 2.0f;
                datePaint.AntiAlias = true;
                datePaint.TextSize = 20;
                datePaint.TextAlign = Paint.Align.Center;


                // Top Gradient
                TopStartColor = Color.Argb(255, 173, 83, 137);
                TopEndColor =  Color.Argb(255, 60, 16, 83);

                // Top rect
                TopRect = new Paint();
                timePaint.AntiAlias = true;

                // Bot rect
                BotRect = new Paint();
                timePaint.AntiAlias = true;

                // Instantiate the time object:
                //time = new Time();
            }

            // Called when the properties of the Wear device are determined, specifically 
            // low bit ambient mode (the screen supports fewer bits for each color in
            // ambient mode):
            public override void OnPropertiesChanged(Bundle properties) 
            {
                base.OnPropertiesChanged (properties);

                lowBitAmbient = properties.GetBoolean (MyWatchFaceService.PropertyLowBitAmbient);

                if (Log.IsLoggable (Tag, LogPriority.Debug))
                    Log.Debug (Tag, "OnPropertiesChanged: low-bit ambient = " + lowBitAmbient);
            }

            // Called periodically to update the time shown by the watch face: at least 
            // once per minute in ambient and interactive modes, and whenever the date, 
            // time, or timezone has changed:
            public override void OnTimeTick ()
            {
                base.OnTimeTick ();

                if (Log.IsLoggable (Tag, LogPriority.Debug))
                    Log.Debug (Tag, "onTimeTick: ambient = " + IsInAmbientMode);
                
                Invalidate ();
            }

            // Called when the device enters or exits ambient mode. In ambient mode,
            // the watch face disables anti-aliasing while drawing.
            public override void OnAmbientModeChanged (bool inAmbientMode) 
            {
                base.OnAmbientModeChanged (inAmbientMode);

                if (Log.IsLoggable (Tag, LogPriority.Debug))
                    Log.Debug (Tag, "OnAmbientMode");
                
                if (lowBitAmbient)
                {
                   bool antiAlias = !inAmbientMode;
                   timePaint.AntiAlias = antiAlias;
                   datePaint.AntiAlias = antiAlias;
                   TopRect.AntiAlias = antiAlias;
                   BotRect.AntiAlias = antiAlias;

                }

                Invalidate ();
            }

            public override void OnDraw (Canvas canvas, Rect bounds)
            {
                timedate = DateTime.Now;

                int hour = timedate.Hour;
                int minute = timedate.Minute; 

                string hourtext = timedate.ToString("HH");
                string minutetext = timedate.ToString("mm");

                string monthtext = timedate.ToString("MMM");
                string daytext = timedate.ToString("dd");

                int width = bounds.Width ();
                int height = bounds.Height ();

                float centerX = width / 2.0f;
                float centerY = height / 2.0f;

                float hourX = GetX(hour, 24, width);
                float minuteX = GetX(minute, 60, width);

                canvas.DrawColor (Color.Black);

                TopShader = new LinearGradient(0, centerY / 2, width, centerY / 2, TopStartColor, TopEndColor, Shader.TileMode.Clamp);
                BotShader = new LinearGradient(0, height - (centerY / 2),width, height - (centerY / 2), TopStartColor, TopEndColor, Shader.TileMode.Clamp);
                TopRect.SetShader(TopShader);
                BotRect.SetShader(BotShader);
                canvas.DrawRect(0, 0,hourX, centerY, TopRect);
                canvas.DrawRect(0,centerY, minuteX, height, BotRect);


                canvas.DrawText(monthtext, centerX / 2, (centerY /2) - 1f, datePaint);
                canvas.DrawText(daytext, centerX / 2, (centerY / 2) + datePaint.TextSize + 1f, datePaint);

                canvas.DrawText(hourtext, centerX, centerY - 3f, timePaint);
                canvas.DrawText(minutetext, centerX, centerY+timePaint.TextSize, timePaint);


            }

            private float GetX(float val, float max, float width)
            {
                float proc = (val / max) * 100;
                float ret = (width / 100) * proc;
                return ret;
            }

            private float GetY(float val, float max, float height)
            {
                float proc = (val / max) * 100;
                float ret = (height / 100) * proc;
                return ret;
            }

            // Called whenever the watch face is becoming visible or hidden. Note that
            // you must call base.OnVisibilityChanged first:
            public override void OnVisibilityChanged (bool visible)
            {
                base.OnVisibilityChanged (visible);

                if (Log.IsLoggable (Tag, LogPriority.Debug))
                    Log.Debug (Tag, "OnVisibilityChanged: " + visible);
                
                // If the watch face became visible, register the timezone receiver
                // and get the current time. Else, unregister the timezone receiver:

                //if (visible)
                //{
                //    RegisterTimezoneReceiver ();
                //    time.Clear(Java.Util.TimeZone.Default.ID);
                //    time.SetToNow();
                //}
                //else
                //    UnregisterTimezoneReceiver ();
            }

            // Run the timer only when visible and in interactive mode:
            //bool ShouldTimerBeRunning() 
            //{
            //    return IsVisible && !IsInAmbientMode;
            //}

            //bool registeredTimezoneReceiver = false;

            // Registers the time zone broadcast receiver (defined at the end of 
            // this file) to handle time zone change events:

            //void RegisterTimezoneReceiver()
            //{
            //    if (registeredTimezoneReceiver)
            //        return;
            //    else
            //    {
            //        if (timeZoneReceiver == null)
            //        {
            //            timeZoneReceiver = new TimeZoneReceiver ();
            //            timeZoneReceiver.Receive = (intent) => {
            //                time.Clear (intent.GetStringExtra ("time-zone"));
            //                time.SetToNow ();
            //            };
            //        }
            //        registeredTimezoneReceiver = true;
            //        IntentFilter filter = new IntentFilter(Intent.ActionTimezoneChanged);
            //        Application.Context.RegisterReceiver (timeZoneReceiver, filter);
            //    }
            //}

            // Unregisters the timezone Broadcast receiver:

            //void UnregisterTimezoneReceiver() 
            //{
            //    if (!registeredTimezoneReceiver)
            //        return;
            //    registeredTimezoneReceiver = false;
            //    Application.Context.UnregisterReceiver (timeZoneReceiver);
            //}
        }
    }

    // Time zone broadcast receiver. OnReceive is called when the
    // time zone changes:

    public class TimeZoneReceiver: BroadcastReceiver 
    {
        public Action<Intent> Receive { get; set; }

        public override void OnReceive (Context context, Intent intent)
        {
            //if (Receive != null)
            //    Receive (intent);
        }
    }
}
