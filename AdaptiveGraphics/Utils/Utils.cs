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

        public static int GetMedian(List<int> array)
        {
            if (array == null || array.Count == 0)
            {
                throw new ArgumentException("Cannot compute the median of an empty array");
        }

            var sortedArray = array.OrderBy(x => x).ToArray();
            int count = sortedArray.Length;
            int mid = count / 2;

            if (count % 2 == 1)
        {
                return sortedArray[mid];
            }
            return (sortedArray[mid - 1] + sortedArray[mid]) / 2;
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
            if (!AdaptiveGraphicsModSystem.Config.DebugLogs) return;
#if DEBUG
            AdaptiveGraphicsModSystem.Logger.Debug("[Adaptive Graphics] " + msg);
        }
#endif
#if RELEASE // FOR TESTING PURPOSES
            AdaptiveGraphicsModSystem.Logger.Notification("[Adaptive Graphics Debug] " + msg);
        }
#endif
    }
}
