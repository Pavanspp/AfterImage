using UnityEngine;

// Auto-wires all cross-GameObject references on scene load.
// Lives on a SceneLinker GameObject in every scene.
// Finds Player, Echo, and Camera by tag at Awake and connects everything.
// No manual Inspector wiring needed for cross-references.
public class SceneLinker : MonoBehaviour
{
    [Header("Tags — must match GameObject tags in scene")]
    public string playerTag = "Player";
    public string echoTag   = "Echo";
    public string cameraTag = "MainCamera";

    void Awake()
    {
        GameObject playerGO = GameObject.FindWithTag(playerTag);
        GameObject echoGO   = GameObject.FindWithTag(echoTag);
        GameObject cameraGO = GameObject.FindWithTag(cameraTag);

        if (playerGO == null) { Debug.LogError("SceneLinker: No GameObject tagged 'Player' found."); return; }
        if (echoGO   == null) { Debug.LogError("SceneLinker: No GameObject tagged 'Echo' found.");   return; }
        if (cameraGO == null) { Debug.LogError("SceneLinker: No GameObject tagged 'MainCamera' found."); return; }

        // ── Grab all components ──
        PlayerStateHub      playerState  = playerGO.GetComponent<PlayerStateHub>();
        PlayerMover         playerMover  = playerGO.GetComponent<PlayerMover>();
        PlayerPathTrail     pathTrail    = playerGO.GetComponent<PlayerPathTrail>();
        EchoInputHandler    echoInput    = playerGO.GetComponent<EchoInputHandler>();

        EchoStateHub        echoState    = echoGO.GetComponent<EchoStateHub>();
        EchoRecordingBuffer echoBuffer   = echoGO.GetComponent<EchoRecordingBuffer>();
        EchoReplayer        echoReplayer = echoGO.GetComponent<EchoReplayer>();
        EchoController      echoCtrl     = echoGO.GetComponent<EchoController>();
        EchoVisuals         echoVisuals  = echoGO.GetComponent<EchoVisuals>();

        CameraFollow2D      cam          = cameraGO.GetComponent<CameraFollow2D>();

        // ── Wire Echo components ──
        echoBuffer.playerState   = playerState;

        echoReplayer.buffer      = echoBuffer;
        echoReplayer.echoState   = echoState;

        echoCtrl.echoState       = echoState;
        echoCtrl.buffer          = echoBuffer;
        echoCtrl.replayer        = echoReplayer;
        echoCtrl.visuals         = echoVisuals;
        echoCtrl.echoCollider    = echoGO.GetComponent<BoxCollider2D>();
        echoCtrl.playerState     = playerState;
        echoCtrl.playerMover     = playerMover;

        echoVisuals.echoRenderer = echoGO.GetComponent<SpriteRenderer>();
        echoVisuals.echoState    = echoState;
        echoVisuals.replayer     = echoReplayer;

        // ── Wire Player components that need Echo ──
        pathTrail.echoReplayer   = echoReplayer;
        pathTrail.echoState      = echoState;

        echoInput.echoController = echoCtrl;

        // ── Wire Camera ──
        cam.target = playerGO.transform;

        Debug.Log("SceneLinker: All references wired successfully.");
    }
}