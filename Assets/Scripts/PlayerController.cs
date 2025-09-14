using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Colyseus.Schema;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private float smoothing = 10f; // velocidade de interpolaçao
    [SerializeField] private Grid worldGrid;

    private Rigidbody2D rb;
    private StateCallbackStrategy<FarmRoomSchema> callbacks;
    private List<InputMessage> pendingInputs = new List<InputMessage>();
    private int seq = 0;

    [System.Serializable]
    private class InputMessage
    {
        public int seq;
        public bool up, down, left, right;
        public float dt;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => NetworkManager.Instance.IsConnected);

        callbacks = Callbacks.Get(NetworkManager.Instance.farmRoom);
        callbacks.OnAdd(state => state.players, OnPlayerAdd);

        Debug.Log("PlayerController connected to room.");
    }

    void FixedUpdate()
    {
        Vector2 inputVector = moveAction.action.ReadValue<Vector2>();
        if (inputVector == Vector2.zero) return;

        InputMessage input = new InputMessage
        {
            seq = seq++,
            up = inputVector.y > 0,
            down = inputVector.y < 0,
            left = inputVector.x < 0,
            right = inputVector.x > 0,
            dt = Time.fixedDeltaTime
        };

        pendingInputs.Add(input);

        // envia para o servidor
        NetworkManager.Instance.farmRoom.Send("move", input);

        // predicao local
        Vector2 delta = Vector2.zero;
        if (input.up) delta.y += 1;
        if (input.down) delta.y -= 1;
        if (input.left) delta.x -= 1;
        if (input.right) delta.x += 1;

        delta = delta.normalized * moveSpeed * input.dt;
        rb.MovePosition(rb.position + delta);
    }

    public void OnServerState(Vector2 newServerCellPos, int ackSeq)
    {
        // Corrige a posição do cliente imediatamente
        rb.position = newServerCellPos;

        // Remove todos os inputs que o servidor já processou
        pendingInputs.RemoveAll(input => input.seq <= ackSeq);

        // Reaplica os inputs que ainda não foram confirmados
        foreach (var input in pendingInputs)
        {
            Vector2 delta = Vector2.zero;
            if (input.up) delta.y += 1;
            if (input.down) delta.y -= 1;
            if (input.left) delta.x -= 1;
            if (input.right) delta.x += 1;

            delta = delta.normalized * moveSpeed * input.dt;
            rb.position += delta; // Move sem enviar de novo
        }
    }

    void OnPlayerAdd(string key, PlayerSchema player)
    {
        if (key != NetworkManager.Instance.farmRoom.SessionId) return;

        callbacks.OnChange(player, () =>
        {
            Vector2 newServerCellPos = new Vector2(player.position.x, player.position.y);
            OnServerState(newServerCellPos, (int)player.lastSeq + 1);
            moveSpeed = player.moveSpeed;
        });
    }
}
