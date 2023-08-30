namespace GourmetClient.Network
{
    using System;
    using System.Threading.Tasks;

    public class LoginHandle : IAsyncDisposable
    {
        private readonly Func<ValueTask> _disposeAction;

        private bool _disposed;

        public LoginHandle(bool loginSuccessful, Func<ValueTask> disposeAction)
        {
            _disposeAction = disposeAction;
            LoginSuccessful = loginSuccessful;
        }

        public bool LoginSuccessful { get; }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            return _disposeAction();
        }
    }
}
