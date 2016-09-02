using System.Windows;

namespace WordTraining.Windows
{
    public partial class RenameDictionaryWindow
    {
        #region Public Properties and Initialization

        public string DictionaryName { get; set; }

        public RenameDictionaryWindow()
        {
            InitializeComponent();
            MainGrid.DataContext = this;

            NameTextBox.Focus();
        }

        #endregion Public Properties and Initialization

        #region Event Handlers

        private void OnRenameButtonClick(object sender, RoutedEventArgs e)
        {
            DictionaryName = NameTextBox.Text;
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            NameTextBox.CaretIndex = DictionaryName.Length;
        }
        
        #endregion Event Handlers
    }
}