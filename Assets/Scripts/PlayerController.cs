using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Colyseus.Schema;

public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction;

    [Header("Reconciliação")]
    [SerializeField] private float teleportThreshold = 4f;
    [SerializeField] private float smoothingThreshold = 0.05f;
    [SerializeField] private float smoothTime = 0.1f;

    private Rigidbody2D rb;
    private StateCallbackStrategy<FarmRoomSchema> callbacks;
    private long seq = 0;

    private List<InputMessage> pendingInputs = new List<InputMessage>();
    private Vector2 velocitySmooth;

    [System.Serializable]
    private class InputMessage
    {
        public long seq;
        public float dx;
        public float dy;
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
    }

    void FixedUpdate()
    {
        if (!NetworkManager.Instance.IsConnected) return;
        Time.fixedDeltaTime = 1f / 60f;

        Vector2 inputVector = moveAction.action.ReadValue<Vector2>();

        // Cria e envia input
        var msg = new InputMessage { seq = ++seq, dx = inputVector.x, dy = inputVector.y };
        NetworkManager.Instance.farmRoom.Send("move", msg);

        // Adiciona no buffer de inputs pendentes
        pendingInputs.Add(msg);

        // Predição local imediata
        rb.position += inputVector * moveSpeed * Time.fixedDeltaTime;

        // Reconciliação
        Reconcile();
    }

    void Reconcile()
    {
        var player = NetworkManager.Instance.farmRoom.State.players[NetworkManager.Instance.farmRoom.SessionId];
        if (player == null) return;

        Vector2 serverPos = new Vector2(player.position.x, player.position.y);

        // Remove inputs já processados pelo servidor
        pendingInputs.RemoveAll(input => input.seq <= player.lastSeq);

        // Predição: reaplica apenas inputs pendentes
        Vector2 predictedPos = serverPos;
        foreach (var input in pendingInputs)
        {
            predictedPos += new Vector2(input.dx, input.dy) * moveSpeed * Time.fixedDeltaTime;
        }

        // Correção suave ou teleport
        float distance = Vector2.Distance(rb.position, predictedPos);

        if (distance > teleportThreshold)
        {
            rb.position = predictedPos; // Teleporta se estiver muito fora
        }
        else if (distance > smoothingThreshold)
        {
            rb.position = Vector2.SmoothDamp(rb.position, predictedPos, ref velocitySmooth, smoothTime);
        }
    }

    void OnPlayerAdd(string key, PlayerSchema player)
    {
        if (key != NetworkManager.Instance.farmRoom.SessionId) return;

        callbacks.OnChange(player, () =>
        {
            moveSpeed = player.moveSpeed;
        });
    }
}
