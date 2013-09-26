namespace Core {
    using System;

    /// <summary>
    /// Taken from http://lostechies.com/chrispatterson/2012/11/29/idisposable-done-right/
    /// </summary>
    public class DisposableClass : IDisposable {
        private bool _disposed;
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~DisposableClass() {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing) {
            if (_disposed) return;
            if (disposing) {
                // free IDisposable objects
            }
            // release unmanaged objects
            _disposed = true;
            
        }

    }
}