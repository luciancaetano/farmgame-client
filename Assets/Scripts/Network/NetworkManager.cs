using System.Collections.Generic;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public string endpoint = "ws://localhost:2567";
    public ColyseusClient client;
    public ColyseusRoom<FarmRoomSchema> farmRoom = null;
    public bool IsConnected = false;
    public GameObject onlinePlayerPrefab;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        client = new ColyseusClient(endpoint);
        ConnectToRoom();
    }

    // Update is called once per frame
    void Update()
    {
        if (farmRoom != null && farmRoom.Connection.IsOpen)
        {
            IsConnected = true;
        }
        else
        {
            IsConnected = false;
        }
    }

    private async void ConnectToRoom()
    {
        try
        {
            farmRoom = await client.JoinOrCreate<FarmRoomSchema>("farm");
            Debug.Log("Connected to room: " + farmRoom.RoomId);

            var callbacks = Callbacks.Get(farmRoom);
            callbacks.OnRemove(state => state.players, OnPlayerRemove);
            callbacks.OnAdd(state => state.players, OnPlayerAdd);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to connect to room: " + e.Message);
        }
    }

    public void OnPlayerAdd(string key, PlayerSchema player)
    {
        if (player.id == farmRoom.SessionId) return; // ignora o jogador local

        var initialPos = new Vector2(player.position.x, player.position.y);
        var go = Instantiate(onlinePlayerPrefab, initialPos, Quaternion.identity);
        var onlineController = go.GetComponent<OnlinePlayerController>();
        onlineController.Initialize(player.id, initialPos);
    }

    public void OnPlayerRemove(string key, PlayerSchema player)
    {
        var onlinePlayer = FindObjectsByType<OnlinePlayerController>(FindObjectsSortMode.None);
        foreach (var op in onlinePlayer)
        {
            if (op.PlayerID == key)
            {
                Destroy(op.gameObject);
                break;
            }
        }
    }
}
