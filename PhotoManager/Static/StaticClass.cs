using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace PhotoManager.Static {
    public static class StaticClass {
        /// <summary>
        /// конвертирование IEnumerable в ObservableCollection
        /// </summary>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable) {
            var result = new ObservableCollection<T>();
            foreach (var t in enumerable)
                result.Add(t);
            return result;
        }

        /// <summary>
        /// Возвратить срез из массива
        /// </summary>
        /// <typeparam name="T">Тип массива</typeparam>
        /// <param name="data">Входной массив</param>
        /// <param name="index">Начальный индекс вырезки</param>
        /// <param name="length">Длинна вырезки</param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] data, int index, int length) {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private static bool _semaphore;
        private static readonly object Lockobject = new object();

        /// <summary>
        /// Блокировать поток, пока не будет вызван Release
        /// </summary>
        public static void Lock() {
            lock (Lockobject) {
                while (_semaphore)
                    Thread.Sleep(1);
                _semaphore = true;
            }
        }

        /// <summary>
        /// Разблокировать поток, который был заблокирован Lock-ом
        /// </summary>
        public static void Release() {
            _semaphore = false;
        }

        /// <summary>
        /// Блокировать поток, пока не будет вызван Release
        /// </summary>
        public static void Wait() {
            lock (Lockobject) {
                while (_semaphore)
                    Thread.Sleep(1);
            }
        }


        private static int _counter = 0;
        /// <summary>
        /// Разблокировать поток, который был заблокирован Lock-ом
        /// </summary>
        public static void CounterLock() {
            _counter++;
            lock (Lockobject) {
                while (_semaphore)
                    Thread.Sleep(1);
                _semaphore = true;
            }
        }

        /// <summary>
        /// Блокировать поток, пока не будет вызван Release
        /// </summary>
        public static void CounterRelease() {
            _counter--;
            _semaphore = false;
        }

        public static void CounterReset() {
            _counter = 0;
            _semaphore = false;
        }

        public static void CounterWait() {
            _counter--;
            lock (Lockobject) {
                while (_counter != 0)
                    Thread.Sleep(1);
            }
        }

        public static T GetVisualChild<T>(DependencyObject parent) where T : Visual {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++) {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null) {
                    child = GetVisualChild<T>(v);
                }
                if (child != null) {
                    break;
                }
            }
            return child;
        }
    }
}
