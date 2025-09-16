using UnityEngine;
using UnityEngine.InputSystem;
using Colyseus.Schema;
using System.Collections.Generic;
using System.Collections;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D rb;

    private PlayerSchema Player => GetLocalPlayer();
    private int currentTick = 0;

    private float elapsedTime = 0f;
    private float fixedTimeStep = 1f / 60f; // 60 ticks per second

    [System.Serializable]
    class InputPayload
    {
        public bool left;
        public bool right;
        public bool up;
        public bool down;
        public float tick;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        moveAction.action.Enable();
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => NetworkManager.Instance.IsConnected);
    }

    void Update()
    {
        if (Player == null)
        {
            Debug.LogWarning("Player is null - check network connection");
            return;
        }

        elapsedTime += Time.deltaTime;

        while (elapsedTime >= fixedTimeStep)
        {
            elapsedTime -= fixedTimeStep;
            FixedServerUpdate();
        }
    }

    void FixedServerUpdate()
    {
        currentTick++;

        var inputVector = moveAction.action.ReadValue<Vector2>();

        InputPayload input = new InputPayload
        {
            left = inputVector.x < 0,
            right = inputVector.x > 0,
            up = inputVector.y > 0,
            down = inputVector.y < 0,
            tick = currentTick
        };

        NetworkManager.Instance.farmRoom.Send(0, input);

        Vector2 moveDelta = Vector2.zero;

        float speed = (Player != null && Player.moveSpeed > 0) ? Player.moveSpeed : 0.5f;

        if (input.left)
            moveDelta.x -= speed;
        if (input.right)
            moveDelta.x += speed;
        if (input.up)
            moveDelta.y += speed;
        if (input.down)
            moveDelta.y -= speed;

        // Corrigir: usar Time.fixedDeltaTime em vez de delta (que está em milissegundos)
        Vector2 newPosition = rb.position + moveDelta * fixedTimeStep;
        rb.MovePosition(newPosition);


        // Debug para verificar se está funcionando
        if (moveDelta != Vector2.zero)
        {
            Debug.Log($"Moving player: {moveDelta}, Speed: {Player.moveSpeed}, Position: {newPosition}");
        }
    }

    [ContextMenu("Cheat - Teleport North")]
    public void TeleportNorth()
    {
        TeleportPlayer(Vector2.up);
    }

    [ContextMenu("Cheat - Teleport South")]
    public void TeleportSouth()
    {
        TeleportPlayer(Vector2.down);
    }

    [ContextMenu("Cheat - Teleport East")]
    public void TeleportEast()
    {
        TeleportPlayer(Vector2.right);
    }

    [ContextMenu("Cheat - Teleport West")]
    public void TeleportWest()
    {
        TeleportPlayer(Vector2.left);
    }

    [ContextMenu("Cheat - Random Teleport")]
    public void RandomTeleport()
    {
        Vector2 randomDirection = new Vector2(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;
        TeleportPlayer(randomDirection);
    }

    private void TeleportPlayer(Vector2 direction)
    {
        if (Player == null) return;

        Vector2 teleportPosition = rb.position + (direction * 5f);
        rb.position = teleportPosition;
        Debug.Log($"Teleported to: {teleportPosition}");
    }

    private PlayerSchema GetLocalPlayer()
    {
        var nm = NetworkManager.Instance;
        var farmRoom = nm?.farmRoom;
        var state = farmRoom?.State;
        var sessionId = farmRoom?.SessionId;

        if (state == null || state.players == null || sessionId == null) return null;
        return state.players.ContainsKey(sessionId) ? state.players[sessionId] : null;
    }
}
