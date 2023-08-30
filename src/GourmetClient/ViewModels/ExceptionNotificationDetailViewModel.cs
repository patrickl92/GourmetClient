namespace GourmetClient.ViewModels
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using Behaviors;

    using Network;

    using Notifications;

    using Utils;

    public class ExceptionNotificationDetailViewModel : ObservableObject
    {
        private readonly ExceptionNotification _notification;

        public ExceptionNotificationDetailViewModel(ExceptionNotification notification)
        {
            _notification = notification ?? throw new ArgumentNullException(nameof(notification));

            CopyDetailsToClipboardCommand = new AsyncDelegateCommand(CopyInformationToClipboard);
        }

        public ICommand CopyDetailsToClipboardCommand { get; }

        public string Message => _notification.Message;

        public Exception Exception => _notification.Exception;

        public ExceptionNotification GetNotification() => _notification;

        private Task CopyInformationToClipboard()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("GourmetClient notification details");
            stringBuilder.Append("Type: ").AppendLine(_notification.NotificationType.ToString());
            stringBuilder.Append("Message: ").AppendLine(_notification.Message);

            if (_notification.Exception is GourmetRequestException requestException)
            {
                stringBuilder.Append("Request: ").AppendLine(requestException.UriInfo);
                stringBuilder.AppendLine("Exception:").AppendLine(requestException.ToString());
            }
            else if (_notification.Exception is GourmetParseException parseException)
            {
                stringBuilder.Append("Request: ").AppendLine(parseException.UriInfo);
                stringBuilder.AppendLine("Exception:").AppendLine(parseException.ToString());
                stringBuilder.AppendLine("---------------------------------------------------");
                stringBuilder.AppendLine("HTML:").AppendLine(parseException.ResponseContent);
            }
            else if (_notification.Exception != null)
            {
                stringBuilder.AppendLine("Exception:").AppendLine(_notification.Exception.ToString());
            }

            Clipboard.SetText(stringBuilder.ToString());

            return Task.CompletedTask;
        }
    }
}
