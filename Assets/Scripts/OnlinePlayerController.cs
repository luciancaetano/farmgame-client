using System.Collections;
using UnityEngine;
using Colyseus.Schema;

public class OnlinePlayerController : MonoBehaviour
{
    [SerializeField] private float smoothing = 5f; // velocidade de interpolação em unidades/segundo
    public Grid worldGrid;

    private Rigidbody2D rb;
    private StateCallbackStrategy<FarmRoomSchema> callbacks;
    private Vector2 targetPosition;
    public string PlayerID;

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
        callbacks.OnAdd(state => state.players, OnPlayerAdd);

        Debug.Log($"OnlinePlayerController for {PlayerID} connected.");
    }

    void FixedUpdate()
    {
        // Interpola em direção à posição alvo (do servidor) com velocidade constante
        rb.position = Vector2.MoveTowards(
            rb.position,
            targetPosition,
            smoothing * Time.fixedDeltaTime
        );
    }

    void OnPlayerAdd(string key, PlayerSchema player)
    {
        if (key != PlayerID) return;
        // Escuta mudanças só da posição do player
        callbacks.OnChange(player.position, () =>
        {
            // Converte da célula para world space
            Vector3 cell = new Vector3(player.position.x, player.position.y, 0);
            targetPosition = worldGrid.GetCellCenterWorld(Vector3Int.FloorToInt(cell));
        });
    }
}
