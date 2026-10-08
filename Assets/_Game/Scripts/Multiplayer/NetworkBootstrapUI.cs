using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public sealed class NetworkBootstrapUI : MonoBehaviour
{
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private NetworkManager networkManager;
    private UnityTransport transport;
    private string portText;
    private string statusMessage;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
        portText = port.ToString();
    }

    private void OnGUI()
    {
        const float width = 230f;
        GUILayout.BeginArea(new Rect(Screen.width - width - 16f, 16f, width, 270f), GUI.skin.box);
        GUILayout.Label("One Piece 2D - Multiplayer");

        if (!networkManager.IsListening)
        {
            GUILayout.Label("Server address");
            serverAddress = GUILayout.TextField(serverAddress);
            GUILayout.Label("Port");
            portText = GUILayout.TextField(portText);

            if (GUILayout.Button("Start Host"))
                StartHost();

            if (GUILayout.Button("Start Client"))
                StartClient();

            if (GUILayout.Button("Start Dedicated Server"))
                StartServer();
        }
        else
        {
            string mode = networkManager.IsHost
                ? "Host"
                : networkManager.IsServer ? "Server" : "Client";
            GUILayout.Label($"Mode: {mode}");
            GUILayout.Label($"Clients: {networkManager.ConnectedClientsIds.Count}");
            GUILayout.Label($"Port: {port}");

            if (GUILayout.Button("Shutdown"))
                networkManager.Shutdown();
        }

        if (!string.IsNullOrEmpty(statusMessage))
            GUILayout.Label(statusMessage);

        GUILayout.Label("Move: Arrow/WASD | Target: Tab | Auto: Q");
        GUILayout.EndArea();
    }

    private void StartHost()
    {
        if (!TryPrepareServerPort())
            return;

        transport.SetConnectionData(serverAddress, port, "0.0.0.0");
        statusMessage = networkManager.StartHost()
            ? $"Host started on port {port}."
            : "Could not start Host.";
    }

    private void StartClient()
    {
        if (!TryReadPort())
            return;

        transport.SetConnectionData(serverAddress, port);
        statusMessage = networkManager.StartClient()
            ? $"Connecting to {serverAddress}:{port}..."
            : "Could not start Client.";
    }

    private void StartServer()
    {
        if (!TryPrepareServerPort())
            return;

        transport.SetConnectionData(serverAddress, port, "0.0.0.0");
        statusMessage = networkManager.StartServer()
            ? $"Server started on port {port}."
            : "Could not start Server.";
    }

    private bool TryPrepareServerPort()
    {
        if (!TryReadPort())
            return false;

        ushort requestedPort = port;
        for (int offset = 0; offset < 20; offset++)
        {
            int candidate = requestedPort + offset;
            if (candidate > ushort.MaxValue)
                break;

            ushort candidatePort = (ushort)candidate;
            if (!IsUdpPortAvailable(candidatePort))
                continue;

            port = candidatePort;
            portText = port.ToString();
            statusMessage = port == requestedPort
                ? string.Empty
                : $"Port {requestedPort} is busy. Using {port}.";
            return true;
        }

        statusMessage = $"No free UDP port found near {requestedPort}.";
        return false;
    }

    private bool TryReadPort()
    {
        if (ushort.TryParse(portText, out ushort parsedPort) && parsedPort > 0)
        {
            port = parsedPort;
            return true;
        }

        statusMessage = "Port must be between 1 and 65535.";
        return false;
    }

    private static bool IsUdpPortAvailable(ushort candidatePort)
    {
        try
        {
            using (Socket socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp))
            {
                socket.ExclusiveAddressUse = true;
                socket.Bind(new IPEndPoint(IPAddress.Any, candidatePort));
                return true;
            }
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private void OnDisable()
    {
        if (networkManager != null && networkManager.IsListening)
            networkManager.Shutdown();
    }
}