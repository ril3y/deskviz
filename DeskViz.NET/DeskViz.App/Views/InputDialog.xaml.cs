using System.Windows;
using System.Windows.Input;

namespace DeskViz.App.Views
{
    /// <summary>
    /// Simple input dialog for getting text from user
    /// </summary>
    public partial class InputDialog : Window
    {
        /// <summary>
        /// Gets the input value
        /// </summary>
        public string InputValue => InputTextBox.Text;

        /// <summary>
        /// Initializes a new instance of InputDialog
        /// </summary>
        public InputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeComponent();
            
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.SelectAll();
            InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}