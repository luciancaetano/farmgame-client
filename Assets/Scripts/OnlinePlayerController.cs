using System.Collections;
using UnityEngine;
using Colyseus.Schema;

public class OnlinePlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float smoothing = 5f; // unidades/segundo
    [SerializeField] private float teleportThreshold = 4f; // teleporta se estiver muito longe
    [SerializeField] private float smoothingThreshold = 0.05f; // aplica smooth apenas acima desse delta
    [SerializeField] private float smoothTime = 0.1f;

    public Grid worldGrid;

    private Rigidbody2D rb;
    private StateCallbackStrategy<FarmRoomSchema> callbacks;
    private Vector2 targetPosition;
    private Vector2 velocitySmooth;

    public string PlayerID;
    private PlayerSchema Player
    {
        get
        {
            var nm = NetworkManager.Instance;
            if (nm == null || nm.farmRoom == null || nm.farmRoom.State == null || PlayerID == null)
                return null;
            if (!nm.farmRoom.State.players.ContainsKey(PlayerID))
                return null;
            return nm.farmRoom.State.players[PlayerID];
        }
    }

    // Inicializa com ID e posição inicial
    public void Initialize(string id, Vector2 initialPosition, Grid grid)
    {
        worldGrid = grid;
        PlayerID = id;
        targetPosition = initialPosition;

        if (rb != null)
        {
            rb.position = initialPosition;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    IEnumerator Start()
    {
        // Aguarda conexão com o servidor
        yield return new WaitUntil(() => NetworkManager.Instance.IsConnected);

        callbacks = Callbacks.Get(NetworkManager.Instance.farmRoom);

        // Registra callback para atualizar targetPosition quando o player/npc mudar
        if (Player != null)
        {
            callbacks.OnChange(Player, () =>
            {
                targetPosition = new Vector2(Player.position.x, Player.position.y);
            });
        }

        Debug.Log($"OnlinePlayerController for {PlayerID} connected.");
    }

    void FixedUpdate()
    {
        if (Player == null) return;

        // Calcula distância até a posição do servidor
        float distance = Vector2.Distance(rb.position, targetPosition);

        if (distance > teleportThreshold)
        {
            // Teleporta se estiver muito longe
            rb.position = targetPosition;
            velocitySmooth = Vector2.zero;
        }
        else if (distance > smoothingThreshold)
        {
            // Suaviza movimento
            rb.position = Vector2.SmoothDamp(rb.position, targetPosition, ref velocitySmooth, smoothTime);
        }
        else
        {
            // Pequenos ajustes
            rb.position = targetPosition;
            velocitySmooth = Vector2.zero;
        }
    }
}
