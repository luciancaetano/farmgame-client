using System.Collections;
using System.Collections.Generic;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server Settings")]
    public string endpoint = "ws://localhost:2567";
    public ColyseusClient client;
    public ColyseusRoom<FarmRoomSchema> farmRoom = null;

    [Header("Player Settings")]
    public GameObject onlinePlayerPrefab;
    [SerializeField] private Grid worldGrid;

    [Header("Network Info")]
    public bool IsConnected = false;
    public float Ping = 0f;
    public int PingSamples = 10; // Quantidade de pings para média
    private Queue<float> pingHistory = new Queue<float>();

    private Coroutine pingCoroutine;
    private float lastPingSentTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        client = new ColyseusClient(endpoint);
        ConnectToRoom();
    }

    private void Update()
    {
        IsConnected = farmRoom != null && farmRoom.Connection.IsOpen;
    }

    #region Room Connection

    private async void ConnectToRoom()
    {
        try
        {
            farmRoom = await client.JoinOrCreate<FarmRoomSchema>("farm");
            Debug.Log("Connected to room: " + farmRoom.RoomId);

            var callbacks = Callbacks.Get(farmRoom);
            callbacks.OnRemove(state => state.players, OnPlayerRemove);
            callbacks.OnAdd(state => state.players, OnPlayerAdd);

            // Mensagens de ping/pong
            farmRoom.OnMessage<string>("ping", msg => Debug.Log("Received ping from server: " + msg));
            farmRoom.OnMessage<string>("pong", OnPongReceived);

            // Começa a rotina de ping
            StartPingRoutine();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to connect to room: " + e.Message);
        }
    }

    #endregion

    #region Player Management

    public void OnPlayerAdd(string key, PlayerSchema player)
    {
        if (player.id == farmRoom.SessionId) return; // ignora o jogador local

        Vector2 initialPos = new Vector2(player.position.x, player.position.y);
        GameObject go = Instantiate(onlinePlayerPrefab, initialPos, Quaternion.identity);
        OnlinePlayerController onlineController = go.GetComponent<OnlinePlayerController>();
        onlineController.Initialize(player.id, initialPos, worldGrid);
    }

    public void OnPlayerRemove(string key, PlayerSchema player)
    {
        var onlinePlayers = FindObjectsByType<OnlinePlayerController>(FindObjectsSortMode.None);
        foreach (var op in onlinePlayers)
        {
            if (op.PlayerID == key)
            {
                Destroy(op.gameObject);
                break;
            }
        }
    }

    #endregion

    #region Ping System

    private void OnDisable()
    {
        if (farmRoom != null)
        {
            farmRoom.OnMessage<string>("pong", null);
        }

        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
        }
    }

    private void StartPingRoutine()
    {
        if (pingCoroutine != null)
            StopCoroutine(pingCoroutine);

        pingCoroutine = StartCoroutine(PingRoutine());
    }

    private IEnumerator PingRoutine()
    {
        while (farmRoom != null && farmRoom.Connection.IsOpen)
        {
            lastPingSentTime = Time.time;
            farmRoom.Send("ping", null);
            yield return new WaitForSeconds(2f);
        }
    }

    private void OnPongReceived(string message)
    {
        float currentPing = (Time.time - lastPingSentTime) * 1000f; // ping em ms
        AddPingSample(currentPing);
    }

    private void AddPingSample(float ping)
    {
        pingHistory.Enqueue(ping);
        if (pingHistory.Count > PingSamples)
            pingHistory.Dequeue();

        float sum = 0f;
        foreach (float p in pingHistory)
            sum += p;

        Ping = sum / pingHistory.Count;
        Debug.Log($"Ping médio: {Ping:F1} ms");
    }

    #endregion
}
