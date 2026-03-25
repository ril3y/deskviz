using System;
using System.Windows;
using System.Windows.Input;

namespace DeskViz.Widgets.Meshtastic
{
    public partial class MessageDetailDialog : Window
    {
        private readonly MeshMessage _message;
        private readonly Func<uint, string, System.Threading.Tasks.Task>? _sendReply;

        public MessageDetailDialog(MeshMessage message, Func<uint, string, System.Threading.Tasks.Task>? sendReply)
        {
            InitializeComponent();

            _message = message;
            _sendReply = sendReply;

            SenderText.Text = message.SenderName;
            TimestampText.Text = message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            SignalText.Text = !string.IsNullOrEmpty(message.SignalInfo)
                ? message.SignalInfo
                : "No signal data";
            MessageText.Text = message.Text;

            if (_sendReply == null)
            {
                ReplyTextBox.IsEnabled = false;
                SendButton.IsEnabled = false;
                StatusText.Text = "Not connected - cannot reply";
                StatusText.Visibility = Visibility.Visible;
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var text = ReplyTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(text) || _sendReply == null) return;

            SendButton.IsEnabled = false;
            ReplyTextBox.IsEnabled = false;
            StatusText.Text = "Sending...";
            StatusText.Visibility = Visibility.Visible;

            try
            {
                await _sendReply(_message.From, text);
                StatusText.Text = "Sent!";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(34, 197, 94));
                ReplyTextBox.Text = "";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed: {ex.Message}";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(239, 68, 68));
            }
            finally
            {
                SendButton.IsEnabled = true;
                ReplyTextBox.IsEnabled = true;
            }
        }

        private void ReplyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
