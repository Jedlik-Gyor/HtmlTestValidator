using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HtmlTestValidator.UserControls
{
    /// <summary>
    /// Interaction logic for MessageBar.xaml
    /// </summary>
    public partial class MessageBar : UserControl
    {
        public enum MessageTypeEnum { Error, Warning, Information }

        protected string Message
        {
            get
            {
                return lblMessage.Content.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(lblMessage.Content.ToString()) && !string.IsNullOrEmpty( value))
                    this.Visibility = Visibility.Visible;
                lblMessage.Content = value;
                if (string.IsNullOrEmpty(lblMessage.Content.ToString()))
                    this.Visibility = Visibility.Hidden;
            }
        }

        private MessageTypeEnum messageType = MessageTypeEnum.Error;
        protected MessageTypeEnum MessageType
        {
            get
            {
                return messageType;
            }
            set
            {
                messageType = value;
                switch (messageType)
                {
                    case MessageTypeEnum.Error:
                        lblMessage.Foreground = Brushes.White;
                        lblMessage.Background = Brushes.Red;
                        lblClose.Foreground = Brushes.White;
                        lblClose.Background = Brushes.Red;
                        break;
                    case MessageTypeEnum.Warning:
                        lblMessage.Foreground = Brushes.Black;
                        lblMessage.Background = Brushes.Gold;
                        lblClose.Foreground = Brushes.Black;
                        lblClose.Background = Brushes.Gold;
                        break;
                    case MessageTypeEnum.Information:
                        lblMessage.Foreground = Brushes.White;
                        lblMessage.Background = Brushes.Blue;
                        lblClose.Foreground = Brushes.White;
                        lblClose.Background = Brushes.Blue;
                        break;
                    default:
                        break;
                }

            }

        }

        public event RoutedEventHandler Close;

        public MessageBar()
        {
            InitializeComponent();            
        }

        public void Error(string message)
        {
            Message = message;
            MessageType = MessageTypeEnum.Error;
        }

        public void Warning(string message)
        {
            Message = message;
            MessageType = MessageTypeEnum.Warning;
        }

        public void Info(string message)
        {
            Message = message;
            MessageType = MessageTypeEnum.Information;
        }

        public void Clear()
        {
            Message = string.Empty;
        }

        private void lblClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Clear();
            if (Close != null)
                Close(this, new RoutedEventArgs());
        }

        private void lblMessage_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(lblMessage.Content.ToString());
        }
    }
}
