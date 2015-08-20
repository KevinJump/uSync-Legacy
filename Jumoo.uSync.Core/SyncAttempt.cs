using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Core
{
    public struct SyncAttempt<T>
    {
        public bool Success { get; }
        public string Name { get; set; }
        public T Item {get; }
        public ChangeType Change { get; }
        public string Message { get; }
        public Exception Exception { get; set; }

        private SyncAttempt(bool success, string name, T item, ChangeType change, string message, Exception ex)
        {
            Success = success;
            Name = name;
            Item = item;
            Change = change;
            Message = message;
            Exception = ex;
        }

        public static SyncAttempt<T> Succeed(string name, ChangeType change)
        {
            return new SyncAttempt<T>(true, name, default(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> Succeed(string name, T item, ChangeType change)
        {
            return new SyncAttempt<T>(true, name, item, change, string.Empty, null);
        }

        public static SyncAttempt<T> Succeed(string name, T item, ChangeType change, string message)
        {
            return new SyncAttempt<T>(true, name, item, change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change)
        {
            return new SyncAttempt<T>(false, name, item, change, string.Empty, null);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, name, item, change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, name, item, change, message, ex);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change, Exception ex)
        {
            return new SyncAttempt<T>(false, name, item, change, string.Empty, ex);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, name, default(T), change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, name, default(T), change, message, ex);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change, Exception ex)
        {
            return new SyncAttempt<T>(false, name, default(T), change, string.Empty, ex);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change)
        {
            return new SyncAttempt<T>(false, name, default(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> SucceedIf(bool condition, string name, T item, ChangeType change)
        {
            return new SyncAttempt<T>(condition, name, item, change, string.Empty, null);
        }
    }


    public enum ChangeType
    {
        NoChange = 0,
        Import,
        Export,
        Update,
        Delete,
        WillChange,
        Information,
        Rolledback,
        Fail = 11,
        ImportFail,
        Mismatch
    }
}
