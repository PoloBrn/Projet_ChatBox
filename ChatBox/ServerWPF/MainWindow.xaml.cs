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
        private bool isServerRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            userList.ItemsSource = users;
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (isServerRunning)
            {
                MessageBox.Show("Le serveur est déjà en cours d'exécution.");
                return;
            }

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 1234));
            serverSocket.Listen(10);

            MessageBox.Show("Serveur démarré. En attente de connexions...");
            isServerRunning = true;
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
                    Socket clientSocket = serverSocket.Accept(); // Accepter la connexion entrante
                    MessageBox.Show("Client connecté.");

                    // Demander et attribuer un nom d'utilisateur au client
                    string username = AskForUsername(clientSocket);
                    if (string.IsNullOrEmpty(username))
                    {
                        MessageBox.Show("Nom d'utilisateur invalide.");
                        clientSocket.Close();
                        continue;
                    }

                    // Créer un nouvel utilisateur avec le nom d'utilisateur et l'ajouter à la liste
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
            // Mettre à jour l'interface utilisateur à partir du thread principal
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
                    if (size == 0) // Si size == 0, le client s'est déconnecté
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, size);
                    MessageBox.Show($"{user.Username}: {message}");

                    // Diffuser le message à tous les utilisateurs connectés
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
            // Mettre à jour l'interface utilisateur à partir du thread principal
            Dispatcher.Invoke(() =>
            {
                users.Remove(user);
            });
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isServerRunning)
            {
                MessageBox.Show("Le serveur n'est pas en cours d'exécution.");
                return;
            }

            if (serverSocket != null)
            {
                serverSocket.Close();
                MessageBox.Show("Serveur arrêté.");
            }

            isServerRunning = false;
            startServerButton.IsEnabled = true;
            stopServerButton.IsEnabled = false;
        }
    }
}
