using UnityEngine;
using UnityEngine.InputSystem;
using Colyseus.Schema;
using System.Collections;
using System;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D rb;

    private PlayerSchema Player => GetLocalPlayer();
    private int currentTick = 0;

    private float elapsedTime = 0f;
    private float fixedTimeStep = 1f / 60f; // 0.016s em segundos
    private List<InputPayload> pendingInputs = new List<InputPayload>();
    private Position2D lastServerPosition = new Position2D();


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

        var callbacks = Callbacks.Get(NetworkManager.Instance.farmRoom);

        callbacks.OnAdd(s => s.players, (key, player) =>
        {
            if (player.id == NetworkManager.Instance.farmRoom.SessionId)
            {
                callbacks.OnChange(player, () =>
                {
                    if (lastServerPosition.x != player.position.x || lastServerPosition.y != player.position.y)
                    {
                        OnServerUpdate(player);
                        lastServerPosition.x = player.position.x;
                        lastServerPosition.y = player.position.y;
                    }
                });
            }
        });
    }

    void Update()
    {
        if (Player == null) return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= fixedTimeStep)
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
        pendingInputs.Add(input);
        NetworkManager.Instance.farmRoom.Send(0, input);

        Vector2 moveDelta = CalculateMoveDelta(input);
        rb.MovePosition(rb.position + moveDelta);
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

    private Vector2 CalculateMoveDelta(InputPayload input)
    {
        float speed = (Player != null && Player.moveSpeed > 0) ? Player.moveSpeed : 0.5f;
        Vector2 moveDelta = Vector2.zero;

        if (input.left) moveDelta.x -= speed;
        if (input.right) moveDelta.x += speed;
        if (input.up) moveDelta.y += speed;
        if (input.down) moveDelta.y -= speed;

        return moveDelta;
    }

    void OnServerUpdate(PlayerSchema serverPlayer)
    {
        // Posiciona na "verdade" do servidor
        rb.position = new Vector2(serverPlayer.position.x, serverPlayer.position.y);

        // Remove inputs já confirmados
        pendingInputs.RemoveAll(i => i.tick <= Player.tick);

        // Reaplica os inputs não confirmados
        foreach (var input in pendingInputs)
        {
            Vector2 moveDelta = CalculateMoveDelta(input);
            rb.position += moveDelta;
        }
    }
}
