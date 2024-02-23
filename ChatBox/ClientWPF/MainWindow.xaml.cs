using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ClientWPF
{
    public partial class MainWindow : Window
    {
        private Socket socket;
        private string username;
        private Thread receiveThread;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_ConnectClick(object sender, RoutedEventArgs e)
        {
            username = usernameTextBox.Text;
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Nom d'utilisateur invalide.");
                return;
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect("127.0.0.1", 1234); // Adresse IP et port du serveur
                MessageBox.Show("Connecté au serveur.");

                messageTextBox.IsEnabled = true;
                sendButton.IsEnabled = true;
                connectButton.IsEnabled = false;
                usernameTextBox.IsEnabled = false;

                // Démarrer le thread pour écouter les messages du serveur
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de connexion au serveur : {ex.Message}");
            }
        }

        private void Button_SendClick(object sender, RoutedEventArgs e)
        {
            string message = messageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("Message invalide.");
                return;
            }

            try
            {
                EnvoyerDonnees($"{username}: {message}");
                if (message.ToLower() == "quit")
                {
                    Deconnecter();
                    MessageBox.Show("Déconnecté du serveur.");
                    Close();
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("La connexion avec le serveur a été perdue.");
                Deconnecter();
                Close();
            }

            messageTextBox.Text = "";
        }

        private void EnvoyerDonnees(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            socket.Send(buffer);
        }

        private void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    if (socket.Connected && socket.Available > 0)
                    {
                        byte[] buffer = new byte[255];
                        int taille = socket.Receive(buffer);
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, taille);

                        // Afficher le message dans la zone de chat
                        Dispatcher.Invoke(() =>
                        {
                            chatStackPanel.Children.Add(new TextBlock { Text = receivedMessage });
                            chatScrollViewer.ScrollToBottom();
                        });
                    }

                    Thread.Sleep(100); // Attente avant de vérifier à nouveau
                }
            }
            catch (SocketException)
            {
                // La connexion avec le serveur a été perdue
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("La connexion avec le serveur a été perdue.");
                    Deconnecter();
                    Close();
                });
            }
        }

        private void Deconnecter()
        {
            if (socket != null && socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }
}
