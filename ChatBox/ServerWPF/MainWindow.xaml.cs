using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace ServerWPF
{
    public class User
    {
        public string Username { get; set; }
        public Socket ClientSocket { get; set; }

        public User(string username, Socket clientSocket)
        {
            Username = username;
            ClientSocket = clientSocket;
        }
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<User> users = new ObservableCollection<User>();
        private Socket serverSocket;

        public MainWindow()
        {
            InitializeComponent();
            userList.ItemsSource = users;
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1234));
            serverSocket.Listen(10);

            MessageBox.Show("Serveur démarré. En attente de connexions...");
            startServerButton.IsEnabled = false;
            stopServerButton.IsEnabled = true;

            Thread acceptThread = new Thread(AcceptConnections);
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        private void AcceptConnections()
        {
            try
            {
                while (true)
                {
                    Socket clientSocket = serverSocket.Accept();
                    string username = AskForUsername(clientSocket);
                    if (string.IsNullOrEmpty(username))
                    {
                        MessageBox.Show("Nom d'utilisateur invalide.");
                        clientSocket.Close();
                        continue;
                    }

                    User user = new User(username, clientSocket);
                    AddUserToList(user);

                    Thread clientThread = new Thread(() => HandleClient(user));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la connexion du client : {ex.Message}");
            }
        }

        private string AskForUsername(Socket clientSocket)
        {
            byte[] buffer = new byte[255];
            int size = clientSocket.Receive(buffer);
            string username = Encoding.UTF8.GetString(buffer, 0, size).Trim();
            return username;
        }

        private void AddUserToList(User user)
        {
            Dispatcher.Invoke(() =>
            {
                users.Add(user);
            });
        }

        private void HandleClient(User user)
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[255];
                    int size = user.ClientSocket.Receive(buffer);
                    if (size == 0)
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, size);
                    foreach (User u in users)
                    {
                        u.ClientSocket.Send(Encoding.UTF8.GetBytes($"{user.Username}: {message}"));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la gestion du client {user.Username}: {ex.Message}");
            }
            finally
            {
                RemoveUserFromList(user);
                user.ClientSocket.Close();
                MessageBox.Show($"Client {user.Username} déconnecté.");
            }
        }

        private void RemoveUserFromList(User user)
        {
            Dispatcher.Invoke(() =>
            {
                users.Remove(user);
            });
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
                MessageBox.Show("Serveur arrêté.");
                stopServerButton.IsEnabled = false;
                startServerButton.IsEnabled = true;
            }
        }
    }
}
