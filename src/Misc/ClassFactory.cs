using System;

namespace FaceDetection.Utils
{
    public class Singleton<T> where T : class
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() => CreateInstanceOfT());
        private static T CreateInstanceOfT() => Activator.CreateInstance(typeof(T), true) as T;
        public static T Instance { get => _instance.Value; }
    }

    public class SingletonNotifyPropertyChanged<T> : BaseNotifyPropertyChanged where T : class
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() => CreateInstanceOfT());
        private static T CreateInstanceOfT() => Activator.CreateInstance(typeof(T), true) as T;
        public static T Instance { get => _instance.Value; }
    }
}
