using System;
using System.Collections.Generic;

namespace AdaptiveGraphics
{
    public static class Utils
    {
        /// <summary>
        /// Adds a new item to the queue and ensures the queue size does not exceed the specified limit.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="sample">Item to be added.</param>
        /// <param name="size">Size of the queue is limited to.</param>
        public static void AddSample<T>(this Queue<T> queue, T sample, int size)
        {
            var s = size - 1;
            while (queue.Count > s)
            {
                queue.Dequeue();
            }
            queue.Enqueue(sample);
        }

        public static (int lower, int upper) GetBounds(int value, float tolerance)
        {
            int tol = (int)(value * tolerance);
            return GetBounds(value, tol);
        }

        public static (int outlierMin, int outlierMax) GetBounds(int value, int tolerance)
        {
            return (value - tolerance, value + tolerance);
        }

        public static bool IsWithin<T>(this T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        public static int GetFps(float deltaTime) => (int)(GetFps(1, deltaTime));

        public static int GetFps(int frameCount, float deltaTime) => (int)(1.0f * frameCount / deltaTime);

        public static void LogNotification(string msg)
        {
            AdaptiveGraphicsModSystem.Logger.Notification("[Adaptive Graphics] " + msg);
        }

        public static void LogWarning(string msg)
        {
            AdaptiveGraphicsModSystem.Logger.Warning("[Adaptive Graphics] " + msg);
        }

        public static void LogError(string msg)
        {
            AdaptiveGraphicsModSystem.Logger.Error("[Adaptive Graphics] " + msg);
        }

        public static void LogDebug(string msg)
        {
#if DEBUG
            AdaptiveGraphicsModSystem.Logger.Debug("[Adaptive Graphics] " + msg);
            return;
        }
#endif
#if RELEASE // FOR TESTING PURPOSES
            AdaptiveGraphicsModSystem.Logger.Notification("[Adaptive Graphics Debug] " + msg);
            return; }
#endif
    }
}
