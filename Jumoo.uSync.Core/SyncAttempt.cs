using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core
{
    public struct SyncAttempt<T>
    {
        public bool Success { get; private set; }
        public string Name { get; set; }
        public T Item { get; private set; }
        public Type ItemType { get; private set; }
        public ChangeType Change { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; set; }
        public IEnumerable<uSyncChange> Details { get; set; }

        private SyncAttempt(bool success, string name, T item, Type itemType, ChangeType change, string message, Exception ex) : this()
        {
            Success = success;
            Name = name;
            Item = item;
            ItemType = itemType;
            Change = change;
            Message = message;
            Exception = ex;
        }

        public static SyncAttempt<T> Succeed(string name, ChangeType change)
        {
            return new SyncAttempt<T>(true, name, default(T), typeof(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> Succeed(string name, T item, ChangeType change)
        {
            return new SyncAttempt<T>(true, name, item, typeof(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> Succeed(string name, T item, ChangeType change, string message)
        {
            return new SyncAttempt<T>(true, name, item, typeof(T), change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change)
        {
            return new SyncAttempt<T>(false, name, item, typeof(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, name, item, typeof(T), change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, name, item, typeof(T), change, message, ex);
        }

        public static SyncAttempt<T> Fail(string name, T item, ChangeType change, Exception ex)
        {
            return new SyncAttempt<T>(false, name, item, typeof(T), change, string.Empty, ex);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, name, default(T), typeof(T), change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, name, default(T), typeof(T), change, message, ex);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change, Exception ex)
        {
            return new SyncAttempt<T>(false, name, default(T), typeof(T), change, string.Empty, ex);
        }

        public static SyncAttempt<T> Fail(string name, ChangeType change)
        {
            return new SyncAttempt<T>(false, name, default(T), typeof(T), change, string.Empty, null);
        }

        public static SyncAttempt<T> SucceedIf(bool condition, string name, T item, ChangeType change)
        {
            return new SyncAttempt<T>(condition, name, item, typeof(T), change, string.Empty, null);
        }


        // xelement ones, pass type
        //
        public static SyncAttempt<T> Succeed(string name, T item, Type itemType, ChangeType change)
        {
            return new SyncAttempt<T>(true, name, item, itemType, change, string.Empty, null);
        }

        public static SyncAttempt<T> Fail(string name, Type itemType, ChangeType change, string message)
        {
            return new SyncAttempt<T>(false, name, default(T), itemType, change, message, null);
        }

        public static SyncAttempt<T> Fail(string name, Type itemType, ChangeType change, string message, Exception ex)
        {
            return new SyncAttempt<T>(false, name, default(T), itemType, change, message, ex);
        }

        public static SyncAttempt<T> SucceedIf(bool condition, string name, T item, Type itemType, ChangeType change)
        {
            return new SyncAttempt<T>(condition, name, item, itemType, change, string.Empty, null);
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
