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
        public T Item {get; }
        public ChangeType Change { get; }
        public string Message { get; }
        public Exception Exception { get; set; }

        private SyncAttempt(bool success, T item, ChangeType change, string message, Exception ex)
        {
            Success = success;
            Item = item;
            Change = change;
            Message = message;
            Exception = ex;
        }

        public static SyncAttempt<T> Succeed(ChangeType change)
        {
            return new SyncAttempt<T>(true, default(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> Succeed(T item, ChangeType change)
        {
            return new SyncAttempt<T>(true, item, change, string.Empty, null);
        }

        public static SyncAttempt<T> Succeed(T item, ChangeType change, string message)
        {
            return new SyncAttempt<T>(true, item, change, message, null);
        }

        public static SyncAttempt<T> Fail(T item, ChangeType change)
        {
            return new SyncAttempt<T>(false, item, change, string.Empty, null);
        }

        public static SyncAttempt<T> Fail(T item, ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, item, change, message, null);
        }

        public static SyncAttempt<T> Fail(T item, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, item, change, message, ex);
        }

        public static SyncAttempt<T> Fail(T item, ChangeType change, Exception ex)
        {
            return new SyncAttempt<T>(false, item, change, string.Empty, ex);
        }

        public static SyncAttempt<T> Fail(ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, default(T), change, message, null);
        }

        public static SyncAttempt<T> Fail(ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, default(T), change, message, ex);
        }

        public static SyncAttempt<T> Fail(ChangeType change, Exception ex)
        {
            return new SyncAttempt<T>(false, default(T), change, string.Empty, ex);
        }

        public static SyncAttempt<T> Fail(ChangeType change)
        {
            return new SyncAttempt<T>(false, default(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> SucceedIf(bool condition, T item, ChangeType change)
        {
            return new SyncAttempt<T>(condition, item, change, string.Empty, null);
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
